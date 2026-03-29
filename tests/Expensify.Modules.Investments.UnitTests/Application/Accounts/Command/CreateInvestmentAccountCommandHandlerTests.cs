using NSubstitute;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Application.Abstractions.Users;
using Expensify.Modules.Investments.Application.Accounts.Command.CreateInvestmentAccount;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Domain.Categories;

namespace Expensify.Modules.Investments.UnitTests.Application.Accounts.Command;

[TestFixture]
internal sealed class CreateInvestmentAccountCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenCommandIsValid_ShouldPersistAccount()
    {
        IInvestmentAccountRepository accountRepository = Substitute.For<IInvestmentAccountRepository>();
        IInvestmentCategoryRepository categoryRepository = Substitute.For<IInvestmentCategoryRepository>();
        IUserSettingsService userSettingsService = Substitute.For<IUserSettingsService>();
        IInvestmentsUnitOfWork unitOfWork = Substitute.For<IInvestmentsUnitOfWork>();
        CreateInvestmentAccountCommandHandler sut = new(accountRepository, categoryRepository, userSettingsService, unitOfWork);

        var userId = Guid.NewGuid();
        InvestmentCategory category = CreateCategory(Guid.NewGuid(), "ISA", InvestmentCategorySlugs.Isa, true);
        CreateInvestmentAccountCommand command = new(
            userId,
            "ISA Account",
            "Vanguard",
            category.Id,
            "GBP",
            2.5m,
            null,
            250m,
            "Notes");

        userSettingsService.GetSettingsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new UserSettingsResponse("GBP", "UTC", 1)));
        categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>())
            .Returns(category);

        Result<InvestmentAccountResponse> result = await sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Currency, Is.EqualTo("GBP"));
            Assert.That(result.Value.TotalContributed, Is.EqualTo(0m));
        }

        accountRepository.Received(1).Add(Arg.Any<InvestmentAccount>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenCategoryIsInactive_ShouldReturnFailure()
    {
        IInvestmentAccountRepository accountRepository = Substitute.For<IInvestmentAccountRepository>();
        IInvestmentCategoryRepository categoryRepository = Substitute.For<IInvestmentCategoryRepository>();
        IUserSettingsService userSettingsService = Substitute.For<IUserSettingsService>();
        IInvestmentsUnitOfWork unitOfWork = Substitute.For<IInvestmentsUnitOfWork>();
        CreateInvestmentAccountCommandHandler sut = new(accountRepository, categoryRepository, userSettingsService, unitOfWork);

        var userId = Guid.NewGuid();
        InvestmentCategory category = CreateCategory(Guid.NewGuid(), "ISA", InvestmentCategorySlugs.Isa, false);
        CreateInvestmentAccountCommand command = new(
            userId,
            "ISA Account",
            null,
            category.Id,
            "GBP",
            null,
            null,
            100m,
            null);

        userSettingsService.GetSettingsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new UserSettingsResponse("GBP", "UTC", 1)));
        categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>())
            .Returns(category);

        Result<InvestmentAccountResponse> result = await sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo(InvestmentCategoryErrors.Inactive(category.Id)));
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static InvestmentCategory CreateCategory(Guid id, string name, string slug, bool isActive)
    {
        return InvestmentCategory.Create(id, name, slug, isActive);
    }
}
