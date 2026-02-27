using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Quartz;
using Expensify.Common.Application.Clock;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Infrastructure.Database;

namespace Expensify.Modules.Expenses.Infrastructure.SoftDelete;

[DisallowConcurrentExecution]
internal sealed class ProcessSoftDeletePurgeJob(
    ExpensesDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IOptions<SoftDeleteOptions> softDeleteOptions,
    ILogger<ProcessSoftDeletePurgeJob> logger) : IJob
{
    private const string ModuleName = "Expenses";

    public async Task Execute(IJobExecutionContext context)
    {
        DateTime cutoff = dateTimeProvider.UtcNow.AddDays(-softDeleteOptions.Value.RetentionDays);

        List<Expense> expensesToPurge = await dbContext.Expenses
            .IgnoreQueryFilters()
            .Where(e => e.DeletedAtUtc.HasValue && e.DeletedAtUtc <= cutoff)
            .OrderBy(e => e.DeletedAtUtc)
            .Take(softDeleteOptions.Value.BatchSize)
            .ToListAsync(context.CancellationToken);

        if (expensesToPurge.Count == 0)
        {
            return;
        }

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(context.CancellationToken);

        foreach (Expense expense in expensesToPurge)
        {
            expense.RaiseDeletedEvent();
        }

        dbContext.Expenses.RemoveRange(expensesToPurge);
        await dbContext.SaveChangesAsync(context.CancellationToken);
        await transaction.CommitAsync(context.CancellationToken);

        logger.LogInformation("{Module} - Purged {DeletedRows} soft-deleted expenses", ModuleName, expensesToPurge.Count);
    }
}
