namespace JPGStore.Data.Models.Common;

public record PriceStats
{
    public ulong Price { get; init; }
    public decimal PriceChange { get; init; }
    public decimal PriceChangePercentage { get; init; }
}