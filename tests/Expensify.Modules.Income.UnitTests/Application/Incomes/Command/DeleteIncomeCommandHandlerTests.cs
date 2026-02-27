using NSubstitute;
using Expensify.Common.Application.Clock;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Incomes.Command.DeleteIncome;
using Expensify.Modules.Income.Domain.Incomes;
using IncomeEntity = Expensify.Modules.Income.Domain.Incomes.Income;

namespace Expensify.Modules.Income.UnitTests.Application.Incomes.Command;

[TestFixture]
internal sealed class DeleteIncomeCommandHandlerTests
{
    private IIncomeRepository _repository = null!;
    private IIncomeUnitOfWork _unitOfWork = null!;
    private IDateTimeProvider _dateTimeProvider = null!;
    private DeleteIncomeCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IIncomeRepository>();
        _unitOfWork = Substitute.For<IIncomeUnitOfWork>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _sut = new DeleteIncomeCommandHandler(_repository, _dateTimeProvider, _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenIncomeNotFound_ShouldReturnFailure()
    {
        DeleteIncomeCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        _repository.GetByIdIncludingDeletedAsync(command.IncomeId, Arg.Any<CancellationToken>()).Returns((IncomeEntity?)null);

        Result result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task Handle_WhenIncomeExists_ShouldSoftDeleteAndSave()
    {
        var userId = Guid.NewGuid();
        Result<IncomeEntity> createResult = IncomeEntity.Create(
            userId,
            new Money(100m, "GBP"),
            new IncomeDate(new DateOnly(2026, 2, 28)),
            "ACME",
            IncomeType.Salary,
            "Monthly salary",
            "GBP");

        IncomeEntity income = createResult.Value;
        DeleteIncomeCommand command = new(userId, income.Id);
        var now = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);

        _dateTimeProvider.UtcNow.Returns(now);
        _repository.GetByIdIncludingDeletedAsync(command.IncomeId, Arg.Any<CancellationToken>()).Returns(income);

        Result result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(income.DeletedAtUtc, Is.EqualTo(now));
        }

        _repository.Received(1).Update(income);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenIncomeAlreadyDeleted_ShouldReturnFailure()
    {
        var userId = Guid.NewGuid();
        Result<IncomeEntity> createResult = IncomeEntity.Create(
            userId,
            new Money(100m, "GBP"),
            new IncomeDate(new DateOnly(2026, 2, 28)),
            "ACME",
            IncomeType.Salary,
            "Monthly salary",
            "GBP");
        IncomeEntity income = createResult.Value;
        Result markDeletedResult = income.MarkDeleted(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc));
        Assert.That(markDeletedResult.IsSuccess, Is.True);

        DeleteIncomeCommand command = new(userId, income.Id);
        _repository.GetByIdIncludingDeletedAsync(command.IncomeId, Arg.Any<CancellationToken>()).Returns(income);

        Result result = await _sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
