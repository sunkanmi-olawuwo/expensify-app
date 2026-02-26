using NSubstitute;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Abstractions.Users;
using Expensify.Modules.Expenses.Application.Expenses;
using Expensify.Modules.Expenses.Application.Expenses.Command.UpdateExpense;
using Expensify.Modules.Expenses.Domain.Categories;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Domain.Tags;

namespace Expensify.Modules.Expenses.UnitTests.Application.Expenses.Command;

[TestFixture]
internal sealed class UpdateExpenseCommandHandlerTests
{
    private IExpenseRepository _expenseRepository = null!;
    private IExpenseCategoryRepository _categoryRepository = null!;
    private IExpenseTagRepository _tagRepository = null!;
    private IUserSettingsService _userSettingsService = null!;
    private IExpensesUnitOfWork _unitOfWork = null!;
    private UpdateExpenseCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _expenseRepository = Substitute.For<IExpenseRepository>();
        _categoryRepository = Substitute.For<IExpenseCategoryRepository>();
        _tagRepository = Substitute.For<IExpenseTagRepository>();
        _userSettingsService = Substitute.For<IUserSettingsService>();
        _unitOfWork = Substitute.For<IExpensesUnitOfWork>();
        _sut = new UpdateExpenseCommandHandler(
            _expenseRepository,
            _categoryRepository,
            _tagRepository,
            _userSettingsService,
            _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenTagIdsIsNull_ShouldTreatAsEmptyAndSucceed()
    {
        var userId = Guid.NewGuid();
        var category = ExpenseCategory.Create(userId, "Food");
        var originalTag = ExpenseTag.Create(userId, "Original");
        Result<Expense> expenseResult = Expense.Create(
            userId,
            new Money(12.00m, "GBP"),
            new ExpenseDate(new DateOnly(2026, 2, 1)),
            category.Id,
            "Merchant",
            "Initial",
            PaymentMethod.Card,
            [originalTag],
            "GBP");
        Expense expense = expenseResult.Value;

        var command = new UpdateExpenseCommand(
            userId,
            expense.Id,
            20.00m,
            "GBP",
            new DateOnly(2026, 2, 12),
            category.Id,
            "Updated Merchant",
            "Updated Note",
            PaymentMethod.Transfer,
            null!);

        _expenseRepository
            .GetByIdAsync(expense.Id, Arg.Any<CancellationToken>())
            .Returns(expense);
        _categoryRepository
            .GetByIdAsync(category.Id, Arg.Any<CancellationToken>())
            .Returns(category);
        _userSettingsService
            .GetSettingsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new UserSettingsResponse(userId, "GBP", "UTC", 1)));
        _tagRepository
            .GetByIdsAsync(userId, Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ExpenseTag>());

        Result<ExpenseResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.TagIds, Is.Empty);
            Assert.That(result.Value.PaymentMethod, Is.EqualTo(PaymentMethod.Transfer.ToString()));
        }

        await _tagRepository.Received(1)
            .GetByIdsAsync(
                userId,
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 0),
                Arg.Any<CancellationToken>());
    }
}
