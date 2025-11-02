# 🔐 Sistema de Autenticação - CleverBudget

## 📋 Visão Geral

O CleverBudget utiliza **JWT (JSON Web Tokens)** para autenticação stateless, combinado com **ASP.NET Core Identity** para gerenciamento de usuários.

## 🔑 Fluxo de Autenticação

```
┌─────────┐                                    ┌─────────┐
│ Cliente │                                    │   API   │
└────┬────┘                                    └────┬────┘
     │                                              │
     │  1. POST /api/auth/register                  │
     │  { email, password, confirmPassword }        │
     ├─────────────────────────────────────────────>│
     │                                              │
     │                                         2. Valida dados
     │                                         3. Cria usuário (Identity)
     │                                         4. Gera JWT token
     │                                              │
     │  5. { token, email, expiresIn }              │
     │<─────────────────────────────────────────────┤
     │                                              │
     │  6. POST /api/transactions                   │
     │  Authorization: Bearer <token>               │
     ├─────────────────────────────────────────────>│
     │                                              │
     │                                         7. Valida token
     │                                         8. Extrai userId
     │                                         9. Processa requisição
     │                                              │
     │  10. { ... dados ... }                       │
     │<─────────────────────────────────────────────┤
     │                                              │
```

## 🛠️ Endpoints de Autenticação

### 1. Registro de Usuário

**`POST /api/auth/register`**

#### Request Body
```json
{
  "email": "usuario@example.com",
  "password": "SenhaForte123!",
  "confirmPassword": "SenhaForte123!"
}
```

#### Success Response (200 OK)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "usuario@example.com",
  "expiresIn": 3600
}
```

#### Error Responses

**400 Bad Request - Senhas não conferem**
```json
{
  "message": "As senhas não conferem. Por favor, digite a mesma senha nos dois campos.",
  "errorCode": "PASSWORD_MISMATCH"
}
```

**400 Bad Request - E-mail já existe**
```json
{
  "message": "Já existe uma conta com esse e-mail. Tente fazer login ou use outro e-mail.",
  "errorCode": "EMAIL_ALREADY_EXISTS"
}
```

**400 Bad Request - Senha muito curta**
```json
{
  "message": "A senha deve ter no mínimo 6 caracteres",
  "errorCode": "PasswordTooShort"
}
```

**Outros códigos de erro:**
- `PasswordRequiresNonAlphanumeric` - Requer caractere especial
- `PasswordRequiresDigit` - Requer número
- `PasswordRequiresUpper` - Requer letra maiúscula
- `PasswordRequiresLower` - Requer letra minúscula
- `InvalidEmail` - Formato de e-mail inválido

---

### 2. Login

**`POST /api/auth/login`**

#### Request Body
```json
{
  "email": "usuario@example.com",
  "password": "SenhaForte123!"
}
```

#### Success Response (200 OK)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "usuario@example.com",
  "expiresIn": 3600
}
```

#### Error Response

**401 Unauthorized - Credenciais inválidas**
```json
{
  "message": "E-mail ou senha incorretos. Verifique seus dados e tente novamente.",
  "errorCode": "INVALID_CREDENTIALS"
}
```

⚠️ **Nota de Segurança:** Por motivos de segurança, o sistema retorna a mesma mensagem genérica tanto para e-mail inexistente quanto para senha incorreta, evitando enumeration attacks.

---

### 3. Alterar Senha

**`POST /api/profile/change-password`**

**Requer autenticação:** ✅ Sim (Bearer Token)

#### Request Body
```json
{
  "currentPassword": "SenhaAntiga123!",
  "newPassword": "SenhaNova456!",
  "confirmNewPassword": "SenhaNova456!"
}
```

#### Success Response (200 OK)
```json
{
  "message": "Senha alterada com sucesso"
}
```

#### Error Responses

**400 Bad Request - Senhas novas não conferem**
```json
{
  "message": "As senhas não conferem. Por favor, digite a mesma senha nos dois campos.",
  "errorCode": "PASSWORD_MISMATCH"
}
```

**400 Bad Request - Senha atual incorreta**
```json
{
  "message": "A senha atual está incorreta",
  "errorCode": "PasswordMismatch"
}
```

**404 Not Found - Usuário não encontrado**
```json
{
  "message": "Usuário não encontrado",
  "errorCode": "USER_NOT_FOUND"
}
```

---

## 🔐 JWT (JSON Web Token)

### Estrutura do Token

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.  ← Header
eyJzdWIiOiJ1c2VyQGV4YW1wbGUuY29tIiwi...  ← Payload
SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c   ← Signature
```

### Claims no Payload

```json
{
  "sub": "123e4567-e89b-12d3-a456-426614174000",  // User ID
  "email": "usuario@example.com",                 // E-mail
  "jti": "unique-token-id",                        // Token ID
  "iat": 1699000000,                               // Issued At
  "exp": 1699003600,                               // Expiration (1h)
  "iss": "CleverBudgetAPI",                        // Issuer
  "aud": "CleverBudgetClient"                      // Audience
}
```

### Configuração

No `appsettings.json`:

```json
{
  "Jwt": {
    "Key": "sua-chave-secreta-aqui-minimo-32-caracteres",
    "Issuer": "CleverBudgetAPI",
    "Audience": "CleverBudgetClient",
    "ExpiryInMinutes": 60
  }
}
```

⚠️ **IMPORTANTE:** 
- Use uma chave forte com no mínimo 32 caracteres
- Nunca commite a chave em repositórios públicos
- Use variáveis de ambiente em produção

---

## 🛡️ Usando o Token

### No Frontend (JavaScript)

```javascript
// 1. Armazene o token após login/registro
const response = await fetch('/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, password })
});

const { token } = await response.json();
localStorage.setItem('authToken', token);

// 2. Use o token em requisições autenticadas
const authToken = localStorage.getItem('authToken');

const transactionsResponse = await fetch('/api/transactions', {
  headers: {
    'Authorization': `Bearer ${authToken}`,
    'Content-Type': 'application/json'
  }
});
```

### No Postman/Insomnia

1. Faça login e copie o token
2. Vá em **Authorization**
3. Selecione **Bearer Token**
4. Cole o token no campo

### No cURL

```bash
curl -X GET "https://localhost:5001/api/transactions" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

---

## 👤 ASP.NET Core Identity

### Configuração de Senha

No `Program.cs`:

```csharp
builder.Services.Configure<IdentityOptions>(options =>
{
    // Requisitos de senha
    options.Password.RequireDigit = true;           // Requer número
    options.Password.RequireLowercase = true;       // Requer minúscula
    options.Password.RequireNonAlphanumeric = true; // Requer especial
    options.Password.RequireUppercase = true;       // Requer maiúscula
    options.Password.RequiredLength = 6;            // Tamanho mínimo
    options.Password.RequiredUniqueChars = 1;       // Caracteres únicos

    // Configurações de conta
    options.User.RequireUniqueEmail = true;         // E-mail único
    options.SignIn.RequireConfirmedEmail = false;   // Confirmar e-mail
});
```

### Entidade User

```csharp
public class User : IdentityUser
{
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
    public ICollection<RecurringTransaction> RecurringTransactions { get; set; } = new List<RecurringTransaction>();
}
```

---

## 🔒 Autorização

### Protegendo Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // ← Requer autenticação
public class TransactionsController : ControllerBase
{
    // Todos os métodos requerem autenticação
}
```

### Protegendo Métodos Específicos

```csharp
[HttpGet("public")]
[AllowAnonymous]  // ← Permite acesso sem autenticação
public IActionResult GetPublicData()
{
    return Ok("Dados públicos");
}

[HttpGet("private")]
[Authorize]  // ← Requer autenticação
public IActionResult GetPrivateData()
{
    return Ok("Dados privados");
}
```

### Obtendo o Usuário Atual

```csharp
[HttpGet]
[Authorize]
public async Task<IActionResult> GetMyTransactions()
{
    // Extrair User ID do token JWT
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    
    var transactions = await _transactionService.GetByUserIdAsync(userId);
    return Ok(transactions);
}
```

---

## 🧪 Testando Autenticação

### Teste de Registro

```bash
curl -X POST "https://localhost:5001/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "teste@example.com",
    "password": "Teste123!",
    "confirmPassword": "Teste123!"
  }'
```

### Teste de Login

```bash
curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "teste@example.com",
    "password": "Teste123!"
  }'
```

### Teste de Endpoint Protegido

```bash
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

curl -X GET "https://localhost:5001/api/transactions" \
  -H "Authorization: Bearer $TOKEN"
```

---

## ⚠️ Segurança e Boas Práticas

### ✅ O que fazemos

- ✅ Tokens JWT com expiração de 1 hora
- ✅ Senhas hasheadas com PBKDF2 (via Identity)
- ✅ Validação de senha forte (maiúsculas, números, especiais)
- ✅ Mensagens de erro genéricas no login (anti-enumeration)
- ✅ HTTPS em produção
- ✅ Tokens armazenados apenas no cliente (stateless)

### 🚨 Vulnerabilidades Comuns a Evitar

- ❌ **Não** armazene senhas em texto puro
- ❌ **Não** use tokens sem expiração
- ❌ **Não** revele se o e-mail existe ou não no login
- ❌ **Não** commite chaves secretas no Git
- ❌ **Não** envie tokens em URLs (use headers)
- ❌ **Não** aceite senhas fracas (menos de 6 caracteres)

### 🔐 Melhorias Futuras

- [ ] Refresh Tokens para renovação automática
- [ ] Two-Factor Authentication (2FA)
- [ ] Confirmação de e-mail obrigatória
- [ ] Rate limiting anti-brute force
- [ ] Revogação de tokens (blacklist)
- [ ] OAuth2/OpenID Connect (Google, Facebook)

---

## 📚 Documentos Relacionados

- [Mensagens de Erro](./ERROR_MESSAGES.md) - Todos os códigos de erro
- [Endpoints](./ENDPOINTS.md) - Lista completa de endpoints
- [Segurança](./SECURITY.md) - Práticas de segurança
- [Testes](./TESTING.md) - Como testar autenticação
