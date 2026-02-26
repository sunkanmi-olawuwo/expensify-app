using NSubstitute;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Categories.Command.DeleteExpenseCategory;
using Expensify.Modules.Expenses.Domain.Categories;
using Expensify.Modules.Expenses.Domain.Expenses;

namespace Expensify.Modules.Expenses.UnitTests.Application.Categories.Command;

[TestFixture]
internal sealed class DeleteExpenseCategoryCommandHandlerTests
{
    private IExpenseCategoryRepository _categoryRepository = null!;
    private IExpenseRepository _expenseRepository = null!;
    private IExpensesUnitOfWork _unitOfWork = null!;
    private DeleteExpenseCategoryCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _categoryRepository = Substitute.For<IExpenseCategoryRepository>();
        _expenseRepository = Substitute.For<IExpenseRepository>();
        _unitOfWork = Substitute.For<IExpensesUnitOfWork>();
        _sut = new DeleteExpenseCategoryCommandHandler(_categoryRepository, _expenseRepository, _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenCategoryIsInUse_ShouldReturnValidationFailure()
    {
        var userId = Guid.NewGuid();
        var category = ExpenseCategory.Create(userId, "Food");
        var command = new DeleteExpenseCategoryCommand(userId, category.Id);

        _categoryRepository
            .GetByIdAsync(category.Id, Arg.Any<CancellationToken>())
            .Returns(category);
        _expenseRepository
            .ExistsByCategoryAsync(userId, category.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        Result result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo("Expenses.CategoryInUse"));
        }

        _categoryRepository.DidNotReceive().Remove(Arg.Any<ExpenseCategory>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
