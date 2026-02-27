using NSubstitute;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Incomes.Command.RestoreIncome;
using Expensify.Modules.Income.Domain.Incomes;
using IncomeEntity = Expensify.Modules.Income.Domain.Incomes.Income;

namespace Expensify.Modules.Income.UnitTests.Application.Incomes.Command.RestoreIncome;

[TestFixture]
internal sealed class RestoreIncomeCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenIncomeIsDeleted_ShouldRestoreAndSave()
    {
        IIncomeRepository repository = Substitute.For<IIncomeRepository>();
        IIncomeUnitOfWork unitOfWork = Substitute.For<IIncomeUnitOfWork>();
        RestoreIncomeCommandHandler sut = new(repository, unitOfWork);

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

        RestoreIncomeCommand command = new(userId, income.Id);

        repository.GetByIdIncludingDeletedAsync(command.IncomeId, Arg.Any<CancellationToken>()).Returns(income);

        Result result = await sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(income.DeletedAtUtc, Is.Null);
        }

        repository.Received(1).Update(income);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenIncomeIsNotDeleted_ShouldReturnFailure()
    {
        IIncomeRepository repository = Substitute.For<IIncomeRepository>();
        IIncomeUnitOfWork unitOfWork = Substitute.For<IIncomeUnitOfWork>();
        RestoreIncomeCommandHandler sut = new(repository, unitOfWork);

        var userId = Guid.NewGuid();
        IncomeEntity income = IncomeEntity.Create(
            userId,
            new Money(100m, "GBP"),
            new IncomeDate(new DateOnly(2026, 2, 28)),
            "ACME",
            IncomeType.Salary,
            "Monthly salary",
            "GBP").Value;

        RestoreIncomeCommand command = new(userId, income.Id);
        repository.GetByIdIncludingDeletedAsync(command.IncomeId, Arg.Any<CancellationToken>()).Returns(income);

        Result result = await sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
