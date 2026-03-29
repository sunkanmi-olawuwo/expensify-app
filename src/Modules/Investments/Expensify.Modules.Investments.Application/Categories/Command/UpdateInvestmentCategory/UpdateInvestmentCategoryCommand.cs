using Expensify.Common.Application.Messaging;
using Expensify.Modules.Investments.Application.Abstractions;

namespace Expensify.Modules.Investments.Application.Categories.Command.UpdateInvestmentCategory;

public sealed record UpdateInvestmentCategoryCommand(Guid CategoryId, bool IsActive) : ICommand<InvestmentCategoryResponse>;
