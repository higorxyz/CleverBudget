# ⚙️ Variáveis de Ambiente - CleverBudget

## 📋 Visão Geral

Este documento lista todas as variáveis de ambiente necessárias para executar o CleverBudget em diferentes ambientes.

## 🔧 Configuração por Ambiente

### Development (Local)

Use `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CleverBudget;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "development-key-minimum-32-characters-for-testing",
    "Issuer": "CleverBudgetAPI",
    "Audience": "CleverBudgetClient",
    "ExpiryInMinutes": 60
  },
  "Cloudinary": {
    "CloudName": "seu_cloud_name",
    "ApiKey": "sua_api_key",
    "ApiSecret": "seu_api_secret"
  },
  "Brevo": {
    "ApiKey": "xkeysib-sua-api-key-do-brevo",
    "FromEmail": "noreply@cleverbudget.com",
    "FromName": "CleverBudget"
  }
}
```

⚠️ **Nunca commite este arquivo com dados reais!**

### Production (Railway/Azure/AWS)

Use variáveis de ambiente:

```bash
# Database
ConnectionStrings__DefaultConnection=Server=production-server.database.windows.net;Database=CleverBudget;User Id=admin;Password=SuperSecurePassword123!;TrustServerCertificate=True;

# JWT Authentication
Jwt__Key=production-super-secret-key-minimum-32-characters-never-share-this
Jwt__Issuer=CleverBudgetAPI
Jwt__Audience=CleverBudgetClient
Jwt__ExpiryInMinutes=60

# Cloudinary (Image Upload)
Cloudinary__CloudName=production-cloud-name
Cloudinary__ApiKey=123456789012345
Cloudinary__ApiSecret=super-secret-api-secret-here

# Brevo Email Service
Brevo__ApiKey=xkeysib-production-key-change-this
Brevo__FromEmail=noreply@cleverbudget.com
Brevo__FromName=CleverBudget

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080

# Data Protection (opcional, recomendado para múltiplas instâncias)
DataProtection__ApplicationName=CleverBudget
DataProtection__PersistKeysToFileSystem=/app/keys
```

## 📚 Referência Completa

### 1. Database (Obrigatório)

#### ConnectionStrings__DefaultConnection

**Descrição:** Connection string do banco de dados SQL Server.

**Formato:**
```
Server=<servidor>;Database=<banco>;User Id=<usuario>;Password=<senha>;TrustServerCertificate=True;
```

**Exemplos:**

```bash
# SQL Server LocalDB (Development)
ConnectionStrings__DefaultConnection="Server=(localdb)\\mssqllocaldb;Database=CleverBudget;Trusted_Connection=True;TrustServerCertificate=True;"

# Azure SQL Database
ConnectionStrings__DefaultConnection="Server=tcp:myserver.database.windows.net,1433;Database=CleverBudget;User ID=admin@myserver;Password=MyP@ssw0rd;Encrypt=True;TrustServerCertificate=False;"

# PostgreSQL (alternativa)
ConnectionStrings__DefaultConnection="Host=localhost;Database=cleverbudget;Username=postgres;Password=postgres"
```

**Obrigatório:** ✅ Sim  
**Padrão:** Nenhum

---

### 2. JWT Authentication (Obrigatório)

#### Jwt__Key

**Descrição:** Chave secreta para assinar tokens JWT.

**Requisitos:**
- Mínimo 32 caracteres
- Aleatória e única
- Nunca compartilhar

**Geração:**
```bash
# PowerShell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | % {[char]$_})

# Online
https://randomkeygen.com/ (CodeIgniter Encryption Keys)
```

**Exemplo:**
```bash
Jwt__Key="8f3c9e7a2b1d5f6e4a8c3b2d9f7e6a5c1b4d8f3e9c7a2b5d6f8e1a3c4b7d9f2e5a6c"
```

**Obrigatório:** ✅ Sim  
**Padrão:** Nenhum

---

#### Jwt__Issuer

**Descrição:** Identificador de quem emitiu o token.

**Exemplo:**
```bash
Jwt__Issuer="CleverBudgetAPI"
```

**Obrigatório:** ✅ Sim  
**Padrão:** `CleverBudgetAPI`

---

#### Jwt__Audience

**Descrição:** Identificador de quem pode usar o token.

**Exemplo:**
```bash
Jwt__Audience="CleverBudgetClient"
```

**Obrigatório:** ✅ Sim  
**Padrão:** `CleverBudgetClient`

---

#### Jwt__ExpiryInMinutes

**Descrição:** Tempo de validade do token em minutos.

**Recomendado:** 60 minutos (1 hora)

**Exemplo:**
```bash
Jwt__ExpiryInMinutes=60
```

**Obrigatório:** ✅ Sim  
**Padrão:** `60`

---

### 3. Cloudinary (Opcional)

Necessário apenas se usar upload de imagens.

#### Cloudinary__CloudName

**Descrição:** Nome da conta Cloudinary.

**Como obter:** [Cloudinary Dashboard](https://cloudinary.com/console)

**Exemplo:**
```bash
Cloudinary__CloudName="minha-conta-cloudinary"
```

**Obrigatório:** ❌ Não (mas necessário para upload de imagens)

---

#### Cloudinary__ApiKey

**Descrição:** API Key da conta Cloudinary.

**Exemplo:**
```bash
Cloudinary__ApiKey="123456789012345"
```

**Obrigatório:** ❌ Não

---

#### Cloudinary__ApiSecret

**Descrição:** API Secret da conta Cloudinary.

**Exemplo:**
```bash
Cloudinary__ApiSecret="AbCdEfGhIjKlMnOpQrStUvWxYz"
```

**Obrigatório:** ❌ Não

---

### 4. Brevo Email Service (Opcional)

Necessário apenas se usar envio de e-mails. O CleverBudget utiliza **Brevo** (anteriormente Sendinblue) para envio transacional de e-mails.

#### Brevo__ApiKey

**Descrição:** API Key do Brevo para envio de e-mails transacionais.

**Como obter:**
1. Acesse [Brevo](https://www.brevo.com/) e crie uma conta gratuita
2. Vá em **SMTP & API** → **API Keys**
3. Clique em **Generate a new API key**
4. Copie a chave gerada

**Exemplo:**
```bash
Brevo__ApiKey="xkeysib-1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef-XyZ123AbC"
```

**Obrigatório:** ❌ Não (mas necessário para notificações por e-mail)

**Plano Gratuito:**
- 300 e-mails/dia
- Sem limite de contatos
- Templates transacionais
- API completa

---

#### Brevo__FromEmail

**Descrição:** E-mail remetente configurado no Brevo.

**Importante:** Este e-mail deve estar verificado na sua conta Brevo.

**Como verificar:**
1. Acesse Brevo → **Senders**
2. Adicione e verifique seu domínio/e-mail
3. Use o e-mail verificado aqui

**Exemplo:**
```bash
Brevo__FromEmail="noreply@cleverbudget.com"
```

**Obrigatório:** ❌ Não (mas recomendado para personalização)  
**Padrão:** Se não informado, usa e-mail padrão da conta Brevo

---

#### Brevo__FromName

**Descrição:** Nome do remetente que aparece nos e-mails.

**Exemplo:**
```bash
Brevo__FromName="CleverBudget"
```

**Obrigatório:** ❌ Não (mas recomendado para melhor experiência)  
**Padrão:** Se não informado, usa nome padrão da conta Brevo

---

### 5. ASP.NET Core (Sistema)

#### ASPNETCORE_ENVIRONMENT

**Descrição:** Ambiente de execução.

**Valores:**
- `Development` - Desenvolvimento local
- `Staging` - Ambiente de testes
- `Production` - Produção

**Exemplo:**
```bash
ASPNETCORE_ENVIRONMENT=Production
```

**Obrigatório:** ❌ Não  
**Padrão:** `Production` (se não especificado)

---

#### ASPNETCORE_URLS

**Descrição:** URLs que a aplicação escuta.

**Exemplos:**
```bash
# HTTP apenas (desenvolvimento)
ASPNETCORE_URLS="http://localhost:5000"

# HTTPS (produção)
ASPNETCORE_URLS="https://localhost:5001;http://localhost:5000"

# Docker/Railway
ASPNETCORE_URLS="http://0.0.0.0:8080"
```

**Obrigatório:** ❌ Não  
**Padrão:** `http://localhost:5000;https://localhost:5001`

---

### 6. Data Protection (Recomendado em Produção)

#### DataProtection__ApplicationName

**Descrição:** Nome da aplicação para compartilhar chaves entre instâncias.

**Exemplo:**
```bash
DataProtection__ApplicationName="CleverBudget"
```

**Obrigatório:** ❌ Não  
**Quando usar:** Múltiplas instâncias ou load balancer

---

#### DataProtection__PersistKeysToFileSystem

**Descrição:** Diretório para persistir chaves de criptografia.

**Exemplo:**
```bash
DataProtection__PersistKeysToFileSystem="/app/keys"
```

**Obrigatório:** ❌ Não  
**Quando usar:** Múltiplas instâncias

---

## 🔒 Segurança

### ✅ Boas Práticas

1. **Nunca hardcode secrets** no código
2. **Use variáveis de ambiente** em produção
3. **Adicione `.env` no `.gitignore`**
4. **Rotacione secrets** regularmente
5. **Use diferentes secrets** para dev/staging/prod
6. **Habilite 2FA** em contas de serviço

### 🚨 O Que NÃO Fazer

```csharp
// ❌ ERRADO - Hardcoded
var jwtKey = "minha-chave-super-secreta";

// ❌ ERRADO - Commited no Git
// appsettings.json com dados reais

// ❌ ERRADO - Expor em logs
Console.WriteLine($"JWT Key: {jwtKey}");

// ✅ CORRETO - Variável de ambiente
var jwtKey = configuration["Jwt:Key"];
```

## 🛠️ Ferramentas

### .NET User Secrets (Development)

```bash
# Inicializar
dotnet user-secrets init --project CleverBudget.Api

# Adicionar secret
dotnet user-secrets set "Jwt:Key" "minha-chave-local" --project CleverBudget.Api

# Listar secrets
dotnet user-secrets list --project CleverBudget.Api

# Remover secret
dotnet user-secrets remove "Jwt:Key" --project CleverBudget.Api

# Limpar todos
dotnet user-secrets clear --project CleverBudget.Api
```

### Arquivo .env (Alternativa)

```bash
# .env
ConnectionStrings__DefaultConnection=Server=localhost...
Jwt__Key=minha-chave-secreta
```

Carregar com biblioteca [DotNetEnv](https://github.com/tonerdo/dotnet-env):

```csharp
// Program.cs
DotNetEnv.Env.Load();
```

## 📋 Template Completo

### Development (.env)

```bash
# Database
ConnectionStrings__DefaultConnection="Server=(localdb)\\mssqllocaldb;Database=CleverBudget;Trusted_Connection=True;TrustServerCertificate=True;"

# JWT
Jwt__Key="development-key-minimum-32-characters-for-testing-purposes-only"
Jwt__Issuer="CleverBudgetAPI"
Jwt__Audience="CleverBudgetClient"
Jwt__ExpiryInMinutes=60

# Cloudinary (opcional)
Cloudinary__CloudName="dev-cloud"
Cloudinary__ApiKey="123456789"
Cloudinary__ApiSecret="dev-secret"

# Brevo Email (opcional)
Brevo__ApiKey="xkeysib-development-key-here"
Brevo__FromEmail="noreply@localhost"
Brevo__FromName="CleverBudget Dev"

# ASP.NET
ASPNETCORE_ENVIRONMENT=Development
```

### Production (Railway/Heroku)

```bash
# Database
ConnectionStrings__DefaultConnection="Server=prod-server.database.windows.net;Database=CleverBudget;User Id=admin;Password=CHANGE_ME;TrustServerCertificate=True;"

# JWT (MUDE ESTES VALORES!)
Jwt__Key="PRODUCTION-KEY-MUST-BE-DIFFERENT-AND-MINIMUM-32-CHARACTERS-LONG"
Jwt__Issuer="CleverBudgetAPI"
Jwt__Audience="CleverBudgetClient"
Jwt__ExpiryInMinutes=60

# Cloudinary
Cloudinary__CloudName="production-cloud"
Cloudinary__ApiKey="PROD_API_KEY"
Cloudinary__ApiSecret="PROD_API_SECRET"

# Brevo Email
Brevo__ApiKey="xkeysib-PRODUCTION-KEY-CHANGE-THIS"
Brevo__FromEmail="noreply@cleverbudget.com"
Brevo__FromName="CleverBudget"

# ASP.NET
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS="http://0.0.0.0:8080"
```

## 📚 Documentos Relacionados

- [Setup](./SETUP.md) - Configuração inicial
- [Deploy](./DEPLOYMENT.md) - Deploy em produção
- [Security](./SECURITY.md) - Práticas de segurança
