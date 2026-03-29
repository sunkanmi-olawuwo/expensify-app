using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Domain.Contributions;

namespace Expensify.Modules.Investments.Application.Accounts.Command.DeleteInvestmentAccount;

internal sealed class DeleteInvestmentAccountCommandHandler(
    IInvestmentAccountRepository investmentAccountRepository,
    IInvestmentContributionRepository investmentContributionRepository,
    IDateTimeProvider dateTimeProvider,
    IInvestmentsUnitOfWork unitOfWork)
    : ICommandHandler<DeleteInvestmentAccountCommand>
{
    public async Task<Result> Handle(DeleteInvestmentAccountCommand request, CancellationToken cancellationToken)
    {
        InvestmentAccount? investment = await investmentAccountRepository.GetByIdIncludingDeletedAsync(request.InvestmentId, cancellationToken);
        if (investment is null || investment.UserId != request.UserId)
        {
            return Result.Failure(InvestmentAccountErrors.NotFound(request.InvestmentId));
        }

        Result deleteResult = investment.MarkDeleted(dateTimeProvider.UtcNow);
        if (deleteResult.IsFailure)
        {
            return Result.Failure(InvestmentAccountErrors.NotFound(request.InvestmentId));
        }

        IReadOnlyCollection<InvestmentContribution> contributions =
            await investmentContributionRepository.GetByInvestmentIdIncludingDeletedAsync(investment.Id, cancellationToken);

        foreach (InvestmentContribution contribution in contributions)
        {
            contribution.MarkDeleted(dateTimeProvider.UtcNow);
            investmentContributionRepository.Update(contribution);
        }

        investmentAccountRepository.Update(investment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
