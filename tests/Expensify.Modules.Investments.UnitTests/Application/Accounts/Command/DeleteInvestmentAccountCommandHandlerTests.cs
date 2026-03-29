using NSubstitute;
using Expensify.Common.Application.Clock;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Application.Accounts.Command.DeleteInvestmentAccount;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Domain.Categories;
using Expensify.Modules.Investments.Domain.Contributions;
using InvestmentAccountEntity = Expensify.Modules.Investments.Domain.Accounts.InvestmentAccount;

namespace Expensify.Modules.Investments.UnitTests.Application.Accounts.Command;

[TestFixture]
internal sealed class DeleteInvestmentAccountCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenInvestmentExists_ShouldSoftDeleteAccountAndContributions()
    {
        IInvestmentAccountRepository accountRepository = Substitute.For<IInvestmentAccountRepository>();
        IInvestmentContributionRepository contributionRepository = Substitute.For<IInvestmentContributionRepository>();
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        IInvestmentsUnitOfWork unitOfWork = Substitute.For<IInvestmentsUnitOfWork>();
        DeleteInvestmentAccountCommandHandler sut = new(accountRepository, contributionRepository, dateTimeProvider, unitOfWork);

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
        InvestmentContribution contribution = InvestmentContribution.Create(account.Id, 25m, DateTimeOffset.UtcNow, "note").Value;
        DateTime now = new(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc);

        accountRepository.GetByIdIncludingDeletedAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        contributionRepository.GetByInvestmentIdIncludingDeletedAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns([contribution]);
        dateTimeProvider.UtcNow.Returns(now);

        Result result = await sut.Handle(new DeleteInvestmentAccountCommand(userId, account.Id), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(account.DeletedAtUtc, Is.EqualTo(now));
            Assert.That(contribution.DeletedAtUtc, Is.EqualTo(now));
        }

        accountRepository.Received(1).Update(account);
        contributionRepository.Received(1).Update(contribution);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
