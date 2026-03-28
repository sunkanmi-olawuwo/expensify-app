using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Expensify.Modules.Users.Infrastructure.Database.Migrations;

public partial class AddPreferenceCatalogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "currencies",
            schema: "users",
            columns: table => new
            {
                code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                minor_unit = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                is_default = table.Column<bool>(type: "boolean", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_currencies", x => x.code);
            });

        migrationBuilder.CreateTable(
            name: "timezones",
            schema: "users",
            columns: table => new
            {
                iana_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                is_default = table.Column<bool>(type: "boolean", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_timezones", x => x.iana_id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_currencies_is_default",
            schema: "users",
            table: "currencies",
            column: "is_default",
            unique: true,
            filter: "\"is_default\" AND \"is_active\"");

        migrationBuilder.CreateIndex(
            name: "ix_timezones_is_default",
            schema: "users",
            table: "timezones",
            column: "is_default",
            unique: true,
            filter: "\"is_default\" AND \"is_active\"");

        migrationBuilder.Sql(
            """
            INSERT INTO users.currencies (code, name, symbol, minor_unit, is_active, is_default, sort_order, created_at_utc)
            VALUES
                ('GBP', 'British Pound', '£', 2, TRUE, TRUE, 0, NOW()),
                ('USD', 'US Dollar', '$', 2, TRUE, FALSE, 1, NOW()),
                ('EUR', 'Euro', '€', 2, TRUE, FALSE, 2, NOW())
            ON CONFLICT (code) DO NOTHING;
            """);

        migrationBuilder.Sql(
            """
            INSERT INTO users.timezones (iana_id, display_name, is_active, is_default, sort_order, created_at_utc)
            VALUES
                ('UTC', 'UTC', TRUE, TRUE, 0, NOW()),
                ('Europe/London', 'Europe/London', TRUE, FALSE, 1, NOW()),
                ('Europe/Berlin', 'Europe/Berlin', TRUE, FALSE, 2, NOW()),
                ('America/New_York', 'America/New_York', TRUE, FALSE, 3, NOW())
            ON CONFLICT (iana_id) DO NOTHING;
            """);

        migrationBuilder.Sql(
            """
            INSERT INTO users.currencies (code, name, symbol, minor_unit, is_active, is_default, sort_order, created_at_utc)
            SELECT DISTINCT u.currency, u.currency, u.currency, 2, FALSE, FALSE, 1000, NOW()
            FROM users.users u
            WHERE NOT EXISTS (
                SELECT 1
                FROM users.currencies c
                WHERE c.code = u.currency
            );
            """);

        migrationBuilder.Sql(
            """
            INSERT INTO users.timezones (iana_id, display_name, is_active, is_default, sort_order, created_at_utc)
            SELECT DISTINCT u.timezone, u.timezone, FALSE, FALSE, 1000, NOW()
            FROM users.users u
            WHERE NOT EXISTS (
                SELECT 1
                FROM users.timezones t
                WHERE t.iana_id = u.timezone
            );
            """);

        migrationBuilder.CreateIndex(
            name: "ix_users_currency",
            schema: "users",
            table: "users",
            column: "currency");

        migrationBuilder.CreateIndex(
            name: "ix_users_timezone",
            schema: "users",
            table: "users",
            column: "timezone");

        migrationBuilder.AddForeignKey(
            name: "fk_users_currencies_currency",
            schema: "users",
            table: "users",
            column: "currency",
            principalSchema: "users",
            principalTable: "currencies",
            principalColumn: "code",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_users_timezones_timezone",
            schema: "users",
            table: "users",
            column: "timezone",
            principalSchema: "users",
            principalTable: "timezones",
            principalColumn: "iana_id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_users_currencies_currency",
            schema: "users",
            table: "users");

        migrationBuilder.DropForeignKey(
            name: "fk_users_timezones_timezone",
            schema: "users",
            table: "users");

        migrationBuilder.DropIndex(
            name: "ix_users_currency",
            schema: "users",
            table: "users");

        migrationBuilder.DropIndex(
            name: "ix_users_timezone",
            schema: "users",
            table: "users");

        migrationBuilder.DropTable(
            name: "currencies",
            schema: "users");

        migrationBuilder.DropTable(
            name: "timezones",
            schema: "users");
    }
}
