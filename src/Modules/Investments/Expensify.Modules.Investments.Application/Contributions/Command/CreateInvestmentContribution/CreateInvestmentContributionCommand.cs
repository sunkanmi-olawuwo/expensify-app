using Expensify.Common.Application.Messaging;
using Expensify.Modules.Investments.Application.Abstractions;

namespace Expensify.Modules.Investments.Application.Contributions.Command.CreateInvestmentContribution;

public sealed record CreateInvestmentContributionCommand(
    Guid UserId,
    Guid InvestmentId,
    decimal Amount,
    DateTimeOffset Date,
    string? Notes) : ICommand<InvestmentContributionResponse>;
