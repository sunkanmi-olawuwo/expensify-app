using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Expensify.Modules.Expenses.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddExpenseCategoryForeignKey : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_expenses_category_id",
            schema: "expenses",
            table: "expenses",
            column: "category_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_expenses_category_id",
            schema: "expenses",
            table: "expenses");
    }
}
