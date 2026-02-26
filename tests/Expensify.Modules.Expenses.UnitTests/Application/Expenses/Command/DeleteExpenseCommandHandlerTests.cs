using NSubstitute;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Expenses.Command.DeleteExpense;
using Expensify.Modules.Expenses.Domain.Expenses;

namespace Expensify.Modules.Expenses.UnitTests.Application.Expenses.Command;

[TestFixture]
internal sealed class DeleteExpenseCommandHandlerTests
{
    private IExpenseRepository _expenseRepository = null!;
    private IExpensesUnitOfWork _unitOfWork = null!;
    private DeleteExpenseCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _expenseRepository = Substitute.For<IExpenseRepository>();
        _unitOfWork = Substitute.For<IExpensesUnitOfWork>();
        _sut = new DeleteExpenseCommandHandler(_expenseRepository, _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenExpenseNotFound_ShouldReturnFailure()
    {
        var command = new DeleteExpenseCommand(Guid.NewGuid(), Guid.NewGuid());

        _expenseRepository.GetByIdAsync(command.ExpenseId, Arg.Any<CancellationToken>())
            .Returns((Expense?)null);

        Result result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
    }
}
