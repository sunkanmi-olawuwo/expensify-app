using Expensify.Common.Application.Messaging;
using Expensify.Modules.Investments.Application.Abstractions;

namespace Expensify.Modules.Investments.Application.Categories.Query.GetInvestmentCategories;

public sealed record GetInvestmentCategoriesQuery(bool IncludeInactive) : IQuery<IReadOnlyCollection<InvestmentCategoryResponse>>;
