using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Quartz;
using Expensify.Common.Application.Clock;
using Expensify.Modules.Income.Infrastructure.Database;
using IncomeEntity = Expensify.Modules.Income.Domain.Incomes.Income;

namespace Expensify.Modules.Income.Infrastructure.SoftDelete;

[DisallowConcurrentExecution]
internal sealed class ProcessSoftDeletePurgeJob(
    IncomeDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IOptions<SoftDeleteOptions> softDeleteOptions,
    ILogger<ProcessSoftDeletePurgeJob> logger) : IJob
{
    private const string ModuleName = "Income";

    public async Task Execute(IJobExecutionContext context)
    {
        DateTime cutoff = dateTimeProvider.UtcNow.AddDays(-softDeleteOptions.Value.RetentionDays);

        List<IncomeEntity> incomesToPurge = await dbContext.Incomes
            .IgnoreQueryFilters()
            .Where(i => i.DeletedAtUtc.HasValue && i.DeletedAtUtc <= cutoff)
            .OrderBy(i => i.DeletedAtUtc)
            .Take(softDeleteOptions.Value.BatchSize)
            .ToListAsync(context.CancellationToken);

        if (incomesToPurge.Count == 0)
        {
            return;
        }

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(context.CancellationToken);

        foreach (IncomeEntity income in incomesToPurge)
        {
            income.RaiseDeletedEvent();
        }

        dbContext.Incomes.RemoveRange(incomesToPurge);
        await dbContext.SaveChangesAsync(context.CancellationToken);
        await transaction.CommitAsync(context.CancellationToken);

        logger.LogInformation("{Module} - Purged {DeletedRows} soft-deleted incomes", ModuleName, incomesToPurge.Count);
    }
}
