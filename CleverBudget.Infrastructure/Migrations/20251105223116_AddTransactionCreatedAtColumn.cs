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
            // PostgreSQL: Add column with conditional logic
            migrationBuilder.Sql(
                @"DO $$ 
                BEGIN 
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Transactions' AND column_name = 'CreatedAt'
                    ) THEN
                        ALTER TABLE ""Transactions"" ADD COLUMN ""CreatedAt"" timestamp with time zone DEFAULT NOW() NOT NULL;
                        UPDATE ""Transactions"" SET ""CreatedAt"" = ""Date"" WHERE ""CreatedAt"" IS NULL;
                    END IF;
                END $$;",
                suppressTransaction: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DO $$ 
                BEGIN 
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Transactions' AND column_name = 'CreatedAt'
                    ) THEN
                        ALTER TABLE ""Transactions"" DROP COLUMN ""CreatedAt"";
                    END IF;
                END $$;",
                suppressTransaction: false);
        }
    }
}
