using Npgsql;

Console.WriteLine("🔧 CORRIGINDO tabela Budgets no Railway PostgreSQL...");

var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("❌ DATABASE_URL não encontrada!");
    Console.WriteLine("Cole a connection string do Railway (formato: postgresql://...):");
    connectionString = Console.ReadLine();
}

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("❌ Connection string vazia!");
    return;
}

// Converter postgresql:// para formato Npgsql
if (connectionString.StartsWith("postgresql://"))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}

Console.WriteLine($"🔍 Conectando ao Railway PostgreSQL...");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    Console.WriteLine("✅ Conectado com sucesso!");

    // Verificar se a tabela Budgets existe
    var checkTableQuery = @"
        SELECT EXISTS (
            SELECT FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_name = 'Budgets'
        );
    ";

    using var checkCmd = new NpgsqlCommand(checkTableQuery, connection);
    var tableExists = (bool)(await checkCmd.ExecuteScalarAsync())!;

    if (tableExists)
    {
        Console.WriteLine("⚠️ Tabela Budgets existe com estrutura INCORRETA!");
        Console.WriteLine("🗑️ Deletando tabela Budgets...");

        var dropTableQuery = @"DROP TABLE IF EXISTS ""Budgets"" CASCADE;";
        using var dropCmd = new NpgsqlCommand(dropTableQuery, connection);
        await dropCmd.ExecuteNonQueryAsync();

        Console.WriteLine("✅ Tabela antiga deletada!");
    }

    // Remover migration antiga do histórico
    Console.WriteLine("🗑️ Limpando histórico de migrations...");
    var deleteMigrationQuery = @"
        DELETE FROM ""__EFMigrationsHistory"" 
        WHERE ""MigrationId"" = '20251102024437_AddBudgets';
    ";
    using var deleteMigrationCmd = new NpgsqlCommand(deleteMigrationQuery, connection);
    await deleteMigrationCmd.ExecuteNonQueryAsync();

    Console.WriteLine("📋 Criando tabela Budgets com tipos CORRETOS...");

    var createTableQuery = @"
        CREATE TABLE ""Budgets"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""UserId"" TEXT NOT NULL,
            ""CategoryId"" INTEGER NOT NULL,
            ""Amount"" NUMERIC(18,2) NOT NULL,
            ""Month"" INTEGER NOT NULL,
            ""Year"" INTEGER NOT NULL,
            ""AlertAt50Percent"" BOOLEAN NOT NULL DEFAULT TRUE,
            ""AlertAt80Percent"" BOOLEAN NOT NULL DEFAULT TRUE,
            ""AlertAt100Percent"" BOOLEAN NOT NULL DEFAULT TRUE,
            ""Alert50Sent"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""Alert80Sent"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""Alert100Sent"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT ""FK_Budgets_AspNetUsers_UserId"" FOREIGN KEY (""UserId"") 
                REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE,
            CONSTRAINT ""FK_Budgets_Categories_CategoryId"" FOREIGN KEY (""CategoryId"") 
                REFERENCES ""Categories""(""Id"") ON DELETE RESTRICT
        );

        CREATE INDEX ""IX_Budgets_CategoryId"" ON ""Budgets"" (""CategoryId"");
        CREATE UNIQUE INDEX ""IX_Budgets_UserId_CategoryId_Month_Year"" 
            ON ""Budgets"" (""UserId"", ""CategoryId"", ""Month"", ""Year"");
    ";

    using var createCmd = new NpgsqlCommand(createTableQuery, connection);
    await createCmd.ExecuteNonQueryAsync();

    Console.WriteLine("✅ Tabela Budgets criada com BOOLEAN!");
    Console.WriteLine("✅ Índices criados!");
    Console.WriteLine("✅ Foreign keys configuradas!");

    // Adicionar entrada na tabela de migrations
    var insertMigrationQuery = @"
        INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
        VALUES ('20251102024437_AddBudgets', '9.0.10');
    ";

    using var migrationCmd = new NpgsqlCommand(insertMigrationQuery, connection);
    await migrationCmd.ExecuteNonQueryAsync();

    Console.WriteLine("✅ Migration registrada no histórico!");
    Console.WriteLine();
    Console.WriteLine("🎉 Tabela Budgets CORRIGIDA no Railway!");
    Console.WriteLine();
    Console.WriteLine("⚠️ IMPORTANTE: Faça um novo deploy no Railway para aplicar as mudanças!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Erro: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
}
