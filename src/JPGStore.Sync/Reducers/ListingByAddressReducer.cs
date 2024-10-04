using Cardano.Sync.Data.Models;
using Cardano.Sync.Reducers;
using CardanoSharp.Wallet.Extensions.Models;
using JPGStore.Data.Extensions;
using JPGStore.Data.Models;
using JPGStore.Data.Models.Enums;
using JPGStore.Data.Models.Reducers;
using Microsoft.EntityFrameworkCore;
using PallasDotnet.Models;
using CardanoSharpAddress = CardanoSharp.Wallet.Models.Addresses.Address;
using ChrysalisAddress = Chrysalis.Cardano.Models.Plutus.Address;
using DatumType = PallasDotnet.Models.DatumType;
using Block = PallasDotnet.Models.Block;
using TransactionOutput = PallasDotnet.Models.TransactionOutput;
using JPGStore.Data.Utils;
using Chrysalis.Cardano.Models.Plutus;
using CardanoSharp.Wallet.Extensions;
using Crashr.Data.Models.Redeemers;
using Chrysalis.Cbor;
using Chrysalis.Cardano.Models.Cbor;
using JPGStore.Data.Models.Datums;
using System.Net;
using CardanoSharp.Wallet.Utilities;

namespace JPGStore.Sync.Reducers;

public class ListingByAddressReducer
(
    IDbContextFactory<JPGStoreSyncDbContext> dbContextFactory,
    IConfiguration configuration,
    ILogger<ListingByAddressReducer> logger
) : IReducer
{
    private readonly string _v1validatorPkh = configuration["JPGStoreMarketplaceV1ValidatorScriptHash"]!;
    private readonly string _v2validatorPkh = configuration["JPGStoreMarketplaceV2ValidatorScriptHash"]!;
    private readonly ILogger<ListingByAddressReducer> _logger = logger;

    public async Task RollBackwardAsync(NextResponse response)
    {
        using JPGStoreSyncDbContext _dbContext = dbContextFactory.CreateDbContext();

        // Remove all entries with slot greater than the rollback slot
        ulong rollbackSlot = response.Block.Slot;
        IQueryable<ListingByAddress> rollbackEntries = _dbContext.ListingsByAddress.AsNoTracking().Where(lba => lba.Slot > rollbackSlot);
        _dbContext.ListingsByAddress.RemoveRange(rollbackEntries);

        // Save changes
        await _dbContext.SaveChangesAsync();
        await _dbContext.DisposeAsync();
    }

    public async Task RollForwardAsync(NextResponse response)
    {  
        using JPGStoreSyncDbContext _dbContext = dbContextFactory.CreateDbContext();
        IEnumerable<TransactionBody> transactions = response.Block.TransactionBodies;

        foreach (TransactionBody tx in transactions)
        {
            await ProcessOutputsAsync(response.Block, tx, _dbContext);
            await ProcessInputsAsync(response.Block, tx, _dbContext);
        }

        await _dbContext.SaveChangesAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task ProcessInputsAsync(Block block, TransactionBody tx, JPGStoreSyncDbContext _dbContext)
    {   
        string txHash = tx.Id.ToHex();
        List<string> inputOutRefs = tx.Inputs.Select(input => input.Id.ToHex().ToLowerInvariant() + input.Index).ToList();
        inputOutRefs.Sort();

        List<ListingByAddress> existingListings = await _dbContext.ListingsByAddress
            .AsNoTracking()
            .Where(lba => inputOutRefs.Contains(lba.TxHash + lba.TxIndex))
            .ToListAsync();

        foreach (ListingByAddress existingListing in existingListings)
        {
            ListingByAddress spentListing = new()
            {
                OwnerAddress = existingListing.OwnerAddress,
                TxHash = existingListing.TxHash,
                TxIndex = existingListing.TxIndex,
                Slot = block.Slot,
                Amount = new()
                {
                    Coin = existingListing.Amount.Coin,
                    MultiAsset = existingListing.Amount.MultiAsset
                },
                SpentTxHash = tx.Id.ToHex(),
                UtxoStatus = UtxoStatus.Spent,
                EstimatedTotalListingValue = default!,
                EstimatedFromListingValue = default!,
                EstimatedToListingValue = default
            };

            int redeemerIndex = inputOutRefs.IndexOf(existingListing.TxHash + existingListing.TxIndex);

            // Find the redeemer of the input
            Redeemer? redeemer = tx.Redeemers?
                .Where(r => r.Index == redeemerIndex)
                .FirstOrDefault();

            // Something went wrong if there's no redeemer
            if (redeemer is null) continue;

            string redeemerCborHex = Convert.ToHexString(redeemer.Data);

            try 
            {
                Buy? buyRedeemer = CborSerializer.Deserialize<Buy>(redeemer.Data);
                string sellerAddr = existingListing.OwnerAddress;
                string sellerPkh = Convert.ToHexString(new CardanoSharpAddress(sellerAddr).GetPublicKeyHash()).ToLowerInvariant();

                if (buyRedeemer is null) continue;
                
                TransactionOutput? sellerOutput = tx.Outputs
                    .SkipWhile((o, i) => i < buyRedeemer.Offset.Value)
                    .FirstOrDefault(o => Convert.ToHexString(
                        new CardanoSharpAddress(o.Address.Raw).GetPublicKeyHash()).Equals(sellerPkh,
                        StringComparison.InvariantCultureIgnoreCase));

                if (existingListing.ListingDatumCbor is null) continue;

                Listing? existingListingDatum = CborSerializer.Deserialize<Listing>(existingListing.ListingDatumCbor);

                if (existingListingDatum is null) continue;

                List<string> payoutPkhs = existingListingDatum.Payouts.Value
                    .Select(p => Convert.ToHexString(((VerificationKey)p.Address.PaymentCredential).VerificationKeyHash.Value))
                    .ToList();

                payoutPkhs.Add(_v1validatorPkh);

                // Buyer address is the first output that's not the fee address
                // or any address in the payout
                string? buyerAddress = null;

                try
                {
                    buyerAddress = tx.Outputs
                    .SkipWhile((o, i) => i < buyRedeemer.Offset.Value)
                    .FirstOrDefault(o => !payoutPkhs.Contains(Convert.ToHexString(
                        new CardanoSharpAddress(o.Address.Raw).GetPublicKeyHash()).ToLowerInvariant()))?
                    .Address.Raw!.ToBech32();
                }
                catch { }

                // If buyer address is in the payout pkh, something went wrong?
                // It could be the seller bought his own listing, so we'll leave the buyer address as null
                spentListing.BuyerAddress = buyerAddress;
                spentListing.Status = ListingStatus.Sold;
                spentListing.SellerPayoutValue = new()
                {
                    Coin = sellerOutput!.Amount.Coin,
                    MultiAsset = sellerOutput.Amount.MultiAsset.ToDictionary(k => k.Key.ToHex(), v => v.Value.ToDictionary(
                            k => k.Key.ToHex(),
                            v => v.Value
                    ))
                };
            }
            catch
            {  
                // Check if it is update or a cancel listing
                string? existingListingSubject = existingListing.Amount.MultiAsset
                    .Select(ma => ma.Value
                        .Select(v => ma.Key + v.Key)
                        .FirstOrDefault())
                    .FirstOrDefault();

                List<string> txOutputsSubjects = tx.Outputs
                    .Where(o => 
                    {
                        string outputBech32Addr = o.Address.Raw.ToBech32()!;

                        string pkh = Convert.ToHexString(new CardanoSharpAddress(outputBech32Addr).GetPublicKeyHash()).ToLowerInvariant();

                        return pkh == _v1validatorPkh;
                    })
                    .SelectMany(o => o.Amount.MultiAsset
                        .Select(ma => ma.Value
                            .Select(v => ma.Key.ToHex() + v.Key.ToHex()))
                        .FirstOrDefault()!)
                    .ToList();

                if (existingListingSubject != null && txOutputsSubjects.Contains(existingListingSubject))
                {
                    spentListing.Status = ListingStatus.Updated;
                }
                else
                {
                    spentListing.Status = ListingStatus.Canceled;
                }
            }

            _dbContext.Add(spentListing);
        }

        await Task.CompletedTask;
    }

    private async Task ProcessOutputsAsync(Block block, TransactionBody tx, JPGStoreSyncDbContext _dbContext)
    {
        string txHash = tx.Id.ToHex();

        foreach (TransactionOutput output in tx.Outputs)
        {
            string? outputBech32Addr = output.Address.Raw.ToBech32();

            if (outputBech32Addr is null || !outputBech32Addr.StartsWith("addr")) continue;

            // If output is sent to the marketplace validator, process the output as new listing
            string pkh = Convert.ToHexString(new CardanoSharpAddress(outputBech32Addr).GetPublicKeyHash()).ToLowerInvariant();

            if (pkh != _v1validatorPkh && pkh != _v2validatorPkh) continue;

            if (output.Datum is null) continue;
            
            if (output.Datum.Type == DatumType.DatumHash)
            {
                try
                {   
                    if (tx.MetaData is null) continue;

                    List<string> datumCborHexList = JPGStoreUtils.MapMetadataToCborHexList(tx.MetaData.Value);

                    byte[] datum = Convert.FromHexString(datumCborHexList[(int)output.Index]);

                    string txOutputDatumHash = Convert.ToHexString(datum).ToLowerInvariant();

                    ChrysalisAddress? ownerCredential = pkh == _v1validatorPkh ? 
                        JPGStoreUtils.GetOwnerCredentialFromDatum(datum, TransactionDatum.Listing) :
                        JPGStoreUtils.GetOwnerCredentialFromDatum(datum, TransactionDatum.Offer);

                    string ownerAddressBech32 = pkh == _v1validatorPkh ? 
                        AddressUtility.GetEnterpriseAddress(CborSerializer.Deserialize<Listing>(datum)!.OwnerPkh.Value, CardanoSharp.Wallet.Enums.NetworkType.Mainnet).ToString() :
                        AddressUtility.GetEnterpriseAddress(CborSerializer.Deserialize<Offer>(datum)!.OwnerPkh.Value, CardanoSharp.Wallet.Enums.NetworkType.Mainnet).ToString();

                    if (ownerCredential is null)
                    {
                        _logger.LogInformation("Owner credential not found in payout addresses");
                        continue;
                    }

                    ListingByAddress listingByAddress = new()
                    {
                        OwnerAddress = ownerAddressBech32,
                        TxHash = tx.Id.ToHex(),
                        TxIndex = output.Index,
                        Slot = block.Slot,
                        Amount = new()
                        {
                            Coin = output.Amount.Coin,
                            MultiAsset = output.Amount.MultiAsset.ToDictionary(k => k.Key.ToHex(), v => v.Value.ToDictionary(
                            k => k.Key.ToHex(),
                            v => v.Value
                        ))
                        },
                        UtxoStatus = UtxoStatus.Unspent,
                        Status = ListingStatus.Created,
                        EstimatedTotalListingValue = default,
                        EstimatedFromListingValue = default,
                        EstimatedToListingValue = default
                    };
                    
                    if (pkh == _v1validatorPkh)
                    {
                        Listing? listingDatum = CborSerializer.Deserialize<Listing>(datum);
                        listingByAddress.ListingDatumCbor = CborSerializer.Serialize(listingDatum!);
                    }
                    else if (pkh == _v2validatorPkh) 
                    {
                        Offer? offerDatum = CborSerializer.Deserialize<Offer>(datum);
                        listingByAddress.ListingDatumCbor = CborSerializer.Serialize(offerDatum!);
                    }
                    
                    _dbContext.Add(listingByAddress);
                }
                catch (Exception e)
                {
                    _logger.LogError("Error deserializing listing datum: {message}", e.Message);
                }
            }
        }

        await Task.CompletedTask;
    }
}