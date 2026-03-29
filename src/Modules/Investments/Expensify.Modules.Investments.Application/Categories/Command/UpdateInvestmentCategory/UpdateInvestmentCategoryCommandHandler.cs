using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Domain.Categories;

namespace Expensify.Modules.Investments.Application.Categories.Command.UpdateInvestmentCategory;

internal sealed class UpdateInvestmentCategoryCommandHandler(
    IInvestmentCategoryRepository investmentCategoryRepository,
    IInvestmentsUnitOfWork unitOfWork)
    : ICommandHandler<UpdateInvestmentCategoryCommand, InvestmentCategoryResponse>
{
    public async Task<Result<InvestmentCategoryResponse>> Handle(UpdateInvestmentCategoryCommand request, CancellationToken cancellationToken)
    {
        InvestmentCategory? category = await investmentCategoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure<InvestmentCategoryResponse>(InvestmentCategoryErrors.NotFound(request.CategoryId));
        }

        category.SetActive(request.IsActive);
        investmentCategoryRepository.Update(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new InvestmentCategoryResponse(category.Id, category.Name, category.Slug, category.IsActive);
    }
}
