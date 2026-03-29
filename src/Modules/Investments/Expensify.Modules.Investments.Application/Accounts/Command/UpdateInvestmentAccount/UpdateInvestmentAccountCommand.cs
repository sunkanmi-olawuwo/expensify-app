using Expensify.Common.Application.Messaging;
using Expensify.Modules.Investments.Application.Abstractions;

namespace Expensify.Modules.Investments.Application.Accounts.Command.UpdateInvestmentAccount;

public sealed record UpdateInvestmentAccountCommand(
    Guid UserId,
    Guid InvestmentId,
    string Name,
    string? Provider,
    Guid CategoryId,
    string Currency,
    decimal? InterestRate,
    DateTimeOffset? MaturityDate,
    decimal CurrentBalance,
    string? Notes) : ICommand<InvestmentAccountResponse>;
