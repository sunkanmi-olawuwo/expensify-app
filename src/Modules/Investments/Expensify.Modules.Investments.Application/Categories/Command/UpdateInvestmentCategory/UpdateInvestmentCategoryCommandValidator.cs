using FluentValidation;

namespace Expensify.Modules.Investments.Application.Categories.Command.UpdateInvestmentCategory;

internal sealed class UpdateInvestmentCategoryCommandValidator : AbstractValidator<UpdateInvestmentCategoryCommand>
{
    public UpdateInvestmentCategoryCommandValidator()
    {
        RuleFor(c => c.CategoryId).NotEmpty();
    }
}
