using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Expensify.Modules.Expenses.Domain.Categories;
using Expensify.Modules.Expenses.Domain.Expenses;

namespace Expensify.Modules.Expenses.Infrastructure.Expenses.Configuration;

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(e => e.Merchant)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.Note)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(e => e.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.DeletedAtUtc);

        builder.HasIndex(e => new { e.UserId, e.ExpenseDate });
        builder.HasIndex(e => new { e.UserId, e.CategoryId, e.ExpenseDate });
        builder.HasIndex(e => new { e.UserId, e.Merchant });
        builder.HasIndex(e => new { e.UserId, e.DeletedAtUtc });

        builder.HasOne<ExpenseCategory>()
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Tags)
            .WithMany()
            .UsingEntity("expense_expense_tags");
    }
}
