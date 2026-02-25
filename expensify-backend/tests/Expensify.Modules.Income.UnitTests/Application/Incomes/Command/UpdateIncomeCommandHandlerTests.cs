using NSubstitute;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Abstractions.Users;
using Expensify.Modules.Income.Application.Incomes.Command.UpdateIncome;
using Expensify.Modules.Income.Domain.Incomes;
using IncomeEntity = Expensify.Modules.Income.Domain.Incomes.Income;

namespace Expensify.Modules.Income.UnitTests.Application.Incomes.Command;

[TestFixture]
internal sealed class UpdateIncomeCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenIncomeNotFound_ShouldReturnFailure()
    {
        IIncomeRepository repository = Substitute.For<IIncomeRepository>();
        IUserSettingsService settingsService = Substitute.For<IUserSettingsService>();
        IIncomeUnitOfWork unitOfWork = Substitute.For<IIncomeUnitOfWork>();
        UpdateIncomeCommandHandler sut = new(repository, settingsService, unitOfWork);

        UpdateIncomeCommand command = new(Guid.NewGuid(), Guid.NewGuid(), 120m, "GBP", new DateOnly(2026, 2, 28), "Client", IncomeType.Freelance, "note");

        repository.GetByIdAsync(command.IncomeId, Arg.Any<CancellationToken>()).Returns((IncomeEntity?)null);

        Result<IncomeResponse> result = await sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
    }
}
