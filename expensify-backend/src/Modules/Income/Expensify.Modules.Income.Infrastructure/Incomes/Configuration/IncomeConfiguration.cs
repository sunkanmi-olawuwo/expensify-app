using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Expensify.Modules.Income.Domain.Incomes;
using IncomeEntity = Expensify.Modules.Income.Domain.Incomes.Income;

namespace Expensify.Modules.Income.Infrastructure.Incomes.Configuration;

internal sealed class IncomeConfiguration : IEntityTypeConfiguration<IncomeEntity>
{
    public void Configure(EntityTypeBuilder<IncomeEntity> builder)
    {
        builder.ToTable("incomes");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(i => i.Source)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(i => i.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(i => i.Note)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasIndex(i => new { i.UserId, i.IncomeDate });
        builder.HasIndex(i => new { i.UserId, i.Type, i.IncomeDate });
        builder.HasIndex(i => new { i.UserId, i.Source });
    }
}
