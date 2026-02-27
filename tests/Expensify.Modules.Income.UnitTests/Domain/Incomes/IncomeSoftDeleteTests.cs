using Expensify.Common.Domain;
using Expensify.Modules.Income.Domain.Incomes;
using IncomeEntity = Expensify.Modules.Income.Domain.Incomes.Income;

namespace Expensify.Modules.Income.UnitTests.Domain.Incomes;

[TestFixture]
internal sealed class IncomeSoftDeleteTests
{
    [Test]
    public void MarkDeleted_ThenRestore_ShouldToggleDeletedAtUtc()
    {
        var userId = Guid.NewGuid();
        IncomeEntity income = IncomeEntity.Create(
            userId,
            new Money(100m, "GBP"),
            new IncomeDate(new DateOnly(2026, 2, 28)),
            "ACME",
            IncomeType.Salary,
            "Monthly salary",
            "GBP").Value;

        var deletedAt = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);

        Result markDeletedResult = income.MarkDeleted(deletedAt);
        Assert.That(markDeletedResult.IsSuccess, Is.True);
        Assert.That(income.DeletedAtUtc, Is.EqualTo(deletedAt));

        Result restoreResult = income.Restore();
        Assert.That(restoreResult.IsSuccess, Is.True);
        Assert.That(income.DeletedAtUtc, Is.Null);
    }

    [Test]
    public void MarkDeleted_WhenAlreadyDeleted_ShouldReturnFailure()
    {
        var userId = Guid.NewGuid();
        IncomeEntity income = IncomeEntity.Create(
            userId,
            new Money(100m, "GBP"),
            new IncomeDate(new DateOnly(2026, 2, 28)),
            "ACME",
            IncomeType.Salary,
            "Monthly salary",
            "GBP").Value;

        var deletedAt = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);
        _ = income.MarkDeleted(deletedAt);

        Result result = income.MarkDeleted(deletedAt.AddDays(1));

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Restore_WhenNotDeleted_ShouldReturnFailure()
    {
        var userId = Guid.NewGuid();
        IncomeEntity income = IncomeEntity.Create(
            userId,
            new Money(100m, "GBP"),
            new IncomeDate(new DateOnly(2026, 2, 28)),
            "ACME",
            IncomeType.Salary,
            "Monthly salary",
            "GBP").Value;

        Result result = income.Restore();

        Assert.That(result.IsFailure, Is.True);
    }
}
