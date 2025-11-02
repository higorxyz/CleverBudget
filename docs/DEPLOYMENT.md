# 🚢 Guia de Deploy - CleverBudget

## 🎯 Ambientes

### Development (Local)
- **URL:** `https://localhost:5001`
- **Banco:** SQL Server LocalDB
- **Configuração:** `appsettings.Development.json`

### Production (Railway)
- **URL:** `https://cleverbudget-production.up.railway.app`
- **Banco:** PostgreSQL ou SQL Server na nuvem
- **Configuração:** Variáveis de ambiente

## 🚂 Deploy no Railway

### 1️⃣ Pré-requisitos

- Conta no [Railway](https://railway.app/)
- Repositório Git (GitHub, GitLab, etc.)
- Dockerfile no projeto (já incluído)

### 2️⃣ Configuração Inicial

#### Criar Novo Projeto

1. Acesse [Railway Dashboard](https://railway.app/dashboard)
2. Clique em **New Project**
3. Selecione **Deploy from GitHub repo**
4. Escolha o repositório `CleverBudget`

#### Adicionar Banco de Dados

1. No projeto Railway, clique em **New**
2. Selecione **Database** → **PostgreSQL** ou **SQL Server**
3. Railway criará automaticamente o banco
4. Copie a connection string gerada

### 3️⃣ Variáveis de Ambiente

Adicione as seguintes variáveis no Railway:

```bash
# Database
ConnectionStrings__DefaultConnection=Server=...;Database=...;User=...;Password=...;

# JWT
Jwt__Key=sua-chave-secreta-minimo-32-caracteres-aqui
Jwt__Issuer=CleverBudgetAPI
Jwt__Audience=CleverBudgetClient
Jwt__ExpiryInMinutes=60

# Cloudinary (Upload de Imagens)
Cloudinary__CloudName=seu_cloud_name
Cloudinary__ApiKey=sua_api_key
Cloudinary__ApiSecret=seu_api_secret

# Brevo (Email Service)
Brevo__ApiKey=xkeysib-sua-api-key-do-brevo
Brevo__FromEmail=noreply@cleverbudget.com
Brevo__FromName=CleverBudget

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
```

⚠️ **IMPORTANTE:** 
- Use secrets/variáveis de ambiente, nunca hardcode
- Gere uma chave JWT forte e única para produção
- Ative 2FA nas contas de serviço (Cloudinary, Brevo)

#### Configuração do Brevo (Email Service)

O CleverBudget utiliza **Brevo** (anteriormente Sendinblue) para envio de e-mails transacionais.

**Passos para configurar:**

1. **Criar conta gratuita:**
   - Acesse [Brevo](https://www.brevo.com/)
   - Crie uma conta (300 e-mails/dia gratuitos)

2. **Obter API Key:**
   - Acesse **SMTP & API** → **API Keys**
   - Clique em **Generate a new API key**
   - Copie a chave (formato: `xkeysib-...`)
   - Configure a variável `Brevo__ApiKey`

3. **Configurar remetente:**
   - Acesse **Senders**
   - Adicione e verifique seu domínio/e-mail
   - Use o e-mail verificado em `Brevo__FromEmail`

4. **Variáveis obrigatórias:**
   ```bash
   Brevo__ApiKey=xkeysib-sua-api-key-aqui
   Brevo__FromEmail=noreply@seudominio.com  # E-mail verificado
   Brevo__FromName=CleverBudget              # Nome que aparece no e-mail
   ```

**Funcionalidades que usam e-mail:**
- ✉️ Notificações de orçamento excedido
- ✉️ Lembretes de metas financeiras
- ✉️ Alertas de transações importantes
- ✉️ Confirmação de alterações no perfil (futuro)

### 4️⃣ Configurar Dockerfile

O Dockerfile já está incluído no projeto:

```dockerfile
# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copiar arquivos de projeto
COPY CleverBudget.sln .
COPY CleverBudget.Api/*.csproj ./CleverBudget.Api/
COPY CleverBudget.Core/*.csproj ./CleverBudget.Core/
COPY CleverBudget.Infrastructure/*.csproj ./CleverBudget.Infrastructure/
COPY CleverBudget.Application/*.csproj ./CleverBudget.Application/

# Restaurar dependências
RUN dotnet restore

# Copiar código-fonte
COPY . .

# Build e publicar
WORKDIR /app/CleverBudget.Api
RUN dotnet publish -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Expor porta
EXPOSE 8080

# Iniciar aplicação
ENTRYPOINT ["dotnet", "CleverBudget.Api.dll"]
```

### 5️⃣ Deploy Automático

Railway detecta automaticamente o Dockerfile e:

1. ✅ Faz build da imagem Docker
2. ✅ Executa migrations (se configurado)
3. ✅ Inicia a aplicação
4. ✅ Gera URL pública

#### Migrations Automáticas

Adicione em `Program.cs`:

```csharp
// Aplicar migrations automaticamente em produção
if (app.Environment.IsProduction())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();  // Aplica migrations pendentes
    }
}
```

### 6️⃣ Configurar Domínio (Opcional)

1. No Railway, vá em **Settings** → **Domains**
2. Clique em **Generate Domain** (subdomínio gratuito)
3. Ou adicione domínio customizado (ex: `api.cleverbudget.com`)

### 7️⃣ Monitoramento

Railway fornece:
- **Logs em tempo real**
- **Métricas de CPU/Memória**
- **Build history**
- **Restart automático** em caso de falha

Acesse via Dashboard → Logs

## 🐳 Deploy Manual com Docker

### Build da Imagem

```bash
# Na raiz do projeto
docker build -t cleverbudget-api .
```

### Executar Localmente

```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;..." \
  -e Jwt__Key="sua-chave-aqui" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  cleverbudget-api
```

### Push para Registry

```bash
# Docker Hub
docker tag cleverbudget-api seu-usuario/cleverbudget-api:latest
docker push seu-usuario/cleverbudget-api:latest

# GitHub Container Registry
docker tag cleverbudget-api ghcr.io/seu-usuario/cleverbudget-api:latest
docker push ghcr.io/seu-usuario/cleverbudget-api:latest
```

## ☁️ Deploy em Outras Plataformas

### Azure App Service

```bash
# 1. Criar Resource Group
az group create --name CleverBudgetRG --location eastus

# 2. Criar App Service Plan
az appservice plan create --name CleverBudgetPlan --resource-group CleverBudgetRG --sku B1 --is-linux

# 3. Criar Web App
az webapp create --resource-group CleverBudgetRG --plan CleverBudgetPlan --name cleverbudget-api --runtime "DOTNET|9.0"

# 4. Configurar variáveis
az webapp config appsettings set --resource-group CleverBudgetRG --name cleverbudget-api --settings \
  Jwt__Key="sua-chave" \
  ConnectionStrings__DefaultConnection="..."

# 5. Deploy
az webapp deployment source config --name cleverbudget-api --resource-group CleverBudgetRG --repo-url https://github.com/seu-usuario/CleverBudget --branch main --manual-integration
```

### AWS Elastic Beanstalk

```bash
# 1. Instalar EB CLI
pip install awsebcli

# 2. Inicializar
eb init -p "64bit Amazon Linux 2 v2.5.0 running .NET Core" cleverbudget-api

# 3. Criar ambiente
eb create cleverbudget-prod

# 4. Configurar variáveis
eb setenv Jwt__Key="sua-chave" ConnectionStrings__DefaultConnection="..."

# 5. Deploy
eb deploy
```

### Heroku

```bash
# 1. Login
heroku login

# 2. Criar app
heroku create cleverbudget-api

# 3. Adicionar PostgreSQL
heroku addons:create heroku-postgresql:hobby-dev

# 4. Configurar buildpack
heroku buildpacks:set https://github.com/jincod/dotnetcore-buildpack

# 5. Configurar variáveis
heroku config:set Jwt__Key="sua-chave"

# 6. Deploy
git push heroku main
```

## 🔄 CI/CD com GitHub Actions

### Criar `.github/workflows/deploy.yml`

```yaml
name: Deploy to Railway

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore
      
    - name: Test
      run: dotnet test --no-build --verbosity normal
      
  deploy:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Deploy to Railway
      uses: bervProject/railway-deploy@main
      with:
        railway_token: ${{ secrets.RAILWAY_TOKEN }}
        service: cleverbudget-api
```

### Configurar Secrets no GitHub

1. Vá em **Settings** → **Secrets and variables** → **Actions**
2. Adicione:
   - `RAILWAY_TOKEN` - Token de deploy do Railway

## 📊 Monitoramento e Logs

### Application Insights (Azure)

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();
```

```bash
# Variável de ambiente
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=...
```

### Serilog para Logs Estruturados

```csharp
// Já configurado em Program.cs
Log.Information("Application starting up at {Time}", DateTime.UtcNow);
```

Logs salvos em:
- Console (Railway)
- Arquivo `logs/log-{Date}.txt`
- Application Insights (se configurado)

## 🔒 Checklist de Segurança

Antes de fazer deploy em produção:

- [ ] ✅ HTTPS habilitado (Railway fornece automaticamente)
- [ ] ✅ Chave JWT forte e única
- [ ] ✅ Variáveis de ambiente configuradas (não hardcoded)
- [ ] ✅ CORS configurado corretamente
- [ ] ✅ Rate limiting ativado (futuro)
- [ ] ✅ Migrations aplicadas
- [ ] ✅ Testes passando (354/354)
- [ ] ✅ Logs estruturados configurados
- [ ] ✅ Backup do banco configurado
- [ ] ✅ Secrets rotacionados regularmente

## 🚨 Troubleshooting

### Erro: "Database connection failed"

**Solução:**
```bash
# Verificar connection string
echo $ConnectionStrings__DefaultConnection

# Testar conexão
dotnet ef database update --project CleverBudget.Infrastructure --startup-project CleverBudget.Api
```

### Erro: "Port 8080 already in use"

**Solução:**
```bash
# Alterar porta em railway.toml
[deploy]
startCommand = "dotnet CleverBudget.Api.dll --urls http://0.0.0.0:8080"
```

### Migrations não aplicadas

**Solução:**
```bash
# Aplicar manualmente
dotnet ef database update --project CleverBudget.Infrastructure --startup-project CleverBudget.Api --connection "sua-connection-string-producao"
```

### Logs não aparecem

**Solução:**
```csharp
// Adicionar em Program.cs
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    await next();
});
```

## 📈 Escalabilidade

### Horizontal Scaling (Railway)

1. Dashboard → Settings → **Replicas**
2. Aumentar número de instâncias
3. Railway adiciona load balancer automaticamente

### Database Scaling

- **Índices:** Já otimizados (ver [DATABASE_SCHEMA.md](./DATABASE_SCHEMA.md))
- **Read Replicas:** Considerar para alta carga
- **Connection Pooling:** Já configurado no EF Core

### Caching (Futuro)

```csharp
// Redis para cache distribuído
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});
```

## 📚 Documentos Relacionados

- [Variáveis de Ambiente](./ENVIRONMENT_VARIABLES.md)
- [Database Schema](./DATABASE_SCHEMA.md)
- [Segurança](./SECURITY.md)
- [Arquitetura](./ARCHITECTURE.md)
