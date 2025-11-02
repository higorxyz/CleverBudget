using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleverBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixBooleanTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Converter colunas INTEGER para BOOLEAN apenas no PostgreSQL
            // SQLite continua usando INTEGER para booleanos (é o comportamento padrão)
            
            migrationBuilder.Sql(@"
                -- PostgreSQL: Converter INTEGER para BOOLEAN
                -- SQLite: Vai ignorar (não tem ALTER COLUMN TYPE)
                ALTER TABLE ""RecurringTransactions"" 
                ALTER COLUMN ""IsActive"" TYPE boolean USING ""IsActive""::int::boolean;

                ALTER TABLE ""Categories"" 
                ALTER COLUMN ""IsDefault"" TYPE boolean USING ""IsDefault""::int::boolean;
            ", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverter para INTEGER no PostgreSQL
            migrationBuilder.Sql(@"
                ALTER TABLE ""RecurringTransactions"" 
                ALTER COLUMN ""IsActive"" TYPE integer USING CASE WHEN ""IsActive"" THEN 1 ELSE 0 END;

                ALTER TABLE ""Categories"" 
                ALTER COLUMN ""IsDefault"" TYPE integer USING CASE WHEN ""IsDefault"" THEN 1 ELSE 0 END;
            ", suppressTransaction: true);
        }
    }
}
