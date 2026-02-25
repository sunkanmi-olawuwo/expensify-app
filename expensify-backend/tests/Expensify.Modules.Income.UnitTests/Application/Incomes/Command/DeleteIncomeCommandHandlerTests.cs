using NSubstitute;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Incomes.Command.DeleteIncome;
using Expensify.Modules.Income.Domain.Incomes;
using IncomeEntity = Expensify.Modules.Income.Domain.Incomes.Income;

namespace Expensify.Modules.Income.UnitTests.Application.Incomes.Command;

[TestFixture]
internal sealed class DeleteIncomeCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenIncomeNotFound_ShouldReturnFailure()
    {
        IIncomeRepository repository = Substitute.For<IIncomeRepository>();
        IIncomeUnitOfWork unitOfWork = Substitute.For<IIncomeUnitOfWork>();
        DeleteIncomeCommandHandler sut = new(repository, unitOfWork);
        DeleteIncomeCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        repository.GetByIdAsync(command.IncomeId, Arg.Any<CancellationToken>()).Returns((IncomeEntity?)null);

        Result result = await sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
    }
}
