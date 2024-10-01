
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using JPGStore.Data.Models.Reducers;

namespace JPGStore.Data.Models.Reducers;

public record ListingByAddress : Listing
{
    [NotMapped]
    public JPGStorePriceMapping ListingValue { get; set; } = default!;

    public string ListingValueJson
    {
        get => JsonSerializer.Serialize(ListingValue);
        set => ListingValue = JsonSerializer.Deserialize<JPGStorePriceMapping>(value) ?? throw new InvalidOperationException("Unable to deserialize.");
    }
}