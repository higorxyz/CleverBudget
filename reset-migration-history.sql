-- Remove a migration FixMissingColumns do histórico para forçar re-execução
DELETE FROM "__EFMigrationsHistory" 
WHERE "MigrationId" = '20251107000213_FixMissingColumns';

-- Verificar migrations registradas
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
