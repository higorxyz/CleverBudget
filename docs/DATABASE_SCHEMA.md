# 💾 Schema do Banco de Dados - CleverBudget

## 📊 Diagrama ER (Entidade-Relacionamento)

```
┌─────────────────────┐
│       Users         │
│ (ASP.NET Identity)  │
├─────────────────────┤
│ Id (PK)             │
│ Email               │
│ PasswordHash        │
│ SecurityStamp       │
│ ...Identity fields  │
└──────────┬──────────┘
           │ 1
           │
           │ N
    ┌──────┴───────┬──────────────┬────────────────┐
    │              │              │                │
    │              │              │                │
┌───▼──────────┐ ┌─▼────────────┐ ┌▼─────────────┐ ┌▼──────────────────────┐
│ Transactions │ │ Categories   │ │ Goals        │ │ RecurringTransactions │
├──────────────┤ ├──────────────┤ ├──────────────┤ ├───────────────────────┤
│ Id (PK)      │ │ Id (PK)      │ │ Id (PK)      │ │ Id (PK)               │
│ UserId (FK)  │ │ UserId (FK)  │ │ UserId (FK)  │ │ UserId (FK)           │
│ CategoryId * │ │ Name         │ │ Name         │ │ CategoryId (FK)       │
│ Amount       │ │ Type         │ │ TargetAmount │ │ Amount                │
│ Description  │ │ CreatedAt    │ │ CurrentAmount│ │ Description           │
│ Date         │ └──────────────┘ │ Deadline     │ │ StartDate             │
│ Type         │                  │ CreatedAt    │ │ Frequency             │
│ ImageUrl     │                  └──────────────┘ │ IsActive              │
│ CreatedAt    │                                   │ NextOccurrence        │
│ UpdatedAt    │                                   │ CreatedAt             │
└──────────────┘                                   └───────────────────────┘
    │ N                                                 │ N
    │                                                   │
    │ 1                                                 │ 1
    ▼                                                   ▼
┌──────────────┐                                  ┌──────────────┐
│ Categories   │                                  │ Categories   │
│ (opcional)   │                                  │ (opcional)   │
└──────────────┘                                  └──────────────┘
```

## 📋 Tabelas e Campos

### 1. **AspNetUsers** (Identity)

Gerenciada pelo ASP.NET Core Identity.

| Campo | Tipo | Descrição | Constraints |
|-------|------|-----------|-------------|
| **Id** | string (GUID) | Identificador único | PK, NOT NULL |
| Email | string(256) | E-mail do usuário | UNIQUE, NOT NULL |
| NormalizedEmail | string(256) | E-mail normalizado | INDEXED |
| PasswordHash | string | Hash da senha | NOT NULL |
| SecurityStamp | string | Token de segurança | NOT NULL |
| PhoneNumber | string | Telefone | NULLABLE |
| TwoFactorEnabled | bool | 2FA habilitado | DEFAULT false |
| LockoutEnd | DateTimeOffset? | Fim do bloqueio | NULLABLE |
| AccessFailedCount | int | Tentativas falhas | DEFAULT 0 |
| ... | ... | Outros campos Identity | ... |

**Índices:**
- `IX_AspNetUsers_NormalizedEmail` (UNIQUE)
- `IX_AspNetUsers_NormalizedUserName` (UNIQUE)

---

### 2. **Categories**

Categorias de transações (ex: "Alimentação", "Transporte", "Salário").

| Campo | Tipo | Descrição | Constraints |
|-------|------|-----------|-------------|
| **Id** | int | Identificador único | PK, IDENTITY |
| **UserId** | string (GUID) | ID do usuário | FK → AspNetUsers.Id, NOT NULL |
| Name | string(100) | Nome da categoria | NOT NULL |
| Type | int (enum) | Tipo: 0=Expense, 1=Income | NOT NULL |
| CreatedAt | DateTime | Data de criação | NOT NULL, DEFAULT GETUTCDATE() |

**Constraints:**
- `FK_Categories_AspNetUsers_UserId`
- Unique: (UserId, Name, Type) - Mesma categoria por tipo

**Índices:**
- `IX_Categories_UserId`

**Enum TransactionType:**
```csharp
public enum TransactionType
{
    Expense = 0,  // Despesa
    Income = 1    // Receita
}
```

---

### 3. **Transactions**

Transações financeiras (receitas e despesas).

| Campo | Tipo | Descrição | Constraints |
|-------|------|-----------|-------------|
| **Id** | int | Identificador único | PK, IDENTITY |
| **UserId** | string (GUID) | ID do usuário | FK → AspNetUsers.Id, NOT NULL |
| CategoryId | int? | ID da categoria | FK → Categories.Id, NULLABLE |
| Amount | decimal(18,2) | Valor da transação | NOT NULL, CHECK > 0 |
| Description | string(500) | Descrição | NOT NULL |
| Date | DateTime | Data da transação | NOT NULL |
| Type | int (enum) | Tipo: 0=Expense, 1=Income | NOT NULL |
| ImageUrl | string? | URL da imagem (receipt) | NULLABLE |
| CreatedAt | DateTime | Data de criação | NOT NULL, DEFAULT GETUTCDATE() |
| UpdatedAt | DateTime? | Data de atualização | NULLABLE |

**Constraints:**
- `FK_Transactions_AspNetUsers_UserId` (CASCADE DELETE)
- `FK_Transactions_Categories_CategoryId` (SET NULL on delete)
- `CK_Transactions_Amount_Positive` (Amount > 0)

**Índices:**
- `IX_Transactions_UserId`
- `IX_Transactions_CategoryId`
- `IX_Transactions_Date` (para queries por período)
- `IX_Transactions_Type`

**Queries Comuns:**
```sql
-- Transações por período
SELECT * FROM Transactions 
WHERE UserId = @userId 
  AND Date BETWEEN @startDate AND @endDate
ORDER BY Date DESC;

-- Gastos por categoria no mês
SELECT c.Name, SUM(t.Amount) as Total
FROM Transactions t
JOIN Categories c ON t.CategoryId = c.Id
WHERE t.UserId = @userId 
  AND t.Type = 0  -- Expense
  AND MONTH(t.Date) = @month
GROUP BY c.Name;
```

---

### 4. **Goals**

Metas financeiras do usuário.

| Campo | Tipo | Descrição | Constraints |
|-------|------|-----------|-------------|
| **Id** | int | Identificador único | PK, IDENTITY |
| **UserId** | string (GUID) | ID do usuário | FK → AspNetUsers.Id, NOT NULL |
| Name | string(200) | Nome da meta | NOT NULL |
| TargetAmount | decimal(18,2) | Valor alvo | NOT NULL, CHECK > 0 |
| CurrentAmount | decimal(18,2) | Valor atual | NOT NULL, DEFAULT 0, CHECK >= 0 |
| Deadline | DateTime? | Data limite | NULLABLE |
| CreatedAt | DateTime | Data de criação | NOT NULL, DEFAULT GETUTCDATE() |

**Constraints:**
- `FK_Goals_AspNetUsers_UserId` (CASCADE DELETE)
- `CK_Goals_TargetAmount_Positive` (TargetAmount > 0)
- `CK_Goals_CurrentAmount_NonNegative` (CurrentAmount >= 0)

**Índices:**
- `IX_Goals_UserId`

**Lógica de Negócio:**
- `ProgressPercentage = (CurrentAmount / TargetAmount) * 100`
- `IsCompleted = CurrentAmount >= TargetAmount`

---

### 5. **RecurringTransactions**

Transações recorrentes (ex: salário mensal, aluguel).

| Campo | Tipo | Descrição | Constraints |
|-------|------|-----------|-------------|
| **Id** | int | Identificador único | PK, IDENTITY |
| **UserId** | string (GUID) | ID do usuário | FK → AspNetUsers.Id, NOT NULL |
| CategoryId | int? | ID da categoria | FK → Categories.Id, NULLABLE |
| Amount | decimal(18,2) | Valor da transação | NOT NULL, CHECK > 0 |
| Description | string(500) | Descrição | NOT NULL |
| StartDate | DateTime | Data de início | NOT NULL |
| Frequency | int (enum) | Frequência | NOT NULL |
| IsActive | bool | Ativa/Inativa | NOT NULL, DEFAULT true |
| NextOccurrence | DateTime | Próxima ocorrência | NOT NULL |
| CreatedAt | DateTime | Data de criação | NOT NULL, DEFAULT GETUTCDATE() |

**Constraints:**
- `FK_RecurringTransactions_AspNetUsers_UserId` (CASCADE DELETE)
- `FK_RecurringTransactions_Categories_CategoryId` (SET NULL)
- `CK_RecurringTransactions_Amount_Positive` (Amount > 0)

**Índices:**
- `IX_RecurringTransactions_UserId`
- `IX_RecurringTransactions_NextOccurrence`
- `IX_RecurringTransactions_IsActive`

**Enum RecurrenceFrequency:**
```csharp
public enum RecurrenceFrequency
{
    Daily = 0,    // Diário
    Weekly = 1,   // Semanal
    Monthly = 2,  // Mensal
    Yearly = 3    // Anual
}
```

**Cálculo de NextOccurrence:**
```csharp
switch (Frequency)
{
    case Daily: NextOccurrence = CurrentDate.AddDays(1); break;
    case Weekly: NextOccurrence = CurrentDate.AddDays(7); break;
    case Monthly: NextOccurrence = CurrentDate.AddMonths(1); break;
    case Yearly: NextOccurrence = CurrentDate.AddYears(1); break;
}
```

---

## 🔗 Relacionamentos

### User → Transactions (1:N)
- Um usuário tem muitas transações
- Deletar usuário deleta todas as transações (CASCADE)

### User → Categories (1:N)
- Um usuário tem muitas categorias
- Deletar usuário deleta todas as categorias (CASCADE)

### User → Goals (1:N)
- Um usuário tem muitas metas
- Deletar usuário deleta todas as metas (CASCADE)

### User → RecurringTransactions (1:N)
- Um usuário tem muitas transações recorrentes
- Deletar usuário deleta todas (CASCADE)

### Category → Transactions (1:N, opcional)
- Uma categoria pode ter muitas transações
- Deletar categoria mantém transações (SET NULL)

### Category → RecurringTransactions (1:N, opcional)
- Uma categoria pode ter muitas transações recorrentes
- Deletar categoria mantém recorrentes (SET NULL)

---

## 🛠️ Migrações

### Criar Nova Migração

```bash
dotnet ef migrations add NomeDaMigracao --project CleverBudget.Infrastructure --startup-project CleverBudget.Api
```

### Aplicar Migrações

```bash
dotnet ef database update --project CleverBudget.Infrastructure --startup-project CleverBudget.Api
```

### Reverter Migração

```bash
dotnet ef database update NomeMigracaoAnterior --project CleverBudget.Infrastructure --startup-project CleverBudget.Api
```

### Remover Última Migração (não aplicada)

```bash
dotnet ef migrations remove --project CleverBudget.Infrastructure --startup-project CleverBudget.Api
```

### Gerar Script SQL

```bash
dotnet ef migrations script --project CleverBudget.Infrastructure --startup-project CleverBudget.Api -o migration.sql
```

---

## 📈 Performance e Otimização

### Índices Recomendados

```sql
-- Já implementados
CREATE INDEX IX_Transactions_UserId ON Transactions(UserId);
CREATE INDEX IX_Transactions_Date ON Transactions(Date);
CREATE INDEX IX_Transactions_CategoryId ON Transactions(CategoryId);

-- Compostos (considerar se necessário)
CREATE INDEX IX_Transactions_UserId_Date ON Transactions(UserId, Date DESC);
CREATE INDEX IX_Transactions_UserId_Type ON Transactions(UserId, Type);
```

### Queries Otimizadas

**EF Core - Include para evitar N+1:**
```csharp
var transactions = await _context.Transactions
    .Include(t => t.Category)  // Carrega categoria junto
    .Where(t => t.UserId == userId)
    .OrderByDescending(t => t.Date)
    .ToListAsync();
```

**EF Core - Paginação:**
```csharp
var pagedTransactions = await _context.Transactions
    .Where(t => t.UserId == userId)
    .OrderByDescending(t => t.Date)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

**EF Core - AsNoTracking para read-only:**
```csharp
var report = await _context.Transactions
    .AsNoTracking()  // Mais rápido para leitura
    .Where(t => t.UserId == userId)
    .GroupBy(t => t.Category.Name)
    .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
    .ToListAsync();
```

---

## 🔐 Segurança de Dados

### Row-Level Security

Todas as queries filtram por `UserId` para garantir que um usuário só acesse seus próprios dados:

```csharp
// ❌ ERRADO - Expõe dados de todos os usuários
var transaction = await _context.Transactions.FindAsync(id);

// ✅ CORRETO - Filtra por UserId
var transaction = await _context.Transactions
    .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
```

### Proteção de Dados Sensíveis

- **Senhas:** Hasheadas com PBKDF2 (via Identity)
- **Tokens JWT:** Assinados com HMAC-SHA256
- **Connection String:** Nunca exposta em logs
- **Imagens:** Armazenadas externamente (Cloudinary)

---

## 📊 Estatísticas e Dados de Exemplo

### Estimativa de Tamanho

| Tabela | Registros/usuário | Tamanho médio/registro | Tamanho anual |
|--------|-------------------|------------------------|---------------|
| Transactions | ~500/ano | ~200 bytes | ~100 KB |
| Categories | ~20 | ~100 bytes | ~2 KB |
| Goals | ~5 | ~150 bytes | ~750 bytes |
| RecurringTransactions | ~10 | ~180 bytes | ~1.8 KB |

**Total por usuário/ano:** ~105 KB

---

## 📚 Documentos Relacionados

- [Migrações](./MIGRATIONS.md) - Guia completo de migrações
- [Arquitetura](./ARCHITECTURE.md) - Estrutura do projeto
- [Endpoints](./ENDPOINTS.md) - APIs que usam estas tabelas
