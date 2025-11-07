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
            // Add all missing columns to Goals table if they don't exist
            migrationBuilder.Sql(
                @"DO $$ 
                BEGIN 
                    -- Add CategoryId
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Goals' AND column_name = 'CategoryId'
                    ) THEN
                        ALTER TABLE ""Goals"" ADD COLUMN ""CategoryId"" integer NOT NULL DEFAULT 0;
                        ALTER TABLE ""Goals"" ADD CONSTRAINT ""FK_Goals_Categories_CategoryId"" 
                        FOREIGN KEY (""CategoryId"") REFERENCES ""Categories"" (""Id"") ON DELETE RESTRICT;
                        CREATE INDEX ""IX_Goals_CategoryId"" ON ""Goals"" (""CategoryId"");
                    END IF;
                    
                    -- Add Month
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Goals' AND column_name = 'Month'
                    ) THEN
                        ALTER TABLE ""Goals"" ADD COLUMN ""Month"" integer NOT NULL DEFAULT 1;
                    END IF;
                    
                    -- Add Year
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Goals' AND column_name = 'Year'
                    ) THEN
                        ALTER TABLE ""Goals"" ADD COLUMN ""Year"" integer NOT NULL DEFAULT 2025;
                    END IF;
                    
                    -- Add CreatedAt
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Goals' AND column_name = 'CreatedAt'
                    ) THEN
                        ALTER TABLE ""Goals"" ADD COLUMN ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW();
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
                    
                    ALTER TABLE ""Goals"" DROP COLUMN IF EXISTS ""Month"";
                    ALTER TABLE ""Goals"" DROP COLUMN IF EXISTS ""Year"";
                    ALTER TABLE ""Goals"" DROP COLUMN IF EXISTS ""CreatedAt"";
                END $$;",
                suppressTransaction: false);
        }
    }
}
