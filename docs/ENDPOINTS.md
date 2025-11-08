# API Endpoints – CleverBudget

## Bases de URL
- Desenvolvimento HTTP: `http://localhost:5220/api/v2`
- Desenvolvimento HTTPS: `https://localhost:7035/api/v2`
- Produção (Railway): `https://cleverbudget-production.up.railway.app/api/v2`
- Versão padrão: `v2`. Ajuste o segmento após `/api/` para acessar versões anteriores, se disponíveis.

## Convenções
- Endpoints marcados com "Requer token" precisam do header `Authorization: Bearer <jwt>`.
- Respostas paginadas usam `PagedResult<T>` com campos `items`, `page`, `pageSize`, `totalCount`, `totalPages`, `hasPreviousPage`, `hasNextPage`.
- Datas aceitam `yyyy-MM-dd` ou ISO-8601.
- Enums relevantes:
  - `TransactionType`: `1=Income`, `2=Expense`
  - `RecurrenceFrequency`: `1=Daily`, `2=Weekly`, `3=Monthly`, `4=Yearly`

---

## Autenticação (`/api/v2/auth`)

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

## Perfil (`/api/v2/profile`)

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

## Orçamentos (`/api/v2/budgets`)

### GET `/`
- Query opcional: `year`, `month`, `scope`, `view`.
- `scope=current` retorna apenas os orçamentos do mês vigente.
- `view=summary` retorna um objeto com totais agregados (exemplo abaixo).
- Sem parâmetros especiais, 200 OK: lista de `BudgetResponseDto` com campos como `amount`, `spent`, `remaining`, `percentageUsed`, status (`Normal`, `Alerta`, `Crítico`, `Excedido`).

### GET `/paged`
- Query extra: `page`, `pageSize` (máx 100), `sortBy`, `sortOrder`.
- Retorna `PagedResult<BudgetResponseDto>`.

### GET `/{id}` | GET `/category/{categoryId}/period?month=&year=`
- Buscam orçamento específico ou por categoria/período.

#### Exemplo de resumo (`GET /?view=summary`)
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

## Categorias (`/api/v2/categories`)

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

## Transações (`/api/v2/transactions`)

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

## Transações recorrentes (`/api/v2/recurringtransactions`)

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
- Atualização permite ajustar `amount`, `description`, `endDate` e `isActive`.
- Para desativar/reativar use `PUT /api/v2/recurringtransactions/{id}` com `{"isActive": false}` ou `{"isActive": true}`.

---

## Metas (`/api/v2/goals`)

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

## Relatórios (`/api/v2/reports`)

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

## Insights Financeiros (`/api/v2/insights`)

### GET `/`
- Query: `startDate`, `endDate`, `categoryId`, `includeIncomeInsights`, `includeExpenseInsights`.
- Requer token. Constrói uma lista de `FinancialInsightDto` ordenados por severidade.
- Cada insight inclui `category`, `severity`, `title`, `summary`, `recommendation`, `impactAmount`, `benchmarkAmount`, `generatedAt` e `dataPoints`.
- Use `includeIncomeInsights=false` ou `includeExpenseInsights=false` para focar em um tipo de análise.

#### Exemplo de resposta
```json
[
  {
    "category": "SpendingPattern",
    "severity": "High",
    "title": "Gastos elevados em Restaurantes",
    "summary": "Os gastos atuais estão 60% acima da média dos últimos meses.",
    "recommendation": "Analise as transações excepcionais e limite novos gastos até equilibrar o orçamento.",
    "impactAmount": 180.0,
    "benchmarkAmount": 300.0,
    "generatedAt": "2025-11-07T12:34:56Z",
    "dataPoints": [
      {
        "label": "Mês atual",
        "value": 480.0,
        "benchmark": 300.0
      }
    ]
  }
]
```

---

## Exportação (`/api/v2/export`)

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

## Backups (`/api/v2/backups`)

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
Invoke-RestMethod -Method Get "https://localhost:7035/api/v2/transactions?page=1" -Headers @{ Authorization = "Bearer $token" }
```

---

## Referências úteis
- [AUTHENTICATION.md](./AUTHENTICATION.md)
- [ENVIRONMENT_VARIABLES.md](./ENVIRONMENT_VARIABLES.md)
- [ERROR_MESSAGES.md](./ERROR_MESSAGES.md)
- [DATABASE_SCHEMA.md](./DATABASE_SCHEMA.md)

---

## Legacy v1

A versão v1 permanece acessível para integrações existentes e mantém o comportamento original sem versionamento segmentado.

- Base URL: `http(s)://<host>/api`
- Principais características:
  - `GET /api/budgets/summary` e `GET /api/budgets/current` continuam disponíveis.
  - Alternar status de transações recorrentes é feito via `POST /api/recurringtransactions/{id}/toggle`.
  - Não há cabeçalhos ETag ou tratamento condicional de cache.
  - Rotas aceitam os mesmos payloads dos exemplos acima, exceto pelos ajustes mencionados.

Planeje migrar para `/api/v2` para aproveitar os recursos mais recentes (versionamento explícito, rotas mais RESTful e respostas com ETag). Enquanto isso, utilize esta seção como referência de compatibilidade.
