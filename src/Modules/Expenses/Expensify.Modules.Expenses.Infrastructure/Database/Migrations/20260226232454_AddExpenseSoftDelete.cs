using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Expensify.Modules.Expenses.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddExpenseSoftDelete : Migration
{
    private static readonly string[] DeletedAtColumns = ["user_id", "deleted_at_utc"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<global::System.DateTime>(
            name: "deleted_at_utc",
            schema: "expenses",
            table: "expenses",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_expenses_user_id_deleted_at_utc",
            schema: "expenses",
            table: "expenses",
            columns: DeletedAtColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_expenses_user_id_deleted_at_utc",
            schema: "expenses",
            table: "expenses");

        migrationBuilder.DropColumn(
            name: "deleted_at_utc",
            schema: "expenses",
            table: "expenses");
    }
}