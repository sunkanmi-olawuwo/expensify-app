using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Application.Abstractions.Users;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Domain.Categories;
using Expensify.Modules.Investments.Domain.Contributions;

namespace Expensify.Modules.Investments.Application.Accounts.Command.UpdateInvestmentAccount;

internal sealed class UpdateInvestmentAccountCommandHandler(
    IInvestmentAccountRepository investmentAccountRepository,
    IInvestmentCategoryRepository investmentCategoryRepository,
    IInvestmentContributionRepository investmentContributionRepository,
    IUserSettingsService userSettingsService,
    IInvestmentsUnitOfWork unitOfWork)
    : ICommandHandler<UpdateInvestmentAccountCommand, InvestmentAccountResponse>
{
    public async Task<Result<InvestmentAccountResponse>> Handle(UpdateInvestmentAccountCommand request, CancellationToken cancellationToken)
    {
        InvestmentAccount? investment = await investmentAccountRepository.GetByIdIncludingDeletedAsync(request.InvestmentId, cancellationToken);
        if (investment is null || investment.UserId != request.UserId || investment.IsDeleted)
        {
            return Result.Failure<InvestmentAccountResponse>(InvestmentAccountErrors.NotFound(request.InvestmentId));
        }

        Result<UserSettingsResponse> userSettingsResult = await userSettingsService.GetSettingsAsync(request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<InvestmentAccountResponse>(userSettingsResult.Error);
        }

        InvestmentCategory? category = await investmentCategoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure<InvestmentAccountResponse>(InvestmentCategoryErrors.NotFound(request.CategoryId));
        }

        bool categoryChanged = category.Id != investment.CategoryId;
        if (!category.IsActive && categoryChanged)
        {
            return Result.Failure<InvestmentAccountResponse>(InvestmentCategoryErrors.Inactive(request.CategoryId));
        }

        Result updateResult = investment.Update(
            request.Name,
            request.Provider,
            request.CategoryId,
            request.Currency,
            request.InterestRate,
            request.MaturityDate,
            request.CurrentBalance,
            request.Notes,
            category.Slug,
            userSettingsResult.Value.Currency);

        if (updateResult.IsFailure)
        {
            return Result.Failure<InvestmentAccountResponse>(updateResult.Error);
        }

        investmentAccountRepository.Update(investment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        decimal totalContributed = await investmentContributionRepository.GetTotalContributedAsync(investment.Id, cancellationToken);

        return new InvestmentAccountResponse(
            investment.Id,
            investment.UserId,
            investment.Name,
            investment.Provider,
            investment.CategoryId,
            category.Name,
            category.Slug,
            investment.Currency,
            investment.InterestRate,
            investment.MaturityDate,
            investment.CurrentBalance,
            investment.Notes,
            totalContributed,
            investment.CreatedAtUtc,
            investment.UpdatedAtUtc);
    }
}
