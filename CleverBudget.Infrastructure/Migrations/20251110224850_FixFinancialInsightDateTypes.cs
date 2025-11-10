using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleverBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixFinancialInsightDateTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""
                    ALTER TABLE "FinancialInsights"
                    ALTER COLUMN "GeneratedAt" TYPE timestamptz USING "GeneratedAt"::timestamptz;

                    ALTER TABLE "FinancialInsights"
                    ALTER COLUMN "StartDate" TYPE timestamptz USING NULLIF("StartDate", '')::timestamptz;

                    ALTER TABLE "FinancialInsights"
                    ALTER COLUMN "EndDate" TYPE timestamptz USING NULLIF("EndDate", '')::timestamptz;
                """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""
                    ALTER TABLE "FinancialInsights"
                    ALTER COLUMN "GeneratedAt" TYPE text USING to_char("GeneratedAt", 'YYYY-MM-DD""T""HH24:MI:SS.MSZ');

                    ALTER TABLE "FinancialInsights"
                    ALTER COLUMN "StartDate" TYPE text USING CASE WHEN "StartDate" IS NULL THEN NULL ELSE to_char("StartDate", 'YYYY-MM-DD""T""HH24:MI:SS.MSZ') END;

                    ALTER TABLE "FinancialInsights"
                    ALTER COLUMN "EndDate" TYPE text USING CASE WHEN "EndDate" IS NULL THEN NULL ELSE to_char("EndDate", 'YYYY-MM-DD""T""HH24:MI:SS.MSZ') END;
                """);
            }
        }
    }
}
