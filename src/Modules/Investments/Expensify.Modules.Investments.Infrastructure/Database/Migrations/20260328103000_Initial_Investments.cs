using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Expensify.Modules.Investments.Infrastructure.Database.Migrations;

[DbContext(typeof(InvestmentsDbContext))]
[Migration("20260328103000_InitialInvestments")]
public partial class InitialInvestments : Migration
{
    private static readonly string[] AccountUserCategoryColumns = ["user_id", "category_id"];
    private static readonly string[] AccountUserDeletedColumns = ["user_id", "deleted_at_utc"];
    private static readonly string[] ContributionInvestmentDateColumns = ["investment_id", "date"];
    private static readonly string[] ContributionInvestmentDeletedColumns = ["investment_id", "deleted_at_utc"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "investments");

        migrationBuilder.CreateTable(
            name: "inbox_message_consumers",
            schema: "investments",
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
            schema: "investments",
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
            name: "investment_categories",
            schema: "investments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_investment_categories", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "outbox_message_consumers",
            schema: "investments",
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
            schema: "investments",
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

        migrationBuilder.CreateTable(
            name: "investment_accounts",
            schema: "investments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                provider = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                category_id = table.Column<Guid>(type: "uuid", nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                interest_rate = table.Column<decimal>(type: "numeric(8,4)", nullable: true),
                maturity_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                current_balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_investment_accounts", x => x.id);
                table.ForeignKey(
                    name: "fk_investment_accounts_investment_categories_category_id",
                    column: x => x.category_id,
                    principalSchema: "investments",
                    principalTable: "investment_categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "investment_contributions",
            schema: "investments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                investment_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_investment_contributions", x => x.id);
                table.ForeignKey(
                    name: "fk_investment_contributions_investment_accounts_investment_id",
                    column: x => x.investment_id,
                    principalSchema: "investments",
                    principalTable: "investment_accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_investment_accounts_category_id",
            schema: "investments",
            table: "investment_accounts",
            column: "category_id");

#pragma warning disable CA1861
        migrationBuilder.CreateIndex(
            name: "ix_investment_accounts_user_id_category_id",
            schema: "investments",
            table: "investment_accounts",
            columns: AccountUserCategoryColumns);

        migrationBuilder.CreateIndex(
            name: "ix_investment_accounts_user_id_deleted_at_utc",
            schema: "investments",
            table: "investment_accounts",
            columns: AccountUserDeletedColumns);
#pragma warning restore CA1861

        migrationBuilder.CreateIndex(
            name: "ix_investment_categories_slug",
            schema: "investments",
            table: "investment_categories",
            column: "slug",
            unique: true);

#pragma warning disable CA1861
        migrationBuilder.CreateIndex(
            name: "ix_investment_contributions_investment_id_date",
            schema: "investments",
            table: "investment_contributions",
            columns: ContributionInvestmentDateColumns);

        migrationBuilder.CreateIndex(
            name: "ix_investment_contributions_investment_id_deleted_at_utc",
            schema: "investments",
            table: "investment_contributions",
            columns: ContributionInvestmentDeletedColumns);
#pragma warning restore CA1861

        migrationBuilder.Sql(
            """
            INSERT INTO investments.investment_categories (id, name, slug, is_active, created_at_utc)
            VALUES
                ('11111111-1111-1111-1111-111111111111', 'ISA', 'isa', TRUE, NOW()),
                ('22222222-2222-2222-2222-222222222222', 'LISA', 'lisa', TRUE, NOW()),
                ('33333333-3333-3333-3333-333333333333', 'Mutual Fund', 'mutual-fund', TRUE, NOW()),
                ('44444444-4444-4444-4444-444444444444', 'Fixed Deposit', 'fixed-deposit', TRUE, NOW()),
                ('55555555-5555-5555-5555-555555555555', 'Other', 'other', TRUE, NOW())
            ON CONFLICT (slug) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "inbox_message_consumers",
            schema: "investments");

        migrationBuilder.DropTable(
            name: "inbox_messages",
            schema: "investments");

        migrationBuilder.DropTable(
            name: "investment_contributions",
            schema: "investments");

        migrationBuilder.DropTable(
            name: "outbox_message_consumers",
            schema: "investments");

        migrationBuilder.DropTable(
            name: "outbox_messages",
            schema: "investments");

        migrationBuilder.DropTable(
            name: "investment_accounts",
            schema: "investments");

        migrationBuilder.DropTable(
            name: "investment_categories",
            schema: "investments");
    }
}
