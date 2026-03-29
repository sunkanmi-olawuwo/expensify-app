using NSubstitute;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Application.Contributions.Command.CreateInvestmentContribution;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Domain.Categories;
using Expensify.Modules.Investments.Domain.Contributions;
using InvestmentAccountEntity = Expensify.Modules.Investments.Domain.Accounts.InvestmentAccount;

namespace Expensify.Modules.Investments.UnitTests.Application.Contributions.Command;

[TestFixture]
internal sealed class CreateInvestmentContributionCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenInvestmentExists_ShouldPersistContribution()
    {
        IInvestmentAccountRepository accountRepository = Substitute.For<IInvestmentAccountRepository>();
        IInvestmentContributionRepository contributionRepository = Substitute.For<IInvestmentContributionRepository>();
        IInvestmentsUnitOfWork unitOfWork = Substitute.For<IInvestmentsUnitOfWork>();
        CreateInvestmentContributionCommandHandler sut = new(accountRepository, contributionRepository, unitOfWork);

        var userId = Guid.NewGuid();
        InvestmentAccountEntity account = InvestmentAccountEntity.Create(
            userId,
            "ISA",
            "Provider",
            Guid.NewGuid(),
            "GBP",
            null,
            null,
            100m,
            null,
            InvestmentCategorySlugs.Isa,
            "GBP").Value;

        accountRepository.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        Result<InvestmentContributionResponse> result = await sut.Handle(
            new CreateInvestmentContributionCommand(userId, account.Id, 50m, DateTimeOffset.UtcNow, "monthly"),
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Amount, Is.EqualTo(50m));
            Assert.That(result.Value.InvestmentId, Is.EqualTo(account.Id));
        }

        contributionRepository.Received(1).Add(Arg.Any<InvestmentContribution>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
