using System.Text.Json;
using System.Text.Json.Serialization;
using JPGStore.Data.Models.Reducers;

namespace JPGStore.Data.Models.Common;

public interface IAssetMetadata { }

public record NftMetadata : IAssetMetadata
{
    public string? ImgUri { get; set; } = default!;
    public JsonElement? Properties { get; set; }
}

public record CollectionMetadata : IAssetMetadata
{
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string? Images { get; init; }
    public string? Socials { get; init; }
}

public record TokenMetadata : IAssetMetadata
{
    public string? AsciiName { get; set; } = default!;
    public string? Logo { get; set; } = default!;
    public string? Url { get; set; } = default!;
    public string? Description { get; set; } = default!;
    public string? Ticker { get; set; } = default!;
    public string? HexName { get; set; } = default!;
    public ulong? Decimals { get; init; }
}

public record RoyaltyMetadata : IAssetMetadata
{
    public string? Address { get; init; } = default!;
    public decimal? Percentage { get; set; }
}
