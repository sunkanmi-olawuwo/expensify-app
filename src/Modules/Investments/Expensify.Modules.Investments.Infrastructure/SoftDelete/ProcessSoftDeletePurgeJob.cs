using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Expensify.Common.Application.Clock;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Domain.Contributions;
using Expensify.Modules.Investments.Infrastructure.Database;

namespace Expensify.Modules.Investments.Infrastructure.SoftDelete;

[DisallowConcurrentExecution]
internal sealed class ProcessSoftDeletePurgeJob(
    InvestmentsDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IOptions<SoftDeleteOptions> softDeleteOptions,
    ILogger<ProcessSoftDeletePurgeJob> logger) : IJob
{
    private const string ModuleName = "Investments";

    public async Task Execute(IJobExecutionContext context)
    {
        DateTime cutoff = dateTimeProvider.UtcNow.AddDays(-softDeleteOptions.Value.RetentionDays);

        List<InvestmentContribution> contributionsToPurge = await dbContext.InvestmentContributions
            .IgnoreQueryFilters()
            .Where(c => c.DeletedAtUtc.HasValue && c.DeletedAtUtc <= cutoff)
            .OrderBy(c => c.DeletedAtUtc)
            .Take(softDeleteOptions.Value.BatchSize)
            .ToListAsync(context.CancellationToken);

        int remainingBatchSize = Math.Max(softDeleteOptions.Value.BatchSize - contributionsToPurge.Count, 0);

        // Only purge accounts whose contributions have all been purged already.
        // This prevents the FK cascade from bypassing domain events on contributions.
        List<InvestmentAccount> accountsToPurge = remainingBatchSize == 0
            ? []
            : await dbContext.InvestmentAccounts
                .IgnoreQueryFilters()
                .Where(a => a.DeletedAtUtc.HasValue && a.DeletedAtUtc <= cutoff)
                .Where(a => !dbContext.InvestmentContributions
                    .IgnoreQueryFilters()
                    .Any(c => c.InvestmentId == a.Id))
                .OrderBy(a => a.DeletedAtUtc)
                .Take(remainingBatchSize)
                .ToListAsync(context.CancellationToken);

        if (contributionsToPurge.Count == 0 && accountsToPurge.Count == 0)
        {
            return;
        }

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(context.CancellationToken);

        dbContext.InvestmentContributions.RemoveRange(contributionsToPurge);

        foreach (InvestmentAccount account in accountsToPurge)
        {
            account.RaiseDeletedEvent();
        }

        dbContext.InvestmentAccounts.RemoveRange(accountsToPurge);
        await dbContext.SaveChangesAsync(context.CancellationToken);
        await transaction.CommitAsync(context.CancellationToken);

        logger.LogInformation(
            "{Module} - Purged {DeletedContributions} soft-deleted contributions and {DeletedAccounts} soft-deleted accounts",
            ModuleName,
            contributionsToPurge.Count,
            accountsToPurge.Count);
    }
}
