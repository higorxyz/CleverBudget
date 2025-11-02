-- 🔧 Script para corrigir tipos booleanos no PostgreSQL (Railway)
-- Execute este script manualmente no banco PostgreSQL do Railway
-- ANTES de fazer o próximo deploy

-- Converter IsActive de INTEGER para BOOLEAN
ALTER TABLE "RecurringTransactions" 
ALTER COLUMN "IsActive" TYPE boolean USING "IsActive"::int::boolean;

-- Converter IsDefault de INTEGER para BOOLEAN  
ALTER TABLE "Categories" 
ALTER COLUMN "IsDefault" TYPE boolean USING "IsDefault"::int::boolean;

-- Verificar se funcionou
SELECT 
    table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE table_name IN ('RecurringTransactions', 'Categories')
AND column_name IN ('IsActive', 'IsDefault')
ORDER BY table_name, column_name;
