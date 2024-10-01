using JPGStore.Data.Models.Enums;

namespace JPGStore.Data.Models.Reducers;

public record UnitPriceMapping
{
    public string PolicyId { get; init; } = default!;
    public string AssetName { get; init; } = default!;
    public ulong Price { get; set; } = default!;
    public ulong Quantity { get; set; } = default!;
    public AssetType AssetType { get; init; }
}