# 📡 API Endpoints - CleverBudget

## 🌐 Base URL

- **Development:** `https://localhost:5001/api`
- **Production:** `https://cleverbudget-production.up.railway.app/api`

## 📋 Índice de Endpoints

- [🔐 Autenticação](#-autenticação)
- [💰 Transações](#-transações)
- [📁 Categorias](#-categorias)
- [🎯 Metas](#-metas)
- [🔄 Transações Recorrentes](#-transações-recorrentes)
- [📊 Relatórios](#-relatórios)
- [📥 Exportação](#-exportação)
- [👤 Perfil](#-perfil)

---

## 🔐 Autenticação

**Base:** `/api/auth`

### POST `/api/auth/register`

Registra um novo usuário.

**Autenticação:** Não requerida

**Request Body:**
```json
{
  "email": "usuario@example.com",
  "password": "SenhaForte123!",
  "confirmPassword": "SenhaForte123!"
}
```

**Success Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "usuario@example.com",
  "expiresIn": 3600
}
```

**Error Responses:**
- `400 Bad Request` - Validação falhou ([ver códigos](./ERROR_MESSAGES.md#register))

---

### POST `/api/auth/login`

Autentica um usuário existente.

**Autenticação:** Não requerida

**Request Body:**
```json
{
  "email": "usuario@example.com",
  "password": "SenhaForte123!"
}
```

**Success Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "usuario@example.com",
  "expiresIn": 3600
}
```

**Error Responses:**
- `401 Unauthorized` - Credenciais inválidas

---

## 💰 Transações

**Base:** `/api/transactions`

**Autenticação:** ✅ Requerida (todas as rotas)

### GET `/api/transactions`

Lista todas as transações do usuário autenticado com filtros opcionais.

**Query Parameters:**
```
?startDate=2024-01-01          # Data inicial (opcional)
&endDate=2024-12-31            # Data final (opcional)
&type=0                        # 0=Expense, 1=Income (opcional)
&categoryId=5                  # ID da categoria (opcional)
&page=1                        # Número da página (default: 1)
&pageSize=10                   # Itens por página (default: 10)
```

**Success Response (200 OK):**
```json
{
  "items": [
    {
      "id": 1,
      "userId": "123e4567-e89b-12d3-a456-426614174000",
      "categoryId": 5,
      "categoryName": "Alimentação",
      "amount": 45.50,
      "description": "Almoço no restaurante",
      "date": "2024-11-01T12:30:00Z",
      "type": 0,
      "imageUrl": "https://res.cloudinary.com/...",
      "createdAt": "2024-11-01T12:35:00Z",
      "updatedAt": null
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 10,
  "totalPages": 15
}
```

---

### GET `/api/transactions/{id}`

Obtém uma transação específica.

**Path Parameters:**
- `id` (int) - ID da transação

**Success Response (200 OK):**
```json
{
  "id": 1,
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "categoryId": 5,
  "categoryName": "Alimentação",
  "amount": 45.50,
  "description": "Almoço no restaurante",
  "date": "2024-11-01T12:30:00Z",
  "type": 0,
  "imageUrl": "https://res.cloudinary.com/...",
  "createdAt": "2024-11-01T12:35:00Z",
  "updatedAt": null
}
```

**Error Responses:**
- `404 Not Found` - Transação não encontrada

---

### POST `/api/transactions`

Cria uma nova transação.

**Request Body:**
```json
{
  "categoryId": 5,
  "amount": 45.50,
  "description": "Almoço no restaurante",
  "date": "2024-11-01T12:30:00Z",
  "type": 0,
  "imageUrl": "https://res.cloudinary.com/..."  // opcional
}
```

**Success Response (201 Created):**
```json
{
  "id": 1,
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "categoryId": 5,
  "categoryName": "Alimentação",
  "amount": 45.50,
  "description": "Almoço no restaurante",
  "date": "2024-11-01T12:30:00Z",
  "type": 0,
  "imageUrl": "https://res.cloudinary.com/...",
  "createdAt": "2024-11-01T12:35:00Z",
  "updatedAt": null
}
```

**Error Responses:**
- `400 Bad Request` - Validação falhou
- `404 Not Found` - Categoria não encontrada

---

### PUT `/api/transactions/{id}`

Atualiza uma transação existente.

**Path Parameters:**
- `id` (int) - ID da transação

**Request Body:**
```json
{
  "categoryId": 6,
  "amount": 50.00,
  "description": "Almoço no restaurante (atualizado)",
  "date": "2024-11-01T12:30:00Z",
  "type": 0,
  "imageUrl": "https://res.cloudinary.com/..."
}
```

**Success Response (200 OK):**
```json
{
  "id": 1,
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "categoryId": 6,
  "categoryName": "Restaurantes",
  "amount": 50.00,
  "description": "Almoço no restaurante (atualizado)",
  "date": "2024-11-01T12:30:00Z",
  "type": 0,
  "imageUrl": "https://res.cloudinary.com/...",
  "createdAt": "2024-11-01T12:35:00Z",
  "updatedAt": "2024-11-01T14:20:00Z"
}
```

**Error Responses:**
- `400 Bad Request` - Validação falhou
- `404 Not Found` - Transação ou categoria não encontrada

---

### DELETE `/api/transactions/{id}`

Deleta uma transação.

**Path Parameters:**
- `id` (int) - ID da transação

**Success Response (204 No Content)**

**Error Responses:**
- `404 Not Found` - Transação não encontrada

---

## 📁 Categorias

**Base:** `/api/categories`

**Autenticação:** ✅ Requerida (todas as rotas)

### GET `/api/categories`

Lista todas as categorias do usuário.

**Query Parameters:**
```
?type=0    # 0=Expense, 1=Income (opcional)
```

**Success Response (200 OK):**
```json
[
  {
    "id": 1,
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "name": "Alimentação",
    "type": 0,
    "createdAt": "2024-01-01T10:00:00Z"
  },
  {
    "id": 2,
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "name": "Salário",
    "type": 1,
    "createdAt": "2024-01-01T10:05:00Z"
  }
]
```

---

### GET `/api/categories/{id}`

Obtém uma categoria específica.

**Success Response (200 OK):**
```json
{
  "id": 1,
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Alimentação",
  "type": 0,
  "createdAt": "2024-01-01T10:00:00Z"
}
```

**Error Responses:**
- `404 Not Found` - Categoria não encontrada

---

### POST `/api/categories`

Cria uma nova categoria.

**Request Body:**
```json
{
  "name": "Alimentação",
  "type": 0
}
```

**Success Response (201 Created):**
```json
{
  "id": 1,
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Alimentação",
  "type": 0,
  "createdAt": "2024-11-01T10:00:00Z"
}
```

**Error Responses:**
- `400 Bad Request` - Validação falhou ou categoria duplicada

---

### PUT `/api/categories/{id}`

Atualiza uma categoria.

**Request Body:**
```json
{
  "name": "Alimentação Fora",
  "type": 0
}
```

**Success Response (200 OK):**
```json
{
  "id": 1,
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Alimentação Fora",
  "type": 0,
  "createdAt": "2024-01-01T10:00:00Z"
}
```

---

### DELETE `/api/categories/{id}`

Deleta uma categoria.

**Success Response (204 No Content)**

**Error Responses:**
- `404 Not Found` - Categoria não encontrada
- `409 Conflict` - Categoria em uso por transações (implementação futura)

---

## 🎯 Metas

**Base:** `/api/goals`

**Autenticação:** ✅ Requerida

### GET `/api/goals`

Lista todas as metas do usuário.

**Success Response (200 OK):**
```json
[
  {
    "id": 1,
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "name": "Férias 2025",
    "targetAmount": 5000.00,
    "currentAmount": 2500.00,
    "deadline": "2025-12-31T23:59:59Z",
    "progressPercentage": 50.0,
    "isCompleted": false,
    "createdAt": "2024-01-01T00:00:00Z"
  }
]
```

---

### POST `/api/goals`

Cria uma nova meta.

**Request Body:**
```json
{
  "name": "Férias 2025",
  "targetAmount": 5000.00,
  "currentAmount": 0.00,
  "deadline": "2025-12-31T23:59:59Z"
}
```

**Success Response (201 Created):**
```json
{
  "id": 1,
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "Férias 2025",
  "targetAmount": 5000.00,
  "currentAmount": 0.00,
  "deadline": "2025-12-31T23:59:59Z",
  "progressPercentage": 0.0,
  "isCompleted": false,
  "createdAt": "2024-11-01T00:00:00Z"
}
```

---

### PUT `/api/goals/{id}`

Atualiza uma meta (geralmente para adicionar valor a `currentAmount`).

**Request Body:**
```json
{
  "name": "Férias 2025",
  "targetAmount": 5000.00,
  "currentAmount": 3000.00,
  "deadline": "2025-12-31T23:59:59Z"
}
```

---

### DELETE `/api/goals/{id}`

Deleta uma meta.

**Success Response (204 No Content)**

---

## 🔄 Transações Recorrentes

**Base:** `/api/recurringtransactions`

**Autenticação:** ✅ Requerida

### GET `/api/recurringtransactions`

Lista todas as transações recorrentes ativas.

**Success Response (200 OK):**
```json
[
  {
    "id": 1,
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "categoryId": 10,
    "categoryName": "Salário",
    "amount": 5000.00,
    "description": "Salário mensal",
    "startDate": "2024-01-01T00:00:00Z",
    "frequency": 2,
    "isActive": true,
    "nextOccurrence": "2024-12-01T00:00:00Z",
    "createdAt": "2024-01-01T00:00:00Z"
  }
]
```

**Frequency Enum:**
- `0` = Daily
- `1` = Weekly
- `2` = Monthly
- `3` = Yearly

---

### POST `/api/recurringtransactions`

Cria uma transação recorrente.

**Request Body:**
```json
{
  "categoryId": 10,
  "amount": 5000.00,
  "description": "Salário mensal",
  "startDate": "2024-01-01T00:00:00Z",
  "frequency": 2
}
```

---

### PUT `/api/recurringtransactions/{id}`

Atualiza uma transação recorrente.

---

### DELETE `/api/recurringtransactions/{id}`

Deleta uma transação recorrente.

**Success Response (204 No Content)**

---

## 📊 Relatórios

**Base:** `/api/reports`

**Autenticação:** ✅ Requerida

### GET `/api/reports/summary`

Obtém resumo financeiro por período.

**Query Parameters:**
```
?startDate=2024-01-01&endDate=2024-12-31
```

**Success Response (200 OK):**
```json
{
  "totalIncome": 60000.00,
  "totalExpense": 45000.00,
  "balance": 15000.00,
  "expenseByCategory": [
    {
      "categoryName": "Alimentação",
      "total": 12000.00,
      "percentage": 26.67
    },
    {
      "categoryName": "Transporte",
      "total": 8000.00,
      "percentage": 17.78
    }
  ],
  "incomeByCategory": [
    {
      "categoryName": "Salário",
      "total": 60000.00,
      "percentage": 100.0
    }
  ]
}
```

---

## 📥 Exportação

**Base:** `/api/export`

**Autenticação:** ✅ Requerida

### GET `/api/export/transactions/csv`

Exporta transações em formato CSV.

**Query Parameters:**
```
?startDate=2024-01-01&endDate=2024-12-31
```

**Success Response (200 OK):**
```
Content-Type: text/csv
Content-Disposition: attachment; filename="transactions_2024.csv"

Data,Descrição,Categoria,Tipo,Valor
2024-01-01,Almoço,Alimentação,Despesa,45.50
2024-01-02,Salário,Salário,Receita,5000.00
...
```

---

## 👤 Perfil

**Base:** `/api/profile`

**Autenticação:** ✅ Requerida

### POST `/api/profile/change-password`

Altera a senha do usuário.

**Request Body:**
```json
{
  "currentPassword": "SenhaAntiga123!",
  "newPassword": "SenhaNova456!",
  "confirmNewPassword": "SenhaNova456!"
}
```

**Success Response (200 OK):**
```json
{
  "message": "Senha alterada com sucesso"
}
```

**Error Responses:**
- `400 Bad Request` - Senhas não conferem ou senha atual incorreta
- `404 Not Found` - Usuário não encontrado

---

## 🔐 Autenticação nos Endpoints

Todos os endpoints protegidos requerem o header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Exemplo com cURL

```bash
curl -X GET "https://localhost:5001/api/transactions" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json"
```

### Exemplo com JavaScript

```javascript
const response = await fetch('/api/transactions', {
  headers: {
    'Authorization': `Bearer ${localStorage.getItem('authToken')}`,
    'Content-Type': 'application/json'
  }
});
```

---

## 📚 Documentos Relacionados

- [Mensagens de Erro](./ERROR_MESSAGES.md) - Códigos de erro completos
- [Autenticação](./AUTHENTICATION.md) - Detalhes de JWT e login
- [Database Schema](./DATABASE_SCHEMA.md) - Estrutura das tabelas
