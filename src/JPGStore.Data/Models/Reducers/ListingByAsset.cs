
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using JPGStore.Data.Models.Enums;

namespace JPGStore.Data.Models.Reducers;

public record ListingByAsset : Listing
{
    public string PolicyId { get; init; } = default!;
    public string AssetName { get; init; } = string.Empty;
    public ulong EstimatedAdaValue { get; init; }
    public ListingType ListingType { get; init; }

    [NotMapped]
    public JPGStorePriceMapping ListingValue { get; set; } = default!;

    public string ListingValueJson
    {
        get => JsonSerializer.Serialize(ListingValue);
        set
        {
            try
            {
                ListingValue = JsonSerializer.Deserialize<JPGStorePriceMapping>(value) ?? throw new InvalidOperationException("Unable to deserialize.");
            }
            catch (Exception)
            {
                ListingValue = new();
            }
        }
    }
}