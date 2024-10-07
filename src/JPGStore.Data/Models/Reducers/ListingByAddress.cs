using Cardano.Sync.Data.Models;
using JPGStore.Data.Models.Enums;
namespace JPGStore.Data.Models.Reducers;

public record ListingByAddress
{
    public string OwnerAddress { get; set; } = default!;
    public string TxHash { get; set; } = default!;
    public ulong TxIndex { get; set; }
    public ulong Slot { get; set; }
    public Value Amount { get; set; } = default!;
    public UtxoStatus UtxoStatus { get; set; }
    public ListingStatus Status { get; set; }
    public ulong EstimatedTotalListingValue { get; set; } = 0;
    public ulong EstimatedFromListingValue { get; set; } = 0;
    public ulong EstimatedToListingValue { get; set; } = 0;

    // Optional fields populated when listing is consumed
    public string? BuyerAddress { get; set; }
    public string? SpentTxHash { get; set; }
    public Value? SellerPayoutValue { get; set; }

    public byte[] ListingDatumCbor { get; set; } = default!;
}