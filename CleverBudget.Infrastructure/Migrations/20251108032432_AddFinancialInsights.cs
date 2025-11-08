using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleverBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialInsights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialInsights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Recommendation = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: false),
                    ImpactAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BenchmarkAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    IncludeIncomeInsights = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludeExpenseInsights = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataPointsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialInsights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialInsights_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInsights_UserId_Category_Severity",
                table: "FinancialInsights",
                columns: new[] { "UserId", "Category", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInsights_UserId_GeneratedAt",
                table: "FinancialInsights",
                columns: new[] { "UserId", "GeneratedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialInsights");
        }
    }
}
