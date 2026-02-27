using NSubstitute;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Expenses.Command.RestoreExpense;
using Expensify.Modules.Expenses.Domain.Categories;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Domain.Tags;

namespace Expensify.Modules.Expenses.UnitTests.Application.Expenses.Command.RestoreExpense;

[TestFixture]
internal sealed class RestoreExpenseCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenExpenseIsDeleted_ShouldRestoreAndSave()
    {
        IExpenseRepository repository = Substitute.For<IExpenseRepository>();
        IExpensesUnitOfWork unitOfWork = Substitute.For<IExpensesUnitOfWork>();
        RestoreExpenseCommandHandler sut = new(repository, unitOfWork);

        var userId = Guid.NewGuid();
        var category = ExpenseCategory.Create(userId, "Food");
        Result<Expense> createResult = Expense.Create(
            userId,
            new Money(25m, "GBP"),
            new ExpenseDate(new DateOnly(2026, 2, 28)),
            category.Id,
            "Tesco",
            "Weekly",
            PaymentMethod.Card,
            Array.Empty<ExpenseTag>(),
            "GBP");

        Expense expense = createResult.Value;
        Result markDeletedResult = expense.MarkDeleted(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc));
        Assert.That(markDeletedResult.IsSuccess, Is.True);

        RestoreExpenseCommand command = new(userId, expense.Id);

        repository.GetByIdIncludingDeletedAsync(command.ExpenseId, Arg.Any<CancellationToken>()).Returns(expense);

        Result result = await sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(expense.DeletedAtUtc, Is.Null);
        }

        repository.Received(1).Update(expense);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenExpenseIsNotDeleted_ShouldReturnFailure()
    {
        IExpenseRepository repository = Substitute.For<IExpenseRepository>();
        IExpensesUnitOfWork unitOfWork = Substitute.For<IExpensesUnitOfWork>();
        RestoreExpenseCommandHandler sut = new(repository, unitOfWork);

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

        RestoreExpenseCommand command = new(userId, expense.Id);
        repository.GetByIdIncludingDeletedAsync(command.ExpenseId, Arg.Any<CancellationToken>()).Returns(expense);

        Result result = await sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
