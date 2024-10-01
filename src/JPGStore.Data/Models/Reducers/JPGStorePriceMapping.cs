namespace JPGStore.Data.Models.Reducers;

public record JPGStorePriceMapping
{
    public List<UnitPriceMapping> FromPrices { get; set; } = [];
    public List<UnitPriceMapping> ToPrices { get; set; } = [];
    public ulong EstimatedFromPrice { get; set; } = default!;
    public ulong EstimatedTargetPrice { get; set; } = default!;
}