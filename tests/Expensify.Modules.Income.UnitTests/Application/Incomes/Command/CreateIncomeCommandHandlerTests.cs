using NSubstitute;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Abstractions.Users;
using Expensify.Modules.Income.Application.Incomes.Command.CreateIncome;
using Expensify.Modules.Income.Domain.Incomes;

namespace Expensify.Modules.Income.UnitTests.Application.Incomes.Command;

[TestFixture]
internal sealed class CreateIncomeCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenCommandIsValid_ShouldSucceed()
    {
        IIncomeRepository repository = Substitute.For<IIncomeRepository>();
        IUserSettingsService settingsService = Substitute.For<IUserSettingsService>();
        IIncomeUnitOfWork unitOfWork = Substitute.For<IIncomeUnitOfWork>();
        var sut = new CreateIncomeCommandHandler(repository, settingsService, unitOfWork);

        var userId = Guid.NewGuid();
        var command = new CreateIncomeCommand(userId, 100m, "GBP", new DateOnly(2026, 2, 28), "ACME", IncomeType.Salary, "Monthly salary");

        settingsService
            .GetSettingsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new UserSettingsResponse("GBP", "UTC", 1)));

        Result<IncomeResponse> result = await sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
