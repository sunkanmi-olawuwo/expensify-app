using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Expensify.Modules.Expenses.Domain.Categories;

namespace Expensify.Modules.Expenses.Infrastructure.Categories.Configuration;

internal sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("expense_categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(80)
            .IsRequired();

        builder.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
    }
}
