using Cardano.Sync.Reducers;
using CardanoSharp.Wallet.Utilities;
using JPGStore.Data.Models;
using JPGStore.Data.Models.Datums;
using JPGStore.Data.Models.Enums;
using JPGStore.Data.Models.Reducers;
using Microsoft.EntityFrameworkCore;
using PallasDotnet.Models;
using CardanoSharpAddress = CardanoSharp.Wallet.Models.Addresses.Address;
using CardanoSharp.Wallet.Extensions.Models;
using JPGStore.Data.Utils;
using Cardano.Sync.Data.Models;
using TransactionOutput = PallasDotnet.Models.TransactionOutput;
using Block = PallasDotnet.Models.Block;
using DatumType = PallasDotnet.Models.DatumType;
using JPGStore.Data.Services;
using CardanoSharp.Wallet.Models;
using JPGStore.Data.Extensions;

namespace JPGStore.Sync.Reducers;

public class ListingByAddressReducer(
    IDbContextFactory<JPGStoreSyncDbContext> dbContextFactory,
    IConfiguration configuration,
    ILogger<ListingByAddressReducer> logger,
    JPGStoreDataService crashrDataService
) : IReducer
{
    private readonly string _validatorPkh = configuration["CrashrMarketplaceValidatorScriptHash"]!;
    private readonly string _feePkh = configuration["CrashrMarketplaceFeePkh"]!;
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
        List<string> inputOutRefs = tx.Inputs.Select(input => input.Id.ToHex().ToLowerInvariant() + input.Index).ToList();
        inputOutRefs.Sort();

        List<ListingByAddress> existingListings = await _dbContext.ListingsByAddress
            .Where(lba => inputOutRefs.Contains(lba.TxHash + lba.TxIndex))
            .ToListAsync();

        foreach (ListingByAddress existingListing in existingListings)
        {
            // Add price stats
            List<Asset> fromAssets = JPGStoreUtils.ValueToAsets(existingListing.Amount);
            List<Asset> toAssets = JPGStoreUtils.CalculateListingValueWithFee(existingListing.ListingDatum.Payouts);

            JPGStorePriceMapping estimatedListingValue = await crashrDataService.GetEstimatedValuesAsync(fromAssets, toAssets, block.Slot);
            ulong estimatedTotalListingValue = estimatedListingValue.EstimatedFromPrice + estimatedListingValue.EstimatedTargetPrice;

            string hashedId = HashingUtils.GenerateHashFromComponents(
                existingListing.Slot,
                existingListing.OwnerAddress,
                existingListing.TxHash,
                existingListing.TxIndex,
                (int)UtxoStatus.Spent,
                existingListing.Status,
                existingListing.Amount.Coin
            );

            // Mark as spent, but listing status will be decided later
            ListingByAddress spentListing = new()
            {
                Id = hashedId,
                OwnerAddress = existingListing.OwnerAddress,
                TxHash = existingListing.TxHash,
                TxIndex = existingListing.TxIndex,
                Slot = block.Slot,
                Amount = new()
                {
                    Coin = existingListing.Amount.Coin,
                    MultiAsset = existingListing.Amount.MultiAsset
                },
                ListingDatum = existingListing.ListingDatum,
                SpentTxHash = tx.Id.ToHex(),
                ListingValue = estimatedListingValue,
                UtxoStatus = UtxoStatus.Spent,
                EstimatedTotalListingValue = estimatedTotalListingValue,
                EstimatedFromListingValue = estimatedListingValue.EstimatedFromPrice,
                EstimatedToListingValue = estimatedListingValue.EstimatedTargetPrice
            };

            int redeemerIndex = inputOutRefs.IndexOf(existingListing.TxHash + existingListing.TxIndex);

            // Find the redeemer of the input
            Redeemer? redeemer = tx.Redeemers?
                .Where(r => r.Index == redeemerIndex)
                .FirstOrDefault();

            // Something went wrong if there's no redeemer
            if (redeemer is null) continue;

            // Check if redeemer is buy or cancel
            try
            {
                BuyRedeemer buyRedeemer = CborConverter.Deserialize<BuyRedeemer>(redeemer.Data);
                string sellerAddr = existingListing.OwnerAddress;
                string sellerPkh = Convert.ToHexString(new CardanoSharpAddress(sellerAddr).GetPublicKeyHash()).ToLowerInvariant();

                TransactionOutput? sellerOutput = tx.Outputs
                    .SkipWhile((o, i) => i < (int)buyRedeemer.Offset)
                    .FirstOrDefault(o => Convert.ToHexString(
                        new CardanoSharpAddress(o.Address.Raw).GetPublicKeyHash()).Equals(sellerPkh,
                        StringComparison.InvariantCultureIgnoreCase));

                List<string> payoutPkhs = existingListing.ListingDatum.Payouts
                    .Select(p => Convert.ToHexString(p.Address.Credential.Hash).ToLowerInvariant())
                    .ToList();

                payoutPkhs.Add(_validatorPkh);
                payoutPkhs.Add(_feePkh);

                // Buyer address is the first output that's not the fee address
                // or any address in the payout
                string? buyerAddress = null;

                try
                {
                    buyerAddress = tx.Outputs
                    .SkipWhile((o, i) => i < (int)buyRedeemer.Offset)
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
                // If there's an exception, then it's a cancel tx
                spentListing.Status = ListingStatus.Canceled;
            }

            _dbContext.ListingsByAddress.Add(spentListing);
        }
    }

    // If output is sent to the marketplace validator, process the output and 
    // mark it as a listing
    private async Task ProcessOutputsAsync(Block block, TransactionBody tx, JPGStoreSyncDbContext _dbContext)
    {
        foreach (TransactionOutput output in tx.Outputs)
        {
            string? outputBech32Addr = output.Address.Raw.ToBech32();

            if (outputBech32Addr is null || !outputBech32Addr.StartsWith("addr")) continue;

            // If output is sent to the marketplace validator, process the output as new listing
            string pkh = Convert.ToHexString(new CardanoSharpAddress(outputBech32Addr).GetPublicKeyHash()).ToLowerInvariant();

            if (pkh != _validatorPkh) continue;
            if (output.Datum is null || output.Datum.Type != DatumType.InlineDatum) continue;

            try
            {
                byte[] datum = output.Datum.Data;
                ListingDatum listingDatum = CborConverter.Deserialize<ListingDatum>(datum);
                Cardano.Sync.Data.Models.Datums.Address? ownerCredential = listingDatum.Payouts
                    .Select(po => po.Address)
                    .Where(a => Convert.ToHexString(a.Credential.Hash).Equals(listingDatum.OwnerPkh, StringComparison.InvariantCultureIgnoreCase))
                    .FirstOrDefault();

                string ownerAddressBech32 = AddressUtility.GetBaseAddress(
                        ownerCredential!.Credential.Hash,
                        ownerCredential.StakeCredential?.Credential.Hash ?? [],
                        JPGStoreUtils.GetNetworkType(configuration)).ToString();

                if (ownerCredential is null)
                {
                    _logger.LogInformation("Owner credential not found in payout addresses");
                    continue;
                }

                // Add price stats
                List<Asset> fromAssets = JPGStoreUtils.ValueToAsets(output.Amount);
                List<Asset> toAssets = JPGStoreUtils.CalculateListingValueWithFee(listingDatum.Payouts);

                JPGStorePriceMapping estimatedListingValue = await crashrDataService.GetEstimatedValuesAsync(fromAssets, toAssets, block.Slot);
                ulong estimatedTotalListingValue = estimatedListingValue.EstimatedFromPrice + estimatedListingValue.EstimatedTargetPrice;

                string hashedId = HashingUtils.GenerateHashFromComponents(
                    block.Slot,
                    ownerAddressBech32,
                    tx.Id.ToHex(),
                    output.Index,
                    (int)UtxoStatus.Unspent,
                    (int)ListingStatus.Created,
                    output.Amount.Coin
                );

                ListingByAddress listingByAddress = new()
                {
                    Id = hashedId,
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
                    ListingDatum = listingDatum,
                    UtxoStatus = UtxoStatus.Unspent,
                    ListingValue = estimatedListingValue,
                    Status = ListingStatus.Created,
                    EstimatedTotalListingValue = estimatedTotalListingValue,
                    EstimatedFromListingValue = estimatedListingValue.EstimatedFromPrice,
                    EstimatedToListingValue = estimatedListingValue.EstimatedTargetPrice
                };

                _dbContext.ListingsByAddress.Add(listingByAddress);
            }
            catch (Exception e)
            {
                _logger.LogError("Error deserializing listing datum: {message}", e.Message);
            }
        }
    }
}
