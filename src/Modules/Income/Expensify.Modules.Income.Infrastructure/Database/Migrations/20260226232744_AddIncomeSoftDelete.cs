using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Expensify.Modules.Income.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddIncomeSoftDelete : Migration
{
    private static readonly string[] DeletedAtColumns = ["user_id", "deleted_at_utc"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<global::System.DateTime>(
            name: "deleted_at_utc",
            schema: "income",
            table: "incomes",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_incomes_user_id_deleted_at_utc",
            schema: "income",
            table: "incomes",
            columns: DeletedAtColumns);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_incomes_user_id_deleted_at_utc",
            schema: "income",
            table: "incomes");

        migrationBuilder.DropColumn(
            name: "deleted_at_utc",
            schema: "income",
            table: "incomes");
    }
}