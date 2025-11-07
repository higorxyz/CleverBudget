-- Script para corrigir a tabela Goals no PostgreSQL
-- Execute este script diretamente no console do Railway Postgres

DO $$ 
BEGIN 
    -- Add CategoryId
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Goals' AND column_name = 'CategoryId'
    ) THEN
        ALTER TABLE "Goals" ADD COLUMN "CategoryId" integer NOT NULL DEFAULT 0;
        ALTER TABLE "Goals" ADD CONSTRAINT "FK_Goals_Categories_CategoryId" 
        FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE RESTRICT;
        CREATE INDEX "IX_Goals_CategoryId" ON "Goals" ("CategoryId");
        RAISE NOTICE 'Coluna CategoryId adicionada';
    ELSE
        RAISE NOTICE 'Coluna CategoryId já existe';
    END IF;
    
    -- Add Month
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Goals' AND column_name = 'Month'
    ) THEN
        ALTER TABLE "Goals" ADD COLUMN "Month" integer NOT NULL DEFAULT 1;
        RAISE NOTICE 'Coluna Month adicionada';
    ELSE
        RAISE NOTICE 'Coluna Month já existe';
    END IF;
    
    -- Add Year
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Goals' AND column_name = 'Year'
    ) THEN
        ALTER TABLE "Goals" ADD COLUMN "Year" integer NOT NULL DEFAULT 2025;
        RAISE NOTICE 'Coluna Year adicionada';
    ELSE
        RAISE NOTICE 'Coluna Year já existe';
    END IF;
    
    -- Add CreatedAt
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Goals' AND column_name = 'CreatedAt'
    ) THEN
        ALTER TABLE "Goals" ADD COLUMN "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW();
        RAISE NOTICE 'Coluna CreatedAt adicionada';
    ELSE
        RAISE NOTICE 'Coluna CreatedAt já existe';
    END IF;
END $$;

-- Verificar as colunas da tabela Goals
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'Goals'
ORDER BY ordinal_position;
