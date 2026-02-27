using NSubstitute;
using Expensify.Common.Application.Clock;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Expenses.Command.DeleteExpense;
using Expensify.Modules.Expenses.Domain.Categories;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Domain.Tags;

namespace Expensify.Modules.Expenses.UnitTests.Application.Expenses.Command;

[TestFixture]
internal sealed class DeleteExpenseCommandHandlerTests
{
    private IExpenseRepository _expenseRepository = null!;
    private IDateTimeProvider _dateTimeProvider = null!;
    private IExpensesUnitOfWork _unitOfWork = null!;
    private DeleteExpenseCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _expenseRepository = Substitute.For<IExpenseRepository>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _unitOfWork = Substitute.For<IExpensesUnitOfWork>();
        _sut = new DeleteExpenseCommandHandler(_expenseRepository, _dateTimeProvider, _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenExpenseNotFound_ShouldReturnFailure()
    {
        var command = new DeleteExpenseCommand(Guid.NewGuid(), Guid.NewGuid());

        _expenseRepository.GetByIdIncludingDeletedAsync(command.ExpenseId, Arg.Any<CancellationToken>())
            .Returns((Expense?)null);

        Result result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task Handle_WhenExpenseExists_ShouldSoftDeleteAndSave()
    {
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

        var command = new DeleteExpenseCommand(userId, expense.Id);
        var now = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);

        _dateTimeProvider.UtcNow.Returns(now);
        _expenseRepository.GetByIdIncludingDeletedAsync(command.ExpenseId, Arg.Any<CancellationToken>())
            .Returns(expense);

        Result result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(expense.DeletedAtUtc, Is.EqualTo(now));
        }

        _expenseRepository.Received(1).Update(expense);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenExpenseAlreadyDeleted_ShouldReturnFailure()
    {
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

        var command = new DeleteExpenseCommand(userId, expense.Id);
        _expenseRepository.GetByIdIncludingDeletedAsync(command.ExpenseId, Arg.Any<CancellationToken>())
            .Returns(expense);

        Result result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
