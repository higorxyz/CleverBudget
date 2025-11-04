# ⚙️ Variáveis de Ambiente - CleverBudget

## Visão geral

A API lê configurações a partir de `appsettings.json`, variáveis de ambiente (`EnvironmentVariables`) e, em desenvolvimento, do arquivo `.env` carregado via [DotNetEnv](https://github.com/tonerdo/dotnet-env). Valores definidos nas variáveis de ambiente sempre têm prioridade.

## Como configurar

### Desenvolvimento local

1. Crie um arquivo `.env` na raiz do repositório (mesmo nível do `.sln`).
2. Use a conexão SQLite padrão ou aponte para outro arquivo/local.
3. Defina apenas os valores necessários; a maioria das integrações é opcional.

Exemplo de `.env`:
```bash
# Banco (SQLite)
ConnectionStrings__DefaultConnection=Data Source=cleverbudget.db

# JWT
JwtSettings__SecretKey=development-key-with-at-least-32-characters
JwtSettings__Issuer=CleverBudget
JwtSettings__Audience=CleverBudgetUsers

# Serviços opcionais
# Cloudinary__CloudName=...
# Cloudinary__ApiKey=...
# Cloudinary__ApiSecret=...
# Brevo__ApiKey=...
# Brevo__FromEmail=...
# Brevo__FromName=...

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Development
```

Se `JwtSettings__SecretKey` não for definido em desenvolvimento, a aplicação usa uma chave temporária logada como aviso. **Não use em produção.**

### Produção (Railway / Docker / Azure / etc.)

- Configure as variáveis diretamente no provedor (Railway, Azure App Service, contêiner, etc.).
- Para PostgreSQL no Railway, defina `DATABASE_URL` ou o conjunto `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD`.
- Opcionalmente, sobrescreva `ConnectionStrings__DefaultConnection` se preferir passar a connection string completa.

Exemplo mínimo:
```bash
ASPNETCORE_ENVIRONMENT=Production
JwtSettings__SecretKey=<chave forte com 32+ caracteres>
JwtSettings__Issuer=CleverBudget
JwtSettings__Audience=CleverBudgetUsers
PORT=8080                   # Railway define automaticamente
DATABASE_URL=postgresql://user:pass@host:5432/cleverbudget

# Serviços opcionais
Brevo__ApiKey=xkeysib-...
Brevo__FromEmail=noreply@cleverbudget.com
Brevo__FromName=CleverBudget
Cloudinary__CloudName=...
Cloudinary__ApiKey=...
Cloudinary__ApiSecret=...
DATAPROTECTION_KEYS_PATH=/app/DataProtection-Keys
```

Em produção, o `Program.cs` também aceita `ConnectionStrings__DefaultConnection` (útil para SQL Server) e as variantes em caixa alta usadas por alguns provedores (`CLOUDINARY_CLOUD_NAME`, `BREVO__APIKEY`, etc.).

## Variáveis suportadas

### Banco de dados

| Variável | Obrigatório | Default | Observações |
|----------|-------------|---------|-------------|
| `ConnectionStrings__DefaultConnection` | ✅ em dev | `Data Source=cleverbudget.db` | Usada para SQLite no desenvolvimento. Você pode apontar para um arquivo absoluto, ex: `Data Source=c:\\data\\cleverbudget.db`. |
| `DATABASE_URL` | ✅ em produção (PostgreSQL) | — | Parseado automaticamente quando começa com `postgresql://`. |
| `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD` | Alternativa ao `DATABASE_URL` | — | Usados quando `DATABASE_URL` não estiver presente. |

> A aplicação falha em `Production` se nenhum conjunto PostgreSQL válido for encontrado.

### JWT / autenticação

| Variável | Obrigatório | Default | Comentário |
|----------|-------------|---------|------------|
| `JwtSettings__SecretKey` | ✅ | (vazia) | Sem esse valor, a API cria uma chave temporária somente para desenvolvimento. |
| `JwtSettings__Issuer` | ✅ | `CleverBudget` | Ajuste se precisar validar tokens de múltiplas origens. |
| `JwtSettings__Audience` | ✅ | `CleverBudgetUsers` | Deve coincidir com o público consumidor dos tokens. |
| `JwtSettings__ExpirationMinutes` | ❌ | `60` | Opcional. Use um número inteiro. |

### Cloudinary (upload de fotos de perfil)

| Variável | Obrigatório | Default | Comentário |
|----------|-------------|---------|------------|
| `Cloudinary__CloudName` | ❌ | — | Caso não seja informado, o upload é desabilitado e a API loga aviso. |
| `Cloudinary__ApiKey` | ❌ | — | |
| `Cloudinary__ApiSecret` | ❌ | — | |
| `CLOUDINARY_CLOUD_NAME`, `CLOUDINARY_API_KEY`, `CLOUDINARY_API_SECRET` | ❌ | — | Alternativas aceitas (maiúsculo, legado). |

### Brevo (e-mails transactivos)

| Variável | Obrigatório | Default | Comentário |
|----------|-------------|---------|------------|
| `Brevo__ApiKey` | ❌ | — | Sem esse valor, os e-mails são simplesmente ignorados e um aviso é registrado. |
| `Brevo__FromEmail` | ❌ | `noreply@cleverbudget.com` | Configure para um remetente verificado. |
| `Brevo__FromName` | ❌ | `CleverBudget` | |
| `BREVO__APIKEY`, `BREVO__FROMEMAIL`, `BREVO__FROMNAME` | ❌ | — | Variantes aceitas. |

### Backups automáticos

| Variável | Obrigatório | Default (`appsettings`) | Comentário |
|----------|-------------|-------------------------|------------|
| `BackupSettings__EnableAutomaticBackups` | ❌ | `false` em dev / `true` em produção | Liga ou desliga o agendador hospedado. |
| `BackupSettings__RootPath` | ❌ | `Backups` (relativo) / `/app/Backups` (produção) | Diretório onde os arquivos `.json.gz` serão salvos. Pode ser absoluto. |
| `BackupSettings__RetentionDays` | ❌ | `7` dev / `14` prod | Remove arquivos mais antigos que `n` dias toda vez que um backup novo é salvo. |
| `BackupSettings__Interval` | ❌ | `1.00:00:00` | Intervalo entre execuções automáticas (`d.hh:mm:ss`). |
| `BackupSettings__RunOnStartup` | ❌ | `true` | Executa backup assim que o serviço inicia, útil para garantir snapshot inicial. |

### ASP.NET Core e infraestrutura

| Variável | Obrigatório | Default | Comentário |
|----------|-------------|---------|------------|
| `ASPNETCORE_ENVIRONMENT` | ❌ | `Production` | Use `Development` localmente para carregar `.env` e habilitar HTTPS/dev configs. |
| `PORT` | ❌ | 5000/5001 | Railway e outros PaaS definem automaticamente; `Program.cs` reconfigura o host para escutar nessa porta. |
| `ASPNETCORE_URLS` | ❌ | `http://localhost:5000;https://localhost:5001` | Útil para contêineres fora do Railway. |
| `DATAPROTECTION_KEYS_PATH` | ❌ | `DataProtection-Keys` | Diretório onde as chaves de Data Protection serão persistidas. |

## Boas práticas

- Use [dotnet user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) se preferir evitar `.env` durante o desenvolvimento.
- Mantenha `.env`, `*.Secrets.json` e outros arquivos sensíveis fora do Git (já presente no `.gitignore`).
- Gire chaves periodicamente e aplique princípios de menor privilégio (ex.: usuário do banco com permissões limitadas).
- Em produção, prefira gerenciadores de segredos do provedor (Azure Key Vault, AWS Parameter Store, Railway Variables, etc.).

## Referências úteis

- `CleverBudget.Api/Program.cs` — onde as variáveis são lidas e aplicadas.
- `CleverBudget.Api/appsettings.json` — valores padrão para desenvolvimento.
- `CleverBudget.Infrastructure/Services/*` — serviços que dependem das integrações opcionais.
