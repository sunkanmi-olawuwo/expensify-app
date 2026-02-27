using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Quartz;
using Expensify.Common.Application.Clock;
using Expensify.Modules.Income.Domain.Incomes;
using Expensify.Modules.Income.Infrastructure.Database;
using Expensify.Modules.Income.Infrastructure.SoftDelete;
using IncomeEntity = Expensify.Modules.Income.Domain.Incomes.Income;

namespace Expensify.Modules.Income.UnitTests.Infrastructure.SoftDelete;

[TestFixture]
internal sealed class ProcessSoftDeletePurgeJobTests
{
    [Test]
    public async Task Execute_WhenSoftDeletedIncomeIsExpired_ShouldHardDeleteIncome()
    {
        DbContextOptions<IncomeDbContext> dbOptions = new DbContextOptionsBuilder<IncomeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var dbContext = new IncomeDbContext(dbOptions);

        var userId = Guid.NewGuid();
        IncomeEntity income = IncomeEntity.Create(
            userId,
            new Money(100m, "GBP"),
            new IncomeDate(new DateOnly(2026, 2, 1)),
            "ACME",
            IncomeType.Salary,
            "Monthly salary",
            "GBP").Value;

        _ = income.MarkDeleted(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        dbContext.Incomes.Add(income);
        await dbContext.SaveChangesAsync();

        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc));

        IOptions<SoftDeleteOptions> options = Options.Create(new SoftDeleteOptions
        {
            RetentionDays = 30,
            BatchSize = 100,
            IntervalInSeconds = 300
        });

        ILogger<ProcessSoftDeletePurgeJob> logger = NullLogger<ProcessSoftDeletePurgeJob>.Instance;
        var job = new ProcessSoftDeletePurgeJob(dbContext, dateTimeProvider, options, logger);

        await job.Execute(Substitute.For<IJobExecutionContext>());

        int incomeCount = await dbContext.Incomes.IgnoreQueryFilters().CountAsync();
        Assert.That(incomeCount, Is.EqualTo(0));
    }
}
