namespace JPGStore.Data.Models.Reducers;

public record JPGStoreNftPrice
{
    public required string Id { get; set; }
    public ulong Slot { get; init; }
    public string TxHash { get; init; } = default!;
    public ulong TxIndex { get; init; }
    public string PolicyId { get; init; } = default!;
    public string AssetName { get; init; } = default!;
    public ulong Price { get; set; } = default!;
}