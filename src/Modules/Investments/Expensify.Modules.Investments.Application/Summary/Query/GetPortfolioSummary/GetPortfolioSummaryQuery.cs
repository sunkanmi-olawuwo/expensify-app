using Expensify.Common.Application.Messaging;
using Expensify.Modules.Investments.Application.Abstractions;

namespace Expensify.Modules.Investments.Application.Summary.Query.GetPortfolioSummary;

public sealed record GetPortfolioSummaryQuery(Guid UserId) : IQuery<PortfolioSummaryResponse>;
