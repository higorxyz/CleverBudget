using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleverBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "Categories",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "Essential");

            migrationBuilder.AddColumn<string>(
                name: "Segment",
                table: "Categories",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Categories",
                type: "text",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Segment",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Categories");
        }
    }
}
