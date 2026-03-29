using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Application.Abstractions.Users;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Domain.Categories;

namespace Expensify.Modules.Investments.Application.Accounts.Command.CreateInvestmentAccount;

internal sealed class CreateInvestmentAccountCommandHandler(
    IInvestmentAccountRepository investmentAccountRepository,
    IInvestmentCategoryRepository investmentCategoryRepository,
    IUserSettingsService userSettingsService,
    IInvestmentsUnitOfWork unitOfWork)
    : ICommandHandler<CreateInvestmentAccountCommand, InvestmentAccountResponse>
{
    public async Task<Result<InvestmentAccountResponse>> Handle(CreateInvestmentAccountCommand request, CancellationToken cancellationToken)
    {
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

        if (!category.IsActive)
        {
            return Result.Failure<InvestmentAccountResponse>(InvestmentCategoryErrors.Inactive(request.CategoryId));
        }

        Result<InvestmentAccount> investmentResult = InvestmentAccount.Create(
            request.UserId,
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

        if (investmentResult.IsFailure)
        {
            return Result.Failure<InvestmentAccountResponse>(investmentResult.Error);
        }

        investmentAccountRepository.Add(investmentResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new InvestmentAccountResponse(
            investmentResult.Value.Id,
            investmentResult.Value.UserId,
            investmentResult.Value.Name,
            investmentResult.Value.Provider,
            investmentResult.Value.CategoryId,
            category.Name,
            category.Slug,
            investmentResult.Value.Currency,
            investmentResult.Value.InterestRate,
            investmentResult.Value.MaturityDate,
            investmentResult.Value.CurrentBalance,
            investmentResult.Value.Notes,
            0m,
            investmentResult.Value.CreatedAtUtc,
            investmentResult.Value.UpdatedAtUtc);
    }
}
