using NSubstitute;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Application.Abstractions.Users;
using Expensify.Modules.Investments.Application.Accounts.Command.UpdateInvestmentAccount;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Domain.Categories;
using Expensify.Modules.Investments.Domain.Contributions;
using InvestmentAccountEntity = Expensify.Modules.Investments.Domain.Accounts.InvestmentAccount;

namespace Expensify.Modules.Investments.UnitTests.Application.Accounts.Command;

[TestFixture]
internal sealed class UpdateInvestmentAccountCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenKeepingInactiveCategoryUnchanged_ShouldSucceedAndReturnTotalContributed()
    {
        IInvestmentAccountRepository accountRepository = Substitute.For<IInvestmentAccountRepository>();
        IInvestmentCategoryRepository categoryRepository = Substitute.For<IInvestmentCategoryRepository>();
        IInvestmentContributionRepository contributionRepository = Substitute.For<IInvestmentContributionRepository>();
        IUserSettingsService userSettingsService = Substitute.For<IUserSettingsService>();
        IInvestmentsUnitOfWork unitOfWork = Substitute.For<IInvestmentsUnitOfWork>();
        UpdateInvestmentAccountCommandHandler sut = new(accountRepository, categoryRepository, contributionRepository, userSettingsService, unitOfWork);

        var userId = Guid.NewGuid();
        InvestmentCategory category = CreateCategory(Guid.NewGuid(), "ISA", InvestmentCategorySlugs.Isa, false);
        InvestmentAccountEntity account = InvestmentAccountEntity.Create(
            userId,
            "ISA",
            "Provider",
            category.Id,
            "GBP",
            1.5m,
            null,
            100m,
            "note",
            category.Slug,
            "GBP").Value;

        UpdateInvestmentAccountCommand command = new(
            userId,
            account.Id,
            "ISA Updated",
            "Provider",
            category.Id,
            "GBP",
            1.25m,
            null,
            125m,
            "updated");

        accountRepository.GetByIdIncludingDeletedAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        userSettingsService.GetSettingsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new UserSettingsResponse("GBP", "UTC", 1)));
        contributionRepository.GetTotalContributedAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(375m);

        Result<InvestmentAccountResponse> result = await sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Name, Is.EqualTo("ISA Updated"));
            Assert.That(result.Value.TotalContributed, Is.EqualTo(375m));
        }

        accountRepository.Received(1).Update(account);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenChangingToInactiveCategory_ShouldReturnFailure()
    {
        IInvestmentAccountRepository accountRepository = Substitute.For<IInvestmentAccountRepository>();
        IInvestmentCategoryRepository categoryRepository = Substitute.For<IInvestmentCategoryRepository>();
        IInvestmentContributionRepository contributionRepository = Substitute.For<IInvestmentContributionRepository>();
        IUserSettingsService userSettingsService = Substitute.For<IUserSettingsService>();
        IInvestmentsUnitOfWork unitOfWork = Substitute.For<IInvestmentsUnitOfWork>();
        UpdateInvestmentAccountCommandHandler sut = new(accountRepository, categoryRepository, contributionRepository, userSettingsService, unitOfWork);

        var userId = Guid.NewGuid();
        InvestmentCategory currentCategory = CreateCategory(Guid.NewGuid(), "ISA", InvestmentCategorySlugs.Isa, true);
        InvestmentCategory newCategory = CreateCategory(Guid.NewGuid(), "Other", InvestmentCategorySlugs.Other, false);
        InvestmentAccountEntity account = InvestmentAccountEntity.Create(
            userId,
            "ISA",
            null,
            currentCategory.Id,
            "GBP",
            null,
            null,
            100m,
            null,
            currentCategory.Slug,
            "GBP").Value;

        UpdateInvestmentAccountCommand command = new(
            userId,
            account.Id,
            "Moved",
            null,
            newCategory.Id,
            "GBP",
            null,
            null,
            100m,
            null);

        accountRepository.GetByIdIncludingDeletedAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        categoryRepository.GetByIdAsync(newCategory.Id, Arg.Any<CancellationToken>()).Returns(newCategory);
        userSettingsService.GetSettingsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new UserSettingsResponse("GBP", "UTC", 1)));

        Result<InvestmentAccountResponse> result = await sut.Handle(command, CancellationToken.None);

        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo(InvestmentCategoryErrors.Inactive(newCategory.Id)));
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static InvestmentCategory CreateCategory(Guid id, string name, string slug, bool isActive)
    {
        return InvestmentCategory.Create(id, name, slug, isActive);
    }
}
