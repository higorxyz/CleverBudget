# 📋 Guia de Mensagens de Erro da API

> **Documentação completa de todas as mensagens de erro retornadas pela CleverBudget API**

Este documento lista todas as possíveis mensagens de erro retornadas pela API, com seus códigos de erro (`errorCode`) e quando ocorrem. Use este guia para implementar tratamento de erros no frontend.

> **Nota sobre versionamento:** Os exemplos abaixo usam rotas v2 (`/api/v2/*`). A versão v1 (`/api/*`) retorna os mesmos códigos e mensagens de erro.

---

## 🔐 Autenticação (`/api/v2/auth`)

### **POST /api/v2/auth/register** - Cadastro de Usuário

#### ✅ **Sucesso (200)**
```json
{
  "token": "eyJhbGc...",
  "email": "usuario@example.com",
  "firstName": "João",
  "lastName": "Silva",
  "expiresAt": "2025-11-02T15:30:00Z"
}
```

#### ❌ **Erros (400 Bad Request)**

| Mensagem | ErrorCode | Quando Ocorre |
|----------|-----------|---------------|
| `"As senhas não conferem. Por favor, digite a mesma senha nos dois campos."` | `PASSWORD_MISMATCH` | Quando `password` ≠ `confirmPassword` |
| `"Já existe uma conta com esse e-mail. Tente fazer login ou use outro e-mail."` | `EMAIL_ALREADY_EXISTS` | E-mail já cadastrado no sistema |
| `"A senha deve ter no mínimo 6 caracteres."` | `PasswordTooShort` | Senha menor que o mínimo configurado |
| `"A senha deve conter pelo menos um caractere especial (!@#$%^&*)."` | `PasswordRequiresNonAlphanumeric` | Sem caracteres especiais |
| `"A senha deve conter pelo menos um número (0-9)."` | `PasswordRequiresDigit` | Sem números |
| `"A senha deve conter pelo menos uma letra maiúscula (A-Z)."` | `PasswordRequiresUpper` | Sem letras maiúsculas |
| `"A senha deve conter pelo menos uma letra minúscula (a-z)."` | `PasswordRequiresLower` | Sem letras minúsculas |
| `"O formato do e-mail é inválido. Por favor, digite um e-mail válido."` | `InvalidEmail` | Formato de e-mail inválido |

**Exemplo de Erro:**
```json
{
  "message": "Já existe uma conta com esse e-mail. Tente fazer login ou use outro e-mail.",
  "errorCode": "EMAIL_ALREADY_EXISTS"
}
```

---

### **POST /api/v2/auth/login** - Login

#### ✅ **Sucesso (200)**
```json
{
  "token": "eyJhbGc...",
  "email": "usuario@example.com",
  "firstName": "João",
  "lastName": "Silva",
  "expiresAt": "2025-11-02T15:30:00Z"
}
```

#### ❌ **Erros (401 Unauthorized)**

| Mensagem | ErrorCode | Quando Ocorre |
|----------|-----------|---------------|
| `"E-mail ou senha incorretos. Verifique seus dados e tente novamente."` | `INVALID_CREDENTIALS` | E-mail não existe OU senha incorreta |

> **🔒 Nota de Segurança:** Por questões de segurança, a API **não** revela se o e-mail existe ou não. Sempre retorna a mesma mensagem genérica.

**Exemplo de Erro:**
```json
{
  "message": "E-mail ou senha incorretos. Verifique seus dados e tente novamente.",
  "errorCode": "INVALID_CREDENTIALS"
}
```

---

## 👤 Perfil do Usuário (`/api/v2/profile`)

### **PUT /api/v2/profile/password** - Alterar Senha

#### ✅ **Sucesso (200)**
```json
{
  "message": "Senha alterada com sucesso"
}
```

#### ❌ **Erros (400 Bad Request)**

| Mensagem | ErrorCode | Quando Ocorre |
|----------|-----------|---------------|
| `"A nova senha e a confirmação não conferem. Por favor, digite a mesma senha nos dois campos."` | `PASSWORD_MISMATCH` | `newPassword` ≠ `confirmPassword` |
| `"A senha atual está incorreta. Verifique e tente novamente."` | `PasswordMismatch` | Senha atual digitada errada |
| `"A nova senha deve ter no mínimo 6 caracteres."` | `PasswordTooShort` | Nova senha muito curta |
| `"A nova senha deve conter pelo menos um caractere especial (!@#$%^&*)."` | `PasswordRequiresNonAlphanumeric` | Sem caracteres especiais |
| `"A nova senha deve conter pelo menos um número (0-9)."` | `PasswordRequiresDigit` | Sem números |
| `"A nova senha deve conter pelo menos uma letra maiúscula (A-Z)."` | `PasswordRequiresUpper` | Sem maiúsculas |
| `"A nova senha deve conter pelo menos uma letra minúscula (a-z)."` | `PasswordRequiresLower` | Sem minúsculas |

**Exemplo de Erro:**
```json
{
  "message": "A senha atual está incorreta. Verifique e tente novamente.",
  "errorCode": "PasswordMismatch"
}
```

---

### **POST /api/v2/profile/photo** - Upload de Foto

#### ✅ **Sucesso (200)**
```json
{
  "message": "Foto enviada e atualizada com sucesso",
  "photoUrl": "https://res.cloudinary.com/..."
}
```

#### ❌ **Erros (400 Bad Request)**

| Mensagem | ErrorCode | Quando Ocorre |
|----------|-----------|---------------|
| `"Arquivo não fornecido"` | N/A | Nenhum arquivo enviado |
| `"Arquivo muito grande. Tamanho máximo: 5 MB"` | N/A | Arquivo > 5 MB |
| `"Tipo de arquivo inválido. Use: JPG, PNG ou WebP"` | N/A | Content-Type inválido |
| `"Extensão de arquivo inválida. Use: .jpg, .jpeg, .png ou .webp"` | N/A | Extensão não permitida |
| `"Arquivo não é uma imagem válida"` | N/A | Magic bytes inválidos |
| `"Imagem rejeitada: conteúdo impróprio detectado. Por favor, escolha outra imagem."` | N/A | **AWS Rekognition** detectou conteúdo impróprio |
| `"Falha ao salvar URL da foto"` | N/A | Erro ao salvar no banco |

**Conteúdo Bloqueado pela Moderação:**
- Nudez e conteúdo sexual explícito
- Violência e sangue
- Conteúdo sugestivo
- Drogas e armas
- Conteúdo ofensivo

---

## 📊 Categorias (`/api/v2/categories`)

### **POST /api/v2/categories** - Criar Categoria

#### ❌ **Erros (400 Bad Request)**

| Mensagem | Quando Ocorre |
|----------|---------------|
| `"Já existe uma categoria com esse nome."` | Nome duplicado para o usuário |

---

### **PUT /api/v2/categories/{id}** - Atualizar Categoria

#### ❌ **Erros (400 Bad Request)**

| Mensagem | Quando Ocorre |
|----------|---------------|
| `"Categoria não encontrada, é uma categoria padrão, ou o nome já existe."` | Categoria não existe, é padrão (não editável) ou nome duplicado |

---

### **DELETE /api/v2/categories/{id}** - Deletar Categoria

#### ❌ **Erros (400 Bad Request)**

| Mensagem | Quando Ocorre |
|----------|---------------|
| `"Não é possível deletar: categoria padrão, não encontrada ou possui transações associadas."` | Categoria padrão OU tem transações vinculadas |

---

## 💸 Transações (`/api/v2/transactions`)

### **POST /api/v2/transactions** - Criar Transação

#### ❌ **Erros (400 Bad Request)**

| Mensagem | Quando Ocorre |
|----------|---------------|
| `"Categoria inválida ou não pertence ao usuário."` | ID de categoria inválido ou pertence a outro usuário |

---

### **PUT /api/v2/transactions/{id}** - Atualizar Transação

#### ❌ **Erros (404 Not Found)**

| Mensagem | Quando Ocorre |
|----------|---------------|
| `"Transação não encontrada ou categoria inválida."` | ID de transação inválido OU categoria inválida |

---

### **DELETE /api/v2/transactions/{id}** - Deletar Transação

#### ❌ **Erros (404 Not Found)**

| Mensagem | Quando Ocorre |
|----------|---------------|
| `"Transação não encontrada."` | ID de transação inválido ou pertence a outro usuário |

---

## 🎯 Metas (`/api/v2/goals`)

### **POST /api/v2/goals** - Criar Meta

#### ❌ **Erros (400 Bad Request)**

| Mensagem | Quando Ocorre |
|----------|---------------|
| `"Categoria inválida ou já existe meta para essa categoria neste mês/ano."` | Categoria inválida OU já existe meta para categoria no mês |

---

### **PUT /api/v2/goals/{id}** - Atualizar Meta

#### ❌ **Erros (404 Not Found)**

| Mensagem | Quando Ocorre |
|----------|---------------|
| `"Meta não encontrada."` | ID de meta inválido ou pertence a outro usuário |

---

### **DELETE /api/v2/goals/{id}** - Deletar Meta

#### ❌ **Erros (404 Not Found)**

| Mensagem | Quando Ocorre |
|----------|---------------|
| `"Meta não encontrada."` | ID de meta inválido ou pertence a outro usuário |

---

## 💰 Orçamentos (`/api/v2/budgets`)

### **POST /api/budgets** - Criar Orçamento

#### ❌ **Erros (400 Bad Request)**

| Mensagem | Quando Ocorre |
|----------|---------------|
| `"Não foi possível criar o orçamento. Verifique se a categoria existe e se já não existe orçamento para esta categoria neste período."` | Categoria inválida OU orçamento duplicado |

---

### **PUT /api/budgets/{id}** - Atualizar Orçamento

#### ❌ **Erros (404 Not Found)**

| Mensagem | Quando Ocorre |
|----------|---------------|
| `"Orçamento não encontrado"` | ID de orçamento inválido ou pertence a outro usuário |

---

### **DELETE /api/budgets/{id}** - Deletar Orçamento

#### ❌ **Erros (404 Not Found)**

| Mensagem | Quando Ocorre |
|----------|---------------|
| `"Orçamento não encontrado"` | ID de orçamento inválido ou pertence a outro usuário |

---

## 🔄 Transações Recorrentes (`/api/recurringtransactions`)

### **POST /api/recurringtransactions** - Criar Transação Recorrente

#### ❌ **Erros (400 Bad Request)**

| Mensagem | Quando Ocorre |
|----------|---------------|
| `"Categoria inválida ou dados incompletos para a frequência selecionada."` | Categoria inválida OU campos obrigatórios faltando |

---

### **PUT /api/recurringtransactions/{id}** - Atualizar Transação Recorrente

#### ❌ **Erros (404 Not Found)**

| Mensagem | Quando Ocorre |
|----------|---------------|
| `"Transação recorrente não encontrada."` | ID inválido ou pertence a outro usuário |

---

### **DELETE /api/recurringtransactions/{id}** - Deletar Transação Recorrente

#### ❌ **Erros (404 Not Found)**

| Mensagem | Quando Ocorre |
|----------|---------------|
| `"Transação recorrente não encontrada."` | ID inválido ou pertence a outro usuário |

---

## 🛡️ Códigos de Erro Comuns

| HTTP Status | Significado | Quando Ocorre |
|-------------|-------------|---------------|
| `200 OK` | ✅ Sucesso | Operação realizada com sucesso |
| `400 Bad Request` | ❌ Dados inválidos | Validação falhou, dados incompletos ou regra de negócio violada |
| `401 Unauthorized` | 🔒 Não autenticado | Token ausente, inválido ou expirado |
| `403 Forbidden` | 🚫 Sem permissão | Tentativa de acessar recurso de outro usuário |
| `404 Not Found` | 🔍 Não encontrado | Recurso não existe ou não pertence ao usuário |
| `500 Internal Server Error` | 💥 Erro do servidor | Erro inesperado (contate o suporte) |

---

## 📝 Notas para Desenvolvedores Frontend

### **Estrutura Padrão de Erro:**
```json
{
  "message": "Mensagem amigável para o usuário",
  "errorCode": "CODIGO_ERRO_OPCIONAL"
}
```

### **Tratamento Recomendado:**

```javascript
try {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  });

  if (!response.ok) {
    const error = await response.json();
    
    // Mostrar mensagem específica
    showError(error.message);
    
    // Opcional: log do errorCode para debug
    console.log('ErrorCode:', error.errorCode);
    
    return;
  }

  const data = await response.json();
  // Sucesso!
  
} catch (err) {
  // Erro de rede ou servidor
  showError('Erro ao conectar ao servidor. Tente novamente.');
}
```

### **Validação de Senhas (Cliente):**

Para melhor UX, valide antes de enviar:

```javascript
function validarSenha(senha) {
  const requisitos = {
    minLength: senha.length >= 6,
    temNumero: /\d/.test(senha),
    temMaiuscula: /[A-Z]/.test(senha),
    temMinuscula: /[a-z]/.test(senha),
    temEspecial: /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(senha)
  };
  
  return requisitos;
}
```

---

## 🎨 Exemplos de Uso

### **Exemplo 1: Cadastro com Senha Fraca**

**Request:**
```http
POST /api/auth/register
Content-Type: application/json

{
  "firstName": "João",
  "lastName": "Silva",
  "email": "joao@example.com",
  "password": "123",
  "confirmPassword": "123"
}
```

**Response (400):**
```json
{
  "message": "A senha deve ter no mínimo 6 caracteres.",
  "errorCode": "PasswordTooShort"
}
```

---

### **Exemplo 2: Login com Credenciais Inválidas**

**Request:**
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "naoexiste@example.com",
  "password": "SenhaErrada123"
}
```

**Response (401):**
```json
{
  "message": "E-mail ou senha incorretos. Verifique seus dados e tente novamente.",
  "errorCode": "INVALID_CREDENTIALS"
}
```

---

### **Exemplo 3: Upload de Imagem Rejeitada**

**Request:**
```http
POST /api/profile/photo
Authorization: Bearer eyJhbGc...
Content-Type: multipart/form-data

[arquivo com conteúdo impróprio]
```

**Response (400):**
```json
{
  "message": "Imagem rejeitada: conteúdo impróprio detectado. Por favor, escolha outra imagem."
}
```

---

## 📞 Suporte

Se você encontrar alguma mensagem de erro não documentada aqui, por favor reporte para a equipe de desenvolvimento.

**Email:** dev.higorxyz@gmail.com  
**GitHub Issues:** https://github.com/higorxyz/cleverbudget/issues

---

**Última atualização:** 02 de Novembro de 2025  
**Versão da API:** 1.0
