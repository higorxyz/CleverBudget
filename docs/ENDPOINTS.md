# API Endpoints – CleverBudget

## Bases de URL
- Desenvolvimento HTTP: `http://localhost:5220/api`
- Desenvolvimento HTTPS: `https://localhost:7035/api`
- Produção (Railway): `https://cleverbudget-production.up.railway.app/api`

## Convenções
- Endpoints marcados com "Requer token" precisam do header `Authorization: Bearer <jwt>`.
- Respostas paginadas usam `PagedResult<T>` com campos `items`, `page`, `pageSize`, `totalCount`, `totalPages`, `hasPreviousPage`, `hasNextPage`.
- Datas aceitam `yyyy-MM-dd` ou ISO-8601.
- Enums relevantes:
  - `TransactionType`: `1=Income`, `2=Expense`
  - `RecurrenceFrequency`: `1=Daily`, `2=Weekly`, `3=Monthly`, `4=Yearly`

---

## Autenticação (`/api/auth`)

### POST `/register`
- Cria usuário com nome e sobrenome.
- Corpo:
  ```json
  {
    "firstName": "Ana",
    "lastName": "Lima",
    "email": "ana@example.com",
    "password": "SenhaForte123!",
    "confirmPassword": "SenhaForte123!"
  }
  ```
- 200 OK:
  ```json
  {
    "token": "<jwt>",
    "email": "ana@example.com",
    "firstName": "Ana",
    "lastName": "Lima",
    "expiresAt": "2025-11-02T18:45:27Z"
  }
  ```
- 400 Bad Request: `{"message": "Senha fraca.", "errorCode": "AUTH_WEAK_PASSWORD"}`

### POST `/login`
- Requer `email` e `password`.
- Retorna o mesmo `AuthResponseDto` acima.
- 401 Unauthorized em credencial inválida (`errorCode` típico: `AUTH_INVALID_CREDENTIALS`).

---

## Perfil (`/api/profile`)

### GET `/`
- Requer token.
- 200 OK (`UserProfileDto`):
  ```json
  {
    "id": "8c6c...",
    "firstName": "Ana",
    "lastName": "Lima",
    "email": "ana@example.com",
    "photoUrl": "https://res.cloudinary.com/...",
    "createdAt": "2024-05-10T14:12:00Z"
  }
  ```

### PUT `/`
- Atualiza `firstName` e `lastName`.
- 200 OK: `{"message":"Perfil atualizado com sucesso"}`.

### PUT `/password`
- Corpo: `currentPassword`, `newPassword`, `confirmPassword`.
- 200 OK em sucesso; 400 Bad Request retorna `message` e `errorCode` (por exemplo `AUTH_PASSWORD_MISMATCH`).

### PUT `/photo` (legado)
- Define uma URL já hospedada. Deve ser evitado; permanece por compatibilidade.

### POST `/photo`
- Recebe `multipart/form-data` com campo `file` (até 5 MB, JPG/PNG/WebP).
- Exige credenciais Cloudinary configuradas.
- 200 OK: `{"message": "Foto enviada e atualizada com sucesso", "photoUrl": "..."}`.

---

## Orçamentos (`/api/budgets`)

### GET `/`
- Query opcional: `year`, `month`.
- 200 OK: lista de `BudgetResponseDto` com campos como `amount`, `spent`, `remaining`, `percentageUsed`, status (`Normal`, `Alerta`, `Crítico`, `Excedido`).

### GET `/paged`
- Query extra: `page`, `pageSize` (máx 100), `sortBy`, `sortOrder`.
- Retorna `PagedResult<BudgetResponseDto>`.

### GET `/{id}` | GET `/category/{categoryId}/period?month=&year=` | GET `/current`
- Buscam orçamento específico, por categoria/período ou todos do mês atual.

### GET `/summary?month=&year=`
- 200 OK:
  ```json
  {
    "month": 11,
    "year": 2025,
    "totalBudget": 2500.00,
    "totalSpent": 1800.00,
    "remaining": 700.00,
    "percentageUsed": 72.0,
    "status": "Alerta"
  }
  ```

### POST `/`
- Corpo:
  ```json
  {
    "categoryId": 12,
    "amount": 800.00,
    "month": 11,
    "year": 2025,
    "alertAt50Percent": true,
    "alertAt80Percent": true,
    "alertAt100Percent": false
  }
  ```
- 201 Created com `BudgetResponseDto`.
- 400 Bad Request quando já existe orçamento para a categoria no período ou categoria inválida.

### PUT `/{id}`
- Campos aceitos: `amount`, `alertAt50Percent`, `alertAt80Percent`, `alertAt100Percent`.

### DELETE `/{id}`
- Remove orçamento; 404 se não existir.

---

## Categorias (`/api/categories`)

### GET `/`
- Query: `page`, `pageSize`, `sortBy` (name|createdAt|isDefault), `sortOrder`.
- Retorna `PagedResult<CategoryResponseDto>` com `isDefault`, `icon`, `color`.

### GET `/all`
- Lista completa (útil para combos).

### GET `/{id}` | POST `/` | PUT `/{id}` | DELETE `/{id}`
- Criação exige `name` e opcional `icon`, `color`.
- Atualização só funciona para categorias customizadas.
- DELETE falha com 400 quando categoria é padrão ou possui transações.

---

## Transações (`/api/transactions`)

### GET `/`
- Query: `page`, `pageSize`, `sortBy` (`date|amount|description|category`), `sortOrder`, `type`, `categoryId`, `startDate`, `endDate`.
- Retorna `PagedResult<TransactionResponseDto>`:
  ```json
  {
    "items": [
      {
        "id": 42,
        "amount": 120.00,
        "type": 2,
        "description": "Supermercado",
        "categoryId": 5,
        "categoryName": "Alimentação",
        "categoryIcon": "utensils",
        "categoryColor": "#FF6B6B",
        "date": "2025-11-01T12:30:00Z",
        "createdAt": "2025-11-01T12:31:12Z"
      }
    ],
    "page": 1,
    "pageSize": 10,
    "totalCount": 87,
    "totalPages": 9
  }
  ```

### GET `/{id}`
- 404 quando a transação não pertence ao usuário.

### POST `/`
- Corpo mínimo:
  ```json
  {
    "amount": 120.00,
    "type": 2,
    "description": "Supermercado",
    "categoryId": 5,
    "date": "2025-11-01"
  }
  ```
- 201 Created com `TransactionResponseDto`.
- 400 Bad Request se a categoria não for do usuário.

### PUT `/{id}`
- Campos opcionais (`amount`, `type`, `description`, `categoryId`, `date`).

### DELETE `/{id}`
- 204 em sucesso, 404 se não existir.

---

## Transações recorrentes (`/api/recurringtransactions`)

### GET `/`
- Query: `page`, `pageSize`, `sortBy` (`amount|description|frequency|startDate`), `sortOrder`, `isActive`.
- Retorna `PagedResult<RecurringTransactionResponseDto>` com campos `frequencyDescription`, `dayOfMonth`, `dayOfWeek`, `lastGeneratedDate`, `nextGenerationDate`.

### GET `/all`
- Lista sem paginação (aceita `isActive`).

### GET `/{id}` | POST `/` | PUT `/{id}` | DELETE `/{id}`
- Criação exige:
  ```json
  {
    "amount": 5000,
    "type": 1,
    "description": "Salário",
    "categoryId": 2,
    "frequency": 3,
    "startDate": "2025-11-01",
    "dayOfMonth": 1
  }
  ```
- Atualização permite ajustar `amount`, `description`, `endDate`.

### PATCH `/{id}/toggle-active`
- Alterna entre ativo/inativo. 404 se não pertencer ao usuário.

---

## Metas (`/api/goals`)

### GET `/`
- Query: `page`, `pageSize`, `sortBy` (`targetAmount|category|month|year`), `sortOrder`, `month`, `year`.
- Retorna `PagedResult<GoalResponseDto>`.

### GET `/all`
- Lista metas sem paginação (suporta `month`, `year`).

### GET `/{id}` | POST `/` | PUT `/{id}` | DELETE `/{id}`
- Criação requer `categoryId`, `targetAmount`, `month`, `year`.
- PUT aceita apenas `targetAmount`.

### GET `/status?month=&year=`
- Retorna `GoalStatusDto` com `currentAmount`, `percentage` e `status` (`Dentro`, `Em risco`, `Atingida`, etc.).

---

## Relatórios (`/api/reports`)

### GET `/summary`
- Query: `startDate`, `endDate`.
- Exemplo:
  ```json
  {
    "totalIncome": 12000.00,
    "totalExpenses": 8200.00,
    "balance": 3800.00,
    "transactionCount": 94,
    "startDate": "2025-08-01T00:00:00Z",
    "endDate": "2025-10-31T23:59:59Z"
  }
  ```

### GET `/categories`
- Query: `startDate`, `endDate`, `expensesOnly` (default `true`).
- Retorna lista de `CategoryReportDto`.

### GET `/monthly?months=12`
- Retorna histórico dos últimos `n` meses (`MonthlyReportDto`).

### GET `/detailed`
- Junta `summary`, `topExpenseCategories`, `topIncomeCategories`, `monthlyHistory` em um `DetailedReportDto`.

---

## Exportação (`/api/export`)

### CSV
- `GET /transactions/csv?startDate=&endDate=`
- `GET /categories/csv`
- `GET /goals/csv?month=&year=`
- Resposta: arquivo `text/csv` gerado pelo CsvHelper com cabeçalhos em português.

### PDF
- `GET /transactions/pdf?startDate=&endDate=`
- `GET /financial-report/pdf?startDate=&endDate=`
- `GET /goals-report/pdf?month=&year=`
- Resposta: `application/pdf` produzido pelo QuestPDF.

---

## Backups (`/api/backups`)

- `GET /` — Lista backups disponíveis no diretório configurado. Retorna `fileName`, `sizeBytes` e `createdAt`.
- `POST /?download=false` — Gera novo backup. Se `download=true`, faz o download imediato do arquivo compactado (`application/gzip`). Caso contrário, grava no disco e responde com `fileName` e `storedOnDisk` (sem expor o caminho físico).
- `GET /{fileName}` — Download de um backup específico previamente gerado.
- `POST /restore` — Recebe `multipart/form-data` com o arquivo `.json.gz` e restaura todo o snapshot do banco, incluindo contas de usuário e papéis.

> Todos os endpoints requerem autenticação JWT. Use contas com privilégios administrativos, já que a restauração faz um replace completo dos dados.

---

## Requisições autenticadas

Inclua sempre:
```
Authorization: Bearer <seu_jwt>
Content-Type: application/json
```

Exemplo PowerShell:
```powershell
Invoke-RestMethod -Method Get "https://localhost:7035/api/transactions?page=1" -Headers @{ Authorization = "Bearer $token" }
```

---

## Referências úteis
- [AUTHENTICATION.md](./AUTHENTICATION.md)
- [ENVIRONMENT_VARIABLES.md](./ENVIRONMENT_VARIABLES.md)
- [ERROR_MESSAGES.md](./ERROR_MESSAGES.md)
- [DATABASE_SCHEMA.md](./DATABASE_SCHEMA.md)
