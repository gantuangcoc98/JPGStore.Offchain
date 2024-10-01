using Cardano.Sync.Data;
using JPGStore.Data.Models.Reducers;
using JPGStore.Data.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace JPGStore.Data.Models;

public class JPGStoreSyncDbContext
(
    DbContextOptions<JPGStoreSyncDbContext> options,
    IConfiguration configuration
) : CardanoDbContext(options, configuration)
{
    public DbSet<JPGStoreNftPrice> JPGStoreNftPrices { get; set; }
    public DbSet<TokenPrice> TokenPrices { get; set; }
    public DbSet<CollectionSummary> CollectionSummaries { get; set; }
    public DbSet<NftByAddress> NftsByAddress { get; set; }
    public DbSet<ListingByAsset> ListingsByAsset { get; set; }
    public DbSet<TransactionHistory> TransactionHistories { get; set; }
    public DbSet<ListingByAddress> ListingsByAddress { get; set; }

    override protected void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ListingByAddress>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.OwnerAddress, e.TxHash, e.Slot, e.TxIndex, e.UtxoStatus, e.Status });
            entity.OwnsOne(e => e.Amount);
            entity.OwnsOne(e => e.SellerPayoutValue);

            // indices
            entity.HasIndex(e => e.Id);
            entity.HasIndex(e => e.EstimatedFromListingValue);
            entity.HasIndex(e => e.EstimatedTotalListingValue);
            entity.HasIndex(e => e.TxHash);
            entity.HasIndex(e => e.TxIndex);
            entity.HasIndex(e => e.Slot);
            entity.HasIndex(e => e.UtxoStatus);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.OwnerAddress);
        });

        modelBuilder.Entity<ListingByAsset>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.PolicyId, e.AssetName, e.TxHash, e.Slot, e.TxIndex, e.UtxoStatus, e.Status, e.ListingType });
            entity.OwnsOne(e => e.Amount);
            entity.OwnsOne(e => e.SellerPayoutValue);

            // indices
            entity.HasIndex(e => e.Id);
            entity.HasIndex(e => e.BuyerAddress);
            entity.HasIndex(e => e.EstimatedFromListingValue);
            entity.HasIndex(e => e.EstimatedTotalListingValue);
            entity.HasIndex(e => e.TxHash);
            entity.HasIndex(e => e.TxIndex);
            entity.HasIndex(e => e.Slot);
            entity.HasIndex(e => e.UtxoStatus);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.OwnerAddress);
            entity.HasIndex(e => e.ListingType);
            entity.HasIndex(e => e.PolicyId);
            entity.HasIndex(e => e.AssetName);

        });

        modelBuilder.Entity<TransactionHistory>(entity =>
        {
            entity.HasKey(e => new { e.FromAddress, e.FromTxHash });

            // indices
            entity.HasIndex(e => e.ToTxHash);
            entity.HasIndex(e => e.FromTxHash);
            entity.HasIndex(e => e.ToAddress);
            entity.HasIndex(e => e.FromAddress);
            entity.HasIndex(e => e.Slot);
        });

        modelBuilder.Entity<NftByAddress>(entity =>
        {
            entity.HasKey(e => new { e.TxHash, e.OutputIndex, e.Slot, e.PolicyId, e.AssetName, e.UtxoStatus, e.Address });

            // indices
            entity.HasIndex(e => e.PolicyId);
            entity.HasIndex(e => e.AssetName);
            entity.HasIndex(e => e.Slot);
            entity.HasIndex(e => e.Address);
        });

        modelBuilder.Entity<CollectionSummary>(entity =>
        {
            entity.HasKey(e => new { e.PolicyId, e.Slot });

            // indices
            entity.HasIndex(e => e.PolicyId);
            entity.HasIndex(e => e.Slot);
            entity.HasIndex(e => e.Volume);
            entity.HasIndex(e => e.Floor);
            entity.HasIndex(e => e.UniqueOwners);
        });

        modelBuilder.Entity<JPGStoreNftPrice>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.PolicyId, e.AssetName, e.TxHash, e.TxIndex, e.Slot });

            // indices
            entity.HasIndex(e => e.Id);
            entity.HasIndex(e => e.PolicyId);
            entity.HasIndex(e => e.AssetName);
            entity.HasIndex(e => e.Slot);
        });

        modelBuilder.Entity<TokenPrice>(entity => 
        {
            entity.HasKey(e => new { e.Id, e.PolicyId, e.AssetName, e.AsciiAssetName, e.TxHash, e.TxIndex, e.Slot });

            // indices
            entity.HasIndex(e => e.Id);
            entity.HasIndex(e => e.PolicyId);
            entity.HasIndex(e => e.AssetName);
            entity.HasIndex(e => e.AsciiAssetName);
            entity.HasIndex(e => e.TxHash);
            entity.HasIndex(e => e.TxIndex);
            entity.HasIndex(e => e.Slot);
        });

        base.OnModelCreating(modelBuilder);
    }
}