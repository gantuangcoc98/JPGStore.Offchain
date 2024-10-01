namespace JPGStore.Data.Models.Reducers;

public record CollectionSummary
{
    public string PolicyId { get; init; } = default!;
    public ulong Slot { get; init; }
    public ulong Volume { get; set; } = default!;
    public ulong Floor { get; set; } = default!;
    public int UniqueOwners { get; set; } = default!;
}