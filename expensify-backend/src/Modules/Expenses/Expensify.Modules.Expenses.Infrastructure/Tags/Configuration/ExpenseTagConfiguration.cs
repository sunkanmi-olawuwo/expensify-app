using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Expensify.Modules.Expenses.Domain.Tags;

namespace Expensify.Modules.Expenses.Infrastructure.Tags.Configuration;

internal sealed class ExpenseTagConfiguration : IEntityTypeConfiguration<ExpenseTag>
{
    public void Configure(EntityTypeBuilder<ExpenseTag> builder)
    {
        builder.ToTable("expense_tags");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasMaxLength(80)
            .IsRequired();

        builder.HasIndex(t => new { t.UserId, t.Name }).IsUnique();
    }
}
