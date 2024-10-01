using Cardano.Sync.Data.Models;
using CardanoSharp.Wallet.CIPs.CIP2;
using CardanoSharp.Wallet.CIPs.CIP2.ChangeCreationStrategies;
using CardanoSharp.Wallet.CIPs.CIP2.Models;
using CardanoSharp.Wallet.Common;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Extensions.Models.Transactions.TransactionWitnesses;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using CardanoSharp.Wallet.TransactionBuilding;
using CardanoSharp.Wallet.Utilities;
using JPGStore.Data.Extensions;
using JPGStore.Data.Models.Common;
using JPGStore.Data.Models.Datums;
using Microsoft.Extensions.Configuration;
using PallasDotnet.Models;
using PeterO.Cbor2;
using Address = CardanoSharp.Wallet.Models.Addresses.Address;
using Asset = CardanoSharp.Wallet.Models.Asset;
using Payout = JPGStore.Data.Models.Datums.Payout;
using TransactionInput = CardanoSharp.Wallet.Models.Transactions.TransactionInput;
using TransactionOutput = CardanoSharp.Wallet.Models.Transactions.TransactionOutput;
using Value = Cardano.Sync.Data.Models.Value;

namespace JPGStore.Data.Utils;

public static class JPGStoreUtils
{
    public static NetworkType GetNetworkType(IConfiguration configuration)
    {
        return configuration.GetValue<int>("CardanoNetworkMagic") switch
        {
            764824073 => NetworkType.Mainnet,
            1 => NetworkType.Preprod,
            2 => NetworkType.Preview,
            _ => throw new NotImplementedException()
        };
    }

    public static List<Asset> ValueToAsets(Value value)
    {
        return value.MultiAsset
            .SelectMany(ma => ma.Value.Select(asset => new Asset
            {
                PolicyId = ma.Key.ToLowerInvariant(),
                Name = asset.Key.ToLowerInvariant(),
                Quantity = (long)asset.Value
            }))
            .ToList().Concat([new() { PolicyId = string.Empty, Name = string.Empty, Quantity = (long)value.Coin }]).ToList();
    }

    public static List<Asset> ValueToAsets(PallasDotnet.Models.Value value)
    {
        return value.MultiAsset
            .SelectMany(ma => ma.Value.Select(asset => new Asset
            {
                PolicyId = ma.Key.ToHex().ToLowerInvariant(),
                Name = asset.Key.ToHex().ToLowerInvariant(),
                Quantity = (long)asset.Value
            }))
            .ToList().Concat([new() { PolicyId = string.Empty, Name = string.Empty, Quantity = (long)value.Coin }]).ToList();
    }

    // Given a list of payout, calculate the total value of a listing
    // including the marketplace fee
    public static List<Asset> CalculateListingValueWithFee(List<Payout> payouts)
    {
        IEnumerable<Asset> aggregatedAssets = payouts.Select(po => po.Amount.Value)
            .SelectMany(dict => dict) // Flatten the dictionaries
            .SelectMany(pair => pair.Value, (pair, innerPair) => new { PolicyId = pair.Key, AssetName = innerPair.Key, Amount = innerPair.Value })
            .GroupBy(item => new { item.PolicyId, item.AssetName }) // Group by PolicyId and AssetName
            .Select(group => new Asset
            {
                PolicyId = group.Key.PolicyId.ToLowerInvariant(),
                Name = group.Key.AssetName.ToLowerInvariant(),
                Quantity = group.Aggregate(0L, (acc, item) => acc + (long)item.Amount)
            });

        long uniqueAssets = aggregatedAssets
            .Where(a => !string.IsNullOrEmpty(a.PolicyId))
            .Where(a => a.Quantity < 100 && a.Quantity > 0)
            .Select(a => new { a.Quantity, a.Name })
            .Aggregate(0L, (acc, item) =>
            {
                if (string.IsNullOrEmpty(item.Name)) return acc + item.Quantity;
                return acc + 1;
            });

        long adaValue = aggregatedAssets
            .Where(a => string.IsNullOrEmpty(a.PolicyId))
            .Select(a => a.Quantity)
            .FirstOrDefault();

        double adaFee = (adaValue * 0.02) + (uniqueAssets * 1_000_000);
        double totalAdaFee = Math.Max(1_000_000, adaFee);
        double adaWithFee = adaValue + totalAdaFee;

        // Calculate token fee
        IEnumerable<Asset> tokenWithFee = aggregatedAssets
            .Where(a => !string.IsNullOrEmpty(a.PolicyId) && a.Quantity >= 100)
            .Select(a => new Asset
            {
                PolicyId = a.PolicyId,
                Name = a.Name,
                Quantity = (long)(a.Quantity * 0.02) + a.Quantity
            });

        IEnumerable<Asset> tokenUnderThreshold = aggregatedAssets
            .Where(a => !string.IsNullOrEmpty(a.PolicyId))
            .Where(a => a.Quantity < 100)
            .Select(a => new Asset
            {
                PolicyId = a.PolicyId,
                Name = a.Name,
                Quantity = a.Quantity
            });

        return tokenWithFee.Concat(tokenUnderThreshold).Concat([new() { PolicyId = string.Empty, Name = string.Empty, Quantity = (long)adaWithFee }]).ToList();
    }
}

