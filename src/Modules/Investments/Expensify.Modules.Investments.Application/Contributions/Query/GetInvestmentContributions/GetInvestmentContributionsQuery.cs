using Expensify.Common.Application.Messaging;
using Expensify.Modules.Investments.Application.Abstractions;

namespace Expensify.Modules.Investments.Application.Contributions.Query.GetInvestmentContributions;

public sealed record GetInvestmentContributionsQuery(Guid UserId, Guid InvestmentId, int Page, int PageSize)
    : IQuery<InvestmentContributionsPageResponse>;
