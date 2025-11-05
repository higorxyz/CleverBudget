# 🔐 Sistema de Autenticação - CleverBudget

O CleverBudget usa **ASP.NET Core Identity** para gerenciamento de usuários, combinado com **JWT** para autenticação stateless. Todas as rotas que precisam de login exigem um token emitido pelos endpoints de autenticação.

## Fluxo resumido

1. Usuário faz `POST /api/v2/auth/register` ou `POST /api/v2/auth/login`.
2. `AuthService` valida credenciais com o `UserManager<User>` do Identity.
3. Um `AuthResponseDto` é retornado com o token JWT, dados básicos do usuário e a data de expiração (`ExpiresAt`).
4. O front-end envia o token no header `Authorization: Bearer <token>` para acessar rotas protegidas.

> **Nota:** A versão v1 (`/api/auth/*`) permanece disponível para compatibilidade com integrações existentes.

## Endpoints principais (v2)

### Registrar usuário — `POST /api/v2/auth/register`

Request:
```json
{
  "firstName": "Maria",
  "lastName": "Silva",
  "email": "maria.silva@example.com",
  "password": "SenhaForte123",
  "confirmPassword": "SenhaForte123"
}
```

Response 200:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "maria.silva@example.com",
  "firstName": "Maria",
  "lastName": "Silva",
  "expiresAt": "2024-11-04T15:30:45.123Z"
}
```

Erros mapeados pelo `AuthService`:
- `PASSWORD_MISMATCH` – nova senha e confirmação diferentes.
- `EMAIL_ALREADY_EXISTS` – e-mail já cadastrado.
- Códigos do Identity (`PasswordTooShort`, `PasswordRequiresUpper`, etc.) são convertidos para mensagens em português antes de retornar `400`.

### Login — `POST /api/v2/auth/login`

Request:
```json
{
  "email": "maria.silva@example.com",
  "password": "SenhaForte123"
}
```

Response 200: mesmo formato do registro (`AuthResponseDto`).

Erros:
- `401 Unauthorized` com `INVALID_CREDENTIALS` para e-mail inexistente ou senha incorreta. A mensagem é intencionalmente genérica para evitar enumeração de usuários.

### Alterar senha — `PUT /api/v2/profile/password`

Header obrigatório: `Authorization: Bearer <token>`

Request:
```json
{
  "currentPassword": "SenhaAtual123",
  "newPassword": "NovaSenha456",
  "confirmPassword": "NovaSenha456"
}
```

Respostas possíveis:
- `200 OK` – `{ "message": "Senha alterada com sucesso" }`
- `400 Bad Request` com:
  - `PASSWORD_MISMATCH` – confirmação não confere.
  - Códigos do Identity (`PasswordMismatch`, `PasswordTooShort`, etc.) convertidos para mensagens amigáveis.
- `404 Not Found` com `USER_NOT_FOUND` – usuário não existe (token inválido ou removido).

## Token JWT

- Algoritmo: `HS256`.
- Claims emitidas:
  - `nameid` (`ClaimTypes.NameIdentifier`): ID do usuário.
  - `email`: e-mail cadastrado.
  - `name`: nome completo.
  - `jti`: identificador único do token.
- A expiração padrão vem de `JwtSettings:ExpirationMinutes` (1 hora). O valor é retornado em `ExpiresAt` no UTC.

### Configuração

`appsettings.json` define o bloco `JwtSettings`:
```json
{
  "JwtSettings": {
    "SecretKey": "",
    "Issuer": "CleverBudget",
    "Audience": "CleverBudgetUsers",
    "ExpirationMinutes": 60
  }
}
```

- Em desenvolvimento, uma chave temporária é usada se `SecretKey` estiver vazia; produção **deve** definir `JwtSettings__SecretKey` via variável de ambiente.
- `Issuer` e `Audience` são lidos diretamente desse bloco; ajuste-os no mesmo local.

### Validação na API

`Program.cs` registra o middleware JWT com `ValidateIssuer` e `ValidateAudience` habilitados. Falha na validação gera `401`.

## Regras de senha (Identity)

Configuradas em `Program.cs`:
- Tamanho mínimo: 6 caracteres.
- Obrigatório ter número, minúscula e maiúscula.
- Caractere especial **não** é exigido (`RequireNonAlphanumeric = false`).
- E-mails devem ser únicos (`RequireUniqueEmail = true`).

## Extras do fluxo

- Durante o registro, categorias padrão são criadas automaticamente em background.
- Um e-mail de boas-vindas é enviado via Brevo quando a API key está configurada.
- O `UserProfileService` expõe `GET /api/v2/profile` e `PUT /api/v2/profile` para atualizar nome e foto.

## Uso nos clientes

```bash
# obter token
curl -X POST https://localhost:5001/api/v2/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"maria.silva@example.com","password":"SenhaForte123"}'

# usar token
curl https://localhost:5001/api/v2/transactions \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

Em aplicações web/mobile, armazene o token com segurança (ex.: Secure Storage). Sempre envie no header `Authorization`.

## Boas práticas implementadas

- Expiração curta dos tokens (1 hora) e assinatura com chave de 32+ caracteres.
- Mensagens de erro genéricas no login.
- Todas as consultas filtram pelo `UserId` recuperado do token.
- HTTPS recomendado/obrigatório em produção.

## Próximos passos desejáveis

1. Refresh tokens para renovar sessões sem pedir login constante.
2. Confirmação de e-mail antes de ativar a conta.
3. MFA/2FA opcional.
4. Política de revogação (blacklist) para tokens comprometidos.

## Referências

- `CleverBudget.Api/Controllers/AuthController.cs`
- `CleverBudget.Infrastructure/Services/AuthService.cs`
- `CleverBudget.Api/Controllers/ProfileController.cs`
