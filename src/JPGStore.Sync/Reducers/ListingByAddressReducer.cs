using Cardano.Sync.Data.Models;
using Cardano.Sync.Reducers;
using CardanoSharp.Wallet.Extensions.Models;
using JPGStore.Data.Extensions;
using JPGStore.Data.Models;
using JPGStore.Data.Models.Datums;
using JPGStore.Data.Models.Enums;
using JPGStore.Data.Models.Reducers;
using Microsoft.EntityFrameworkCore;
using PallasDotnet.Models;
using CardanoSharpAddress = CardanoSharp.Wallet.Models.Addresses.Address;
using DatumType = PallasDotnet.Models.DatumType;
using Block = PallasDotnet.Models.Block;
using TransactionOutput = PallasDotnet.Models.TransactionOutput;
using CardanoSharp.Wallet.Utilities;
using JPGStore.Data.Utils;

namespace JPGStore.Sync.Reducers;

public class ListingByAddressReducer
(
    IDbContextFactory<JPGStoreSyncDbContext> dbContextFactory,
    IConfiguration configuration,
    ILogger<ListingByAddressReducer> logger
) : IReducer
{
    private readonly string _validatorPkh = configuration["JPGStoreMarketplaceValidatorScriptHash"]!;
    private readonly ILogger<ListingByAddressReducer> _logger = logger;

    public async Task RollBackwardAsync(NextResponse response)
    {
        _logger.Log(LogLevel.Trace, "Processing rollback.");

        await Task.CompletedTask;
    }

    public async Task RollForwardAsync(NextResponse response)
    {  
        using JPGStoreSyncDbContext _dbContext = dbContextFactory.CreateDbContext();
        IEnumerable<TransactionBody> transactions = response.Block.TransactionBodies;

        foreach (TransactionBody tx in transactions)
        {
            await ProcessOutputsAsync(response.Block, tx, _dbContext);
        }

        await _dbContext.SaveChangesAsync();
        await _dbContext.DisposeAsync();
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

            if (pkh != _validatorPkh) continue;

            if (output.Datum is null) continue;
            
            if (output.Datum.Type == DatumType.DatumHash)
            {
                try
                {   
                    if (tx.MetaData is null) continue;

                    string datumCborHex = string.Concat
                    (
                        tx.MetaData.Value
                            .EnumerateArray()
                            .Where(element => element[0].GetInt32() != 30)
                            .Select(element => element[1].GetProperty("Text").GetString()?.Replace(",", ""))
                    );

                    byte[] datum = Convert.FromHexString(datumCborHex);

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
                        ListingDatum = listingDatum,
                        UtxoStatus = UtxoStatus.Unspent,
                        Status = ListingStatus.Created,
                        EstimatedTotalListingValue = default,
                        EstimatedFromListingValue = default,
                        EstimatedToListingValue = default
                    };

                    _dbContext.ListingsByAddress.Add(listingByAddress);
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