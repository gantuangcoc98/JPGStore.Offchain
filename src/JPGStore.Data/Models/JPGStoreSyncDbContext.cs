using Cardano.Sync.Data;
using JPGStore.Data.Models.Reducers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace JPGStore.Data.Models;

public class JPGStoreSyncDbContext
(
    DbContextOptions options,
    IConfiguration configuration
) : CardanoDbContext(options, configuration)
{
    public DbSet<ListingByAddress> ListingsByAddress { get; set; }

    override protected void OnModelCreating(ModelBuilder modelBuilder)
    {   
        modelBuilder.Entity<ListingByAddress>(entity => {
            entity.HasKey(e => new { e.OwnerAddress, e.TxHash, e.Slot, e.TxIndex, e.UtxoStatus, e.Status });
            entity.OwnsOne(e => e.Amount);
            entity.OwnsOne(e => e.SellerPayoutValue);

            // indices
            entity.HasIndex(e => e.EstimatedFromListingValue);
            entity.HasIndex(e => e.EstimatedTotalListingValue);
            entity.HasIndex(e => e.TxHash);
            entity.HasIndex(e => e.TxIndex);
            entity.HasIndex(e => e.Slot);
            entity.HasIndex(e => e.UtxoStatus);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.OwnerAddress);
        });

        base.OnModelCreating(modelBuilder);
    }
}