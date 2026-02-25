using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Expensify.Modules.Income.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Initial_Income : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "income");

        migrationBuilder.CreateTable(
            name: "inbox_message_consumers",
            schema: "income",
            columns: table => new
            {
                inbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_inbox_message_consumers", x => new { x.inbox_message_id, x.name });
            });

        migrationBuilder.CreateTable(
            name: "inbox_messages",
            schema: "income",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "text", nullable: false),
                content = table.Column<string>(type: "jsonb", maxLength: 2000, nullable: false),
                occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                error = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_inbox_messages", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "incomes",
            schema: "income",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric", nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                income_date = table.Column<DateOnly>(type: "date", nullable: false),
                source = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_incomes", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "outbox_message_consumers",
            schema: "income",
            columns: table => new
            {
                outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_outbox_message_consumers", x => new { x.outbox_message_id, x.name });
            });

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            schema: "income",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "text", nullable: false),
                content = table.Column<string>(type: "jsonb", maxLength: 2000, nullable: false),
                occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                error = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_outbox_messages", x => x.id);
            });

#pragma warning disable CA1861
        migrationBuilder.CreateIndex(
            name: "ix_incomes_user_id_income_date",
            schema: "income",
            table: "incomes",
            columns: new[] { "user_id", "income_date" });

        migrationBuilder.CreateIndex(
            name: "ix_incomes_user_id_source",
            schema: "income",
            table: "incomes",
            columns: new[] { "user_id", "source" });

        migrationBuilder.CreateIndex(
            name: "ix_incomes_user_id_type_income_date",
            schema: "income",
            table: "incomes",
            columns: new[] { "user_id", "type", "income_date" });
#pragma warning restore CA1861
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "inbox_message_consumers",
            schema: "income");

        migrationBuilder.DropTable(
            name: "inbox_messages",
            schema: "income");

        migrationBuilder.DropTable(
            name: "incomes",
            schema: "income");

        migrationBuilder.DropTable(
            name: "outbox_message_consumers",
            schema: "income");

        migrationBuilder.DropTable(
            name: "outbox_messages",
            schema: "income");
    }
}
