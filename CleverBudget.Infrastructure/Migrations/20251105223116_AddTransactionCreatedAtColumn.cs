using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleverBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionCreatedAtColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider.Equals("Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
            {
                migrationBuilder.Sql(
                    "ALTER TABLE \"Transactions\" ADD COLUMN IF NOT EXISTS \"CreatedAt\" timestamp with time zone DEFAULT NOW();");

                migrationBuilder.Sql(
                    "UPDATE \"Transactions\" SET \"CreatedAt\" = COALESCE(\"CreatedAt\", \"Date\");");

                migrationBuilder.Sql(
                    "ALTER TABLE \"Transactions\" ALTER COLUMN \"CreatedAt\" SET NOT NULL;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider.Equals("Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
            {
                migrationBuilder.Sql(
                    "ALTER TABLE \"Transactions\" DROP COLUMN IF EXISTS \"CreatedAt\";");
            }
        }
    }
}
