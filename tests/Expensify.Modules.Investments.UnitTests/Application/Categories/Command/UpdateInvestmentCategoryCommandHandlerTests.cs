using NSubstitute;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Application.Categories.Command.UpdateInvestmentCategory;
using Expensify.Modules.Investments.Domain.Categories;

namespace Expensify.Modules.Investments.UnitTests.Application.Categories.Command;

[TestFixture]
internal sealed class UpdateInvestmentCategoryCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenCategoryExists_ShouldToggleActiveFlag()
    {
        IInvestmentCategoryRepository categoryRepository = Substitute.For<IInvestmentCategoryRepository>();
        IInvestmentsUnitOfWork unitOfWork = Substitute.For<IInvestmentsUnitOfWork>();
        UpdateInvestmentCategoryCommandHandler sut = new(categoryRepository, unitOfWork);

        InvestmentCategory category = CreateCategory(Guid.NewGuid(), "ISA", InvestmentCategorySlugs.Isa, true);
        categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        Result<InvestmentCategoryResponse> result = await sut.Handle(
            new UpdateInvestmentCategoryCommand(category.Id, false),
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.IsActive, Is.False);
        }

        categoryRepository.Received(1).Update(category);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static InvestmentCategory CreateCategory(Guid id, string name, string slug, bool isActive)
    {
        return InvestmentCategory.Create(id, name, slug, isActive);
    }
}
