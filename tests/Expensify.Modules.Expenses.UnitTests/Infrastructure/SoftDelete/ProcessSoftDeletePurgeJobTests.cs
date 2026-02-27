using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Quartz;
using Expensify.Common.Application.Clock;
using Expensify.Modules.Expenses.Domain.Categories;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Domain.Tags;
using Expensify.Modules.Expenses.Infrastructure.Database;
using Expensify.Modules.Expenses.Infrastructure.SoftDelete;

namespace Expensify.Modules.Expenses.UnitTests.Infrastructure.SoftDelete;

[TestFixture]
internal sealed class ProcessSoftDeletePurgeJobTests
{
    [Test]
    public async Task Execute_WhenSoftDeletedExpenseIsExpired_ShouldHardDeleteExpense()
    {
        DbContextOptions<ExpensesDbContext> dbOptions = new DbContextOptionsBuilder<ExpensesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var dbContext = new ExpensesDbContext(dbOptions);
        var userId = Guid.NewGuid();
        var category = ExpenseCategory.Create(userId, "Food");
        var tag = ExpenseTag.Create(userId, "Groceries");
        Expense expense = Expense.Create(
            userId,
            new Money(25m, "GBP"),
            new ExpenseDate(new DateOnly(2026, 2, 1)),
            category.Id,
            "Tesco",
            "Weekly",
            PaymentMethod.Card,
            [tag],
            "GBP").Value;

        _ = expense.MarkDeleted(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        dbContext.ExpenseCategories.Add(category);
        dbContext.ExpenseTags.Add(tag);
        dbContext.Expenses.Add(expense);
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

        int expenseCount = await dbContext.Expenses.IgnoreQueryFilters().CountAsync();
        Assert.That(expenseCount, Is.EqualTo(0));
    }
}
