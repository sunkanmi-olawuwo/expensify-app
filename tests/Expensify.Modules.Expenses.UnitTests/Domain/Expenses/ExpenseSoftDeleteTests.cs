using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Domain.Categories;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Domain.Tags;

namespace Expensify.Modules.Expenses.UnitTests.Domain.Expenses;

[TestFixture]
internal sealed class ExpenseSoftDeleteTests
{
    [Test]
    public void MarkDeleted_ThenRestore_ShouldToggleDeletedAtUtc()
    {
        var userId = Guid.NewGuid();
        var category = ExpenseCategory.Create(userId, "Food");
        Expense expense = Expense.Create(
            userId,
            new Money(25m, "GBP"),
            new ExpenseDate(new DateOnly(2026, 2, 28)),
            category.Id,
            "Tesco",
            "Weekly",
            PaymentMethod.Card,
            Array.Empty<ExpenseTag>(),
            "GBP").Value;

        var deletedAt = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);

        Result markDeletedResult = expense.MarkDeleted(deletedAt);
        Assert.That(markDeletedResult.IsSuccess, Is.True);
        Assert.That(expense.DeletedAtUtc, Is.EqualTo(deletedAt));

        Result restoreResult = expense.Restore();
        Assert.That(restoreResult.IsSuccess, Is.True);
        Assert.That(expense.DeletedAtUtc, Is.Null);
    }

    [Test]
    public void MarkDeleted_WhenAlreadyDeleted_ShouldReturnFailure()
    {
        var userId = Guid.NewGuid();
        var category = ExpenseCategory.Create(userId, "Food");
        Expense expense = Expense.Create(
            userId,
            new Money(25m, "GBP"),
            new ExpenseDate(new DateOnly(2026, 2, 28)),
            category.Id,
            "Tesco",
            "Weekly",
            PaymentMethod.Card,
            Array.Empty<ExpenseTag>(),
            "GBP").Value;

        var deletedAt = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);
        _ = expense.MarkDeleted(deletedAt);

        Result result = expense.MarkDeleted(deletedAt.AddDays(1));

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void Restore_WhenNotDeleted_ShouldReturnFailure()
    {
        var userId = Guid.NewGuid();
        var category = ExpenseCategory.Create(userId, "Food");
        Expense expense = Expense.Create(
            userId,
            new Money(25m, "GBP"),
            new ExpenseDate(new DateOnly(2026, 2, 28)),
            category.Id,
            "Tesco",
            "Weekly",
            PaymentMethod.Card,
            Array.Empty<ExpenseTag>(),
            "GBP").Value;

        Result result = expense.Restore();

        Assert.That(result.IsFailure, Is.True);
    }
}
