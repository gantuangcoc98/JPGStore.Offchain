using System.Transactions;
using Cardano.Sync.Data.Models.Datums;
using JPGStore.Data.Models.Reducers;

namespace JPGStore.Data.Models.Common;

public enum TransactionType
{
    Offer,
    AcceptOffer,
    CreateListing,
    BuyListing,
    CancelListing,
    UpdateListing,
    ReceiveOffer
}

public enum TransactionStatus
{
    Pending,
    Confirmed,
    Failed,
}

public record TransactionHistory
{
    public TransactionType Type { get; init; }
    public TransactionStatus Status { get; init; } = TransactionStatus.Pending;
    public string FromAddress { get; init; } = default!;
    public string FromTxHash { get; init; } = default!;
    public string? ToAddress { get; init; }
    public string? ToTxHash { get; init; } = default!;
    public ulong Slot { get; init; } = default!;
}