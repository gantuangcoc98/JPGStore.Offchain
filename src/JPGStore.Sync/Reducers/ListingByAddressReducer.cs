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
using JPGStore.Data.Models.Datums;
using CardanoSharp.Wallet.Utilities;
using CborSerialization;
using NSec.Cryptography;

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
                EstimatedToListingValue = default,
                ListingDatumCbor = existingListing.ListingDatumCbor ?? throw new CborException("NULL value was found in ListingByAddress table")
            };

            int redeemerIndex = inputOutRefs.IndexOf(existingListing.TxHash + existingListing.TxIndex);

            // Find the redeemer of the input
            Redeemer? redeemer = tx.Redeemers?
                .Where(r => r.Index == redeemerIndex)
                .FirstOrDefault();

            // Something went wrong if there's no redeemer
            if (redeemer is null) continue;

            string redeemerCborHex = Convert.ToHexString(redeemer.Data);

            bool isOnListingTransaction = false;

            // Check if the existingListingInput is an Offer Contract
            Offer? offerDatum = null;

            try
            {
                byte[] existingOfferDatum = existingListing.ListingDatumCbor;
                offerDatum = CborSerializer.Deserialize<Offer>(existingOfferDatum);
            }
            catch
            {
                isOnListingTransaction = true;
            }

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
                if (isOnListingTransaction)
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
                else if (offerDatum != null)
                {
                    Blake2b algorithm = HashAlgorithm.Blake2b_256;

                    byte[] existingListingHashedDatum = algorithm.Hash(existingListing.ListingDatumCbor);

                    bool isUpdateOutputExist = false;

                    foreach (TransactionOutput output in tx.Outputs)
                    {
                        if (output.Datum is null) continue;

                        if (output.Datum.Data.SequenceEqual(existingListingHashedDatum))
                        {
                            isUpdateOutputExist = true;
                        }
                    }

                    if (isUpdateOutputExist)
                    {
                        spentListing.Status = ListingStatus.Updated;
                    }
                    else
                    {
                        string? offerOwnerPkh = Convert.ToHexString(offerDatum.OwnerPkh.Value).ToLowerInvariant();

                        string? offerOwnerPayoutAsset = offerDatum.Payouts.Value
                            .Where(po => 
                            {
                                byte[] payoutPaymentVKey = ((VerificationKey)po.Address.PaymentCredential).VerificationKeyHash.Value;

                                string payoutPaymentKeyHash = Convert.ToHexString(payoutPaymentVKey);

                                return payoutPaymentKeyHash.Equals(offerOwnerPkh, StringComparison.InvariantCultureIgnoreCase);
                            })
                            .Select(po => po.PayoutValue.Value
                                .Select(v => v.Value
                                    .Amount.Value
                                        .Select(t => Convert.ToHexString(v.Key.Value) + Convert.ToHexString(t.Key.Value))
                                        .FirstOrDefault()
                                )
                                .FirstOrDefault()
                            )
                            .FirstOrDefault();

                        List<TransactionOutput> offerOwnerTxOutputs = tx.Outputs
                            .Where(o => 
                            {
                                string outputBech32Addr = o.Address.Raw.ToBech32()!;

                                string pkh = Convert.ToHexString(new CardanoSharpAddress(outputBech32Addr).GetPublicKeyHash()).ToLowerInvariant();

                                return pkh == offerOwnerPkh;
                            })
                            .ToList();
                        
                        if (offerOwnerTxOutputs.Count > 0)
                        {
                            try
                            {
                                List<string> offerOwnerTxOutputAssets = offerOwnerTxOutputs
                                    .SelectMany(o => o.Amount.MultiAsset
                                        .Select(ma => ma.Value
                                            .Select(v => ma.Key.ToHex() + v.Key.ToHex()))
                                        .FirstOrDefault()!)
                                    .ToList();

                                if (offerOwnerTxOutputAssets.Contains(offerOwnerPayoutAsset!.ToLowerInvariant()))
                                {
                                    spentListing.Status = ListingStatus.Sold;
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.LogError("Cannot evaluate MultiAssets from offerOwnerTxOutputs!");
                            }
                            finally
                            {
                                spentListing.Status = ListingStatus.Canceled;
                            }
                        }
                        else
                        {
                            spentListing.Status = ListingStatus.Canceled;
                        }
                    }
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

                    byte[] outputDatumHash = output.Datum.Data;

                    List<string> datumCborHexList = JPGStoreUtils.MapMetadataToCborHexList(tx.MetaData.Value);

                    // Check if the outputDatumHash can be constructed in one of the datum inside the list.
                    byte[]? datum = null;

                    Blake2b algorithm = HashAlgorithm.Blake2b_256;

                    datumCborHexList.ForEach(datumCborHex => {
                        byte[] hashedDatum = algorithm.Hash(Convert.FromHexString(datumCborHex));

                        if (outputDatumHash.SequenceEqual(hashedDatum))
                        {
                            datum = Convert.FromHexString(datumCborHex);
                        }
                    });

                    if (datum is null) continue;

                    // Check if the datum is a listing or offer, then get the owner address.
                    string? ownerAddressBech32 = null;

                    if (pkh == _v1validatorPkh)
                    {
                        Listing? listingDatum = CborSerializer.Deserialize<Listing>(datum);

                        if (listingDatum is null) continue;

                        ownerAddressBech32 = GetAddressBech32FromListingDatum(listingDatum);
                    }
                    else if (pkh == _v2validatorPkh)
                    {
                        Offer? offerDatum = CborSerializer.Deserialize<Offer>(datum);

                        if (offerDatum is null) continue;

                        ownerAddressBech32 = GetAddressBech32FromOfferDatum(offerDatum);
                    }

                    if (ownerAddressBech32 is null) continue;

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
                        EstimatedToListingValue = default,
                        ListingDatumCbor = datum
                    };

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

    private string? GetAddressBech32FromListingDatum(Listing listingDatum)
    {
        ChrysalisAddress? listingOwnerAddress = listingDatum.Payouts.Value
            .Select(po => po.Address)
            .Where(a => Convert.ToHexString(((VerificationKey)a.PaymentCredential).VerificationKeyHash.Value).Equals(Convert.ToHexString(listingDatum.OwnerPkh.Value), StringComparison.InvariantCultureIgnoreCase))
            .FirstOrDefault();

        if (listingOwnerAddress is null) return null;

        byte[] listingOwnerAddressPaymentVKey = ((VerificationKey)listingOwnerAddress.PaymentCredential).VerificationKeyHash.Value;

        byte[]? listingOwnerAddressStakeVKey = JPGStoreUtils.GetStakeVerificationKeyFromAddress(listingOwnerAddress);

        if (listingOwnerAddressStakeVKey is null) return null;

        return AddressUtility.GetBaseAddress(
            listingOwnerAddressPaymentVKey,
            listingOwnerAddressStakeVKey,
            CardanoSharp.Wallet.Enums.NetworkType.Mainnet
        ).ToString();
    }

    private string? GetAddressBech32FromOfferDatum(Offer offerDatum)
    {
        ChrysalisAddress? offerOwnerAddress = offerDatum.Payouts.Value
            .Select(po => po.Address)
            .Where(a => Convert.ToHexString(((VerificationKey)a.PaymentCredential).VerificationKeyHash.Value).Equals(Convert.ToHexString(offerDatum.OwnerPkh.Value), StringComparison.InvariantCultureIgnoreCase))
            .FirstOrDefault();

        if (offerOwnerAddress is null) return null;

        byte[] offerOwnerAddressPaymentVKey = ((VerificationKey)offerOwnerAddress.PaymentCredential).VerificationKeyHash.Value;

        byte[]? offerOwnerAddressStakeVKey = JPGStoreUtils.GetStakeVerificationKeyFromAddress(offerOwnerAddress);

        if (offerOwnerAddressStakeVKey is null) return null;

        return AddressUtility.GetBaseAddress(
            offerOwnerAddressPaymentVKey,
            offerOwnerAddressStakeVKey,
            CardanoSharp.Wallet.Enums.NetworkType.Mainnet
        ).ToString();
    }
}