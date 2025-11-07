using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleverBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixMissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add missing CategoryId column to Goals table if it doesn't exist
            migrationBuilder.Sql(
                @"DO $$ 
                BEGIN 
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Goals' AND column_name = 'CategoryId'
                    ) THEN
                        ALTER TABLE ""Goals"" ADD COLUMN ""CategoryId"" integer NOT NULL DEFAULT 0;
                        
                        -- Add foreign key constraint
                        ALTER TABLE ""Goals"" ADD CONSTRAINT ""FK_Goals_Categories_CategoryId"" 
                        FOREIGN KEY (""CategoryId"") REFERENCES ""Categories"" (""Id"") ON DELETE RESTRICT;
                        
                        -- Add index
                        CREATE INDEX ""IX_Goals_CategoryId"" ON ""Goals"" (""CategoryId"");
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
                        WHERE table_name = 'Goals' AND column_name = 'CategoryId'
                    ) THEN
                        ALTER TABLE ""Goals"" DROP CONSTRAINT IF EXISTS ""FK_Goals_Categories_CategoryId"";
                        DROP INDEX IF EXISTS ""IX_Goals_CategoryId"";
                        ALTER TABLE ""Goals"" DROP COLUMN ""CategoryId"";
                    END IF;
                END $$;",
                suppressTransaction: false);
        }
    }
}
