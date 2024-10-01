using Cardano.Sync.Data.Models;

namespace JPGStore.Data.Models.Common;

public enum AssetType
{
    Unknown,
    NFT,
    Token,
    Collection
}

public record AssetActiveListing
{
    public string? TxHash { get; set; } = default!;
    public ulong? TxIndex { get; set; }
    public Value Value { get; set; } = default!;
    public ulong? EstimatedToListingValue { get; set; }
}

public record Asset
{
    public AssetType AssetType { get; init; }
    public string PolicyId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Image { get; set; }
    public ulong Quantity { get; set; }
    public ulong? EstimatedAdaValue { get; set; }
    public CollectionMetadata? CollectionMetadata { get; set; } = default!;
    public Dictionary<string, string>? Metadata { get; set; }
    public AssetActiveListing? ActiveListing { get; set; }
    public decimal RoyaltyPercentage { get; set; }
    public ulong RoyaltyShare { get; set; }

    public Asset(AssetType assetType = AssetType.Unknown) => AssetType = assetType;
}

public record NftAsset : Asset
{
    public new AssetType AssetType { get; set; } = AssetType.NFT;
    public string? AsciiAssetName { get; set; }
    public string? OwnerAddress { get; set; } = default!;
    public RoyaltyMetadata? Royalty { get; set; }
    public ulong LastTradedPrice { get; set; }
    public ulong FloorPrice { get; set; }
    public ulong HighestPrice { get; set; }
    public ulong LowestPrice { get; set; }
    public double AveragePrice { get; set; }
    public int TotalTrades { get; set; }

    public NftAsset() : base(AssetType.NFT) { }
}

public record CollectionAsset : Asset
{
    public new AssetType AssetType { get; set; } = AssetType.Collection;
    public PriceStats VolumeStats { get; set; } = default!;
    public PriceStats FloorStats { get; set; } = default!;
    public TotalStats OwnersStats { get; set; } = default!;
    public int TotalListed { get; set; }
    public int Rank { get; set; }
    public RoyaltyMetadata? Royalty { get; init; } = default!;

    public CollectionAsset() : base(AssetType.Collection) { }
}

public record TokenAsset : Asset
{
    public new AssetType AssetType { get; set; } = AssetType.Token;
    public string HexName { get; set; } = default!;

    public TokenAsset() : base(AssetType.Token) { }
}