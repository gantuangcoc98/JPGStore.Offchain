
using System.Net.Http.Headers;
using JPGStore.Data.Models;
using JPGStore.Data.Models.Common;
using JPGStore.Data.Models.Reducers;
using Microsoft.EntityFrameworkCore;

namespace JPGStore.Sync.Workers;

public class VirtualMempoolWorker(
    IDbContextFactory<JPGStoreSyncDbContext> _dbContextFactory,
    IConfiguration _configuration,
    ILogger<VirtualMempoolWorker> _logger
) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int confirmationCount = _configuration.GetValue<int>("VirtualMempoolWorkerConfirmationCount");
        while (!stoppingToken.IsCancellationRequested)
        {
            using JPGStoreSyncDbContext _dbContext = _dbContextFactory.CreateDbContext();

            // Fetch current slot
            ulong currentSlot = await _dbContext.Blocks
                .OrderByDescending(b => b.Slot)
                .Select(b => b.Slot)
                .FirstOrDefaultAsync(stoppingToken);

            ulong removalSlot = currentSlot - (ulong)confirmationCount;

            IQueryable<TransactionHistory> txHistories = _dbContext.TransactionHistories
                .AsNoTracking()
                .Where(t => t.Slot <= removalSlot)
                .GroupJoin(
                    _dbContext.ListingsByAddress,
                    t => t.FromTxHash,
                    l => l.TxHash,
                    (t, lGroupTx) => new { Transaction = t, ListingsByTxHash = lGroupTx }
                )
                .SelectMany(
                    x => x.ListingsByTxHash.DefaultIfEmpty(),
                    (x, l) => new { x.Transaction, ListingByTxHash = l }
                )
                .GroupJoin(
                    _dbContext.ListingsByAddress,
                    x => x.Transaction.FromTxHash,
                    l => l.SpentTxHash,
                    (x, lGroupSpent) => new { x.Transaction, x.ListingByTxHash, ListingsBySpentTxHash = lGroupSpent }
                )
                .SelectMany(
                    x => x.ListingsBySpentTxHash.DefaultIfEmpty(),
                    (x, l) => new { x.Transaction, x.ListingByTxHash, ListingBySpentTxHash = l }
                )
                .Where(x => x.ListingByTxHash == null && x.ListingBySpentTxHash == null)
                .Select(x => x.Transaction);

            _dbContext.TransactionHistories.RemoveRange(txHistories);

            // Save changes
            await _dbContext.SaveChangesAsync(stoppingToken);
            await _dbContext.DisposeAsync();

            await Task.Delay(20000, stoppingToken);
            _logger.LogInformation("Virtual Mempool Worker is running");
        }
    }
}