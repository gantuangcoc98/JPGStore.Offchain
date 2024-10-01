using CardanoSharp.Wallet.Models;
using JPGStore.Data.Models;
using JPGStore.Data.Models.Enums;
using JPGStore.Data.Models.Reducers;
using Microsoft.EntityFrameworkCore;

namespace JPGStore.Data.Services;

public class JPGStoreDataService(IDbContextFactory<JPGStoreSyncDbContext> dbContextFactory)
{
    public async Task<JPGStorePriceMapping> GetEstimatedValuesAsync(List<Asset> fromAssets, List<Asset> toAssets, ulong slot)
    {
        using JPGStoreSyncDbContext dbContext = dbContextFactory.CreateDbContext();

        // Get ADA values
        ulong fromAdaValue = (ulong)fromAssets.Where(a => a.PolicyId == string.Empty).Sum(a => a.Quantity);
        ulong toAdaValue = (ulong)toAssets.Where(a => a.PolicyId == string.Empty).Sum(a => a.Quantity);

        // Convert to assets without lovelace
        List<Asset> fromValueAssets = fromAssets.Where(a => a.PolicyId != string.Empty).ToList();
        List<Asset> toValueAssets = toAssets.Where(a => a.PolicyId != string.Empty).ToList();
        List<string> fromValueUnits = fromValueAssets.Select(a => a.PolicyId + a.Name).ToList();
        List<string> toValueUnits = toValueAssets.Select(a => a.PolicyId + a.Name).ToList();

        // Combine the assets removing duplicates
        List<Asset> commonAssets = fromValueAssets.Concat(toValueAssets).Distinct().ToList();
        List<string> commonUnits = commonAssets.Select(a => a.PolicyId + a.Name).ToList();

        // Get the price of the assets from the database
        IQueryable<UnitPriceMapping> nftPrices = dbContext.JPGStoreNftPrices
            .Where(nft => nft.Slot <= slot && commonUnits.Contains(nft.PolicyId + nft.AssetName))
            .GroupBy(nft => new { nft.PolicyId, nft.AssetName })
            .Select(g => new UnitPriceMapping()
            {
                PolicyId = g.Key.PolicyId,
                AssetName = g.Key.AssetName,
                Price = g.OrderByDescending(n => n.Slot).FirstOrDefault()!.Price,
                AssetType = AssetType.NFT
            });

        IQueryable<UnitPriceMapping> tokenPrices = dbContext.TokenPrices
            .Where(stp => stp.Slot <= slot && commonUnits.Contains(stp.PolicyId + stp.AssetName))
            .GroupBy(stp => new { stp.PolicyId, stp.AssetName })
            .Select(g => new UnitPriceMapping()
            {
                PolicyId = g.Key.PolicyId,
                AssetName = g.Key.AssetName,
                Price = g.OrderByDescending(n => n.Slot).FirstOrDefault()!.Price,
                AssetType = AssetType.FT
            });

        IQueryable<UnitPriceMapping> floorPrices = dbContext.CollectionSummaries
            .Where(cs => cs.Slot <= slot && commonAssets.Select(ca => ca.PolicyId).Contains(cs.PolicyId))
            .GroupBy(cs => cs.PolicyId)
            .Select(g => new UnitPriceMapping()
            {
                PolicyId = g.Key,
                AssetName = string.Empty,
                Price = g.OrderByDescending(n => n.Slot).FirstOrDefault()!.Floor,
                AssetType = AssetType.NFT
            });

        List<UnitPriceMapping> combinedPrices = await nftPrices
            .Concat(tokenPrices)
            .Concat(floorPrices)
            .ToListAsync();

        combinedPrices = combinedPrices
            .GroupBy(cs => new{cs.PolicyId, cs.AssetName})
            .Select(g => g.FirstOrDefault(cs => cs.AssetType == AssetType.FT) ?? g.First())
            .ToList();

        // Get the estimated values
        JPGStorePriceMapping priceMapping = new();

        priceMapping.EstimatedFromPrice += fromAdaValue;
        fromValueAssets.ToList().ForEach(a =>
        {
            string unit = a.PolicyId + a.Name;
            ulong unitPrice = combinedPrices.Find(p => p.PolicyId + p.AssetName == unit)?.Price ?? 0;
            ulong floorPrice = combinedPrices.Find(p => p.PolicyId == a.PolicyId && string.IsNullOrEmpty(p.AssetName))?.Price ?? 0;
            ulong actualPrice = unitPrice <= 0 ? floorPrice : unitPrice;
            ulong estimatedValue = (ulong)a.Quantity * actualPrice;
            AssetType assetType = combinedPrices.Find(p => p.PolicyId + p.AssetName == unit)?.AssetType ?? AssetType.Unknown;

            priceMapping.EstimatedFromPrice += estimatedValue;
            priceMapping.FromPrices.Add(new UnitPriceMapping()
            {
                PolicyId = a.PolicyId,
                AssetName = a.Name,
                Price = estimatedValue,
                AssetType = assetType == AssetType.Unknown ? a.Quantity == 1 ? AssetType.NFT : AssetType.FT : assetType,
                Quantity = (ulong)a.Quantity
            });
        });

        priceMapping.EstimatedTargetPrice += toAdaValue;
        toValueAssets.ToList().ForEach(a =>
        {
            string unit = a.PolicyId + a.Name;
            ulong unitPrice = combinedPrices.Find(p => p.PolicyId + p.AssetName == unit)?.Price ?? 0;
            ulong floorPrice = combinedPrices.Find(p => p.PolicyId == a.PolicyId && string.IsNullOrEmpty(p.AssetName))?.Price ?? 0;
            ulong actualPrice = unitPrice <= 0 ? floorPrice : unitPrice;
            ulong estimatedValue = (ulong)a.Quantity * actualPrice;
            AssetType assetType = combinedPrices.Find(p => p.PolicyId + p.AssetName == unit)?.AssetType ?? AssetType.Unknown;

            priceMapping.EstimatedTargetPrice += estimatedValue;
            priceMapping.ToPrices.Add(new UnitPriceMapping()
            {
                PolicyId = a.PolicyId,
                AssetName = a.Name,
                Price = estimatedValue,
                AssetType = assetType == AssetType.Unknown ? a.Quantity == 1 ? AssetType.NFT : AssetType.FT : assetType,
                Quantity = (ulong)a.Quantity
            });
        });

        return priceMapping;
    }

    public async Task<int> GetUniqueOwnersByCollectionAsync(string policyId, ulong? slot)
    {
        using JPGStoreSyncDbContext _dbContext = dbContextFactory.CreateDbContext();

        IQueryable<NftByAddress> baseQuery = _dbContext.NftsByAddress
            .Where(nba => nba.PolicyId == policyId);

        if (slot.HasValue)
        {
            baseQuery = baseQuery.Where(nba => nba.Slot <= slot);
        }

        return await baseQuery
            .GroupBy(nba => new { nba.PolicyId, nba.AssetName, nba.TxHash, nba.OutputIndex })
            .Where(g => g.Count() < 2)
            .Select(g => g.First().Address)
            .Distinct()
            .CountAsync();
    }

    public async Task<ulong> GetActiveFloorPriceByCollectionAsync(string policyId, ulong? slot)
    {
        using JPGStoreSyncDbContext _dbContext = dbContextFactory.CreateDbContext();

        IQueryable<ListingByAsset> baseQuery = _dbContext.ListingsByAsset
            .Where(lba => lba.EstimatedAdaValue > 0)
            .Where(lba => lba.PolicyId == policyId);

        if (slot.HasValue)
        {
            baseQuery = baseQuery.Where(lba => lba.Slot <= slot);
        }

        List<ulong> adaValues = await baseQuery
            .Where(lba => lba.ListingType == ListingType.Offer)
            .GroupBy(lba => new { lba.TxHash, lba.TxIndex, lba.PolicyId, lba.AssetName })
            .Where(g => g.Count() < 2)
            .Select(g => (ulong?)g.First().EstimatedAdaValue ?? 0UL)
            .ToListAsync();

        return adaValues.Count > 0 ? adaValues.Min() : 0;
    }

    public async Task<ulong> GetHistoricalFloorPriceByCollectionAsync(string policyId, ulong? slot)
    {
        using JPGStoreSyncDbContext _dbContext = dbContextFactory.CreateDbContext();

        IQueryable<ListingByAsset> baseQuery = _dbContext.ListingsByAsset
            .Where(lba => lba.PolicyId == policyId)
            .Where(lba => lba.PolicyId == policyId);

        if (slot.HasValue)
        {
            baseQuery = baseQuery.Where(lba => lba.Slot <= slot);
        }

        List<ulong> adaValues = await baseQuery
            .Where(lba => lba.ListingType == ListingType.Offer)
            .Where(lba => lba.EstimatedAdaValue > 0)
            .Select(lba => (ulong?)lba.EstimatedAdaValue ?? 0UL)
            .ToListAsync();

        return adaValues.Count > 0 ? adaValues.Min() : 0;
    }
}