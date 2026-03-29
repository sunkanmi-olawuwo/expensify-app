using Expensify.Common.Application.Messaging;
using Expensify.Modules.Investments.Application.Abstractions;

namespace Expensify.Modules.Investments.Application.Accounts.Command.CreateInvestmentAccount;

public sealed record CreateInvestmentAccountCommand(
    Guid UserId,
    string Name,
    string? Provider,
    Guid CategoryId,
    string Currency,
    decimal? InterestRate,
    DateTimeOffset? MaturityDate,
    decimal CurrentBalance,
    string? Notes) : ICommand<InvestmentAccountResponse>;
