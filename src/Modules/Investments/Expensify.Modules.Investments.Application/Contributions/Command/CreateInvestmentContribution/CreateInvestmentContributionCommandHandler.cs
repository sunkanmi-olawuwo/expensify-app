using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Domain.Contributions;

namespace Expensify.Modules.Investments.Application.Contributions.Command.CreateInvestmentContribution;

internal sealed class CreateInvestmentContributionCommandHandler(
    IInvestmentAccountRepository investmentAccountRepository,
    IInvestmentContributionRepository investmentContributionRepository,
    IInvestmentsUnitOfWork unitOfWork)
    : ICommandHandler<CreateInvestmentContributionCommand, InvestmentContributionResponse>
{
    public async Task<Result<InvestmentContributionResponse>> Handle(CreateInvestmentContributionCommand request, CancellationToken cancellationToken)
    {
        InvestmentAccount? investment = await investmentAccountRepository.GetByIdAsync(request.InvestmentId, cancellationToken);
        if (investment is null || investment.UserId != request.UserId)
        {
            return Result.Failure<InvestmentContributionResponse>(InvestmentAccountErrors.NotFound(request.InvestmentId));
        }

        Result<InvestmentContribution> contributionResult = InvestmentContribution.Create(
            request.InvestmentId,
            request.Amount,
            request.Date,
            request.Notes);

        if (contributionResult.IsFailure)
        {
            return Result.Failure<InvestmentContributionResponse>(contributionResult.Error);
        }

        investmentContributionRepository.Add(contributionResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new InvestmentContributionResponse(
            contributionResult.Value.Id,
            contributionResult.Value.InvestmentId,
            contributionResult.Value.Amount,
            contributionResult.Value.Date,
            contributionResult.Value.Notes,
            contributionResult.Value.CreatedAtUtc);
    }
}
