namespace JPGStore.Data.Models.Common;

public record TotalStats
{
    public decimal Total { get; init; }
    public decimal TotalChange { get; init; }
    public decimal TotalChangePercentage { get; init; }
}