using JPGStore.Data.Models.Enums;

namespace JPGStore.Data.Models.Reducers;

public record NftByAddress
{
    public string Address { get; set; } = default!;
    public string TxHash { get; init; } = default!;
    public ulong OutputIndex { get; init; }
    public ulong Slot { get; init; }
    public string PolicyId { get; init; } = default!;
    public string AssetName { get; init; } = default!;
    public UtxoStatus UtxoStatus { get; set; }
}