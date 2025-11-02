# 🚀 Guia de Configuração - CleverBudget API

## Pré-requisitos

### Ferramentas Necessárias
- **.NET 9.0 SDK** ou superior ([Download](https://dotnet.microsoft.com/download))
- **SQL Server** (LocalDB, Express ou Developer Edition)
- **Visual Studio 2022** ou **VS Code** com extensão C#
- **Git** para controle de versão

### Conhecimentos Recomendados
- C# e .NET Core
- Entity Framework Core
- ASP.NET Core Web API
- JWT Authentication
- SQL Server

## 📥 Instalação

### 1. Clone o Repositório

```bash
git clone https://github.com/higorxyz/CleverBudget.git
cd CleverBudget
```

### 2. Restaure as Dependências

```bash
dotnet restore
```

### 3. Configure o Banco de Dados

#### Opção A: SQL Server LocalDB (Recomendado para desenvolvimento)

O projeto já está configurado para usar LocalDB por padrão.

```bash
# Aplique as migrações
dotnet ef database update --project CleverBudget.Infrastructure --startup-project CleverBudget.Api
```

#### Opção B: SQL Server Customizado

Edite `CleverBudget.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SEU_SERVIDOR;Database=CleverBudget;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 4. Configure as Variáveis de Ambiente

Crie um arquivo `appsettings.Development.json` (se não existir):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CleverBudget;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "SUA_CHAVE_SECRETA_AQUI_MINIMO_32_CARACTERES",
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

⚠️ **IMPORTANTE:** Nunca commite este arquivo com dados sensíveis!

### 5. Execute a Aplicação

```bash
dotnet run --project CleverBudget.Api
```

A API estará disponível em:
- **HTTP:** `http://localhost:5000`
- **HTTPS:** `https://localhost:5001`
- **Swagger UI:** `https://localhost:5001/swagger`

## 🧪 Executar Testes

```bash
# Todos os testes
dotnet test

# Com cobertura
dotnet test --collect:"XPlat Code Coverage"

# Testes específicos
dotnet test --filter "FullyQualifiedName~AuthService"
```

## 🐳 Docker (Opcional)

```bash
# Build da imagem
docker build -t cleverbudget-api .

# Executar container
docker run -p 5000:8080 -e ConnectionStrings__DefaultConnection="..." cleverbudget-api
```

## 🔧 Ferramentas de Desenvolvimento Recomendadas

### Extensions do VS Code
- **C# Dev Kit** - Suporte completo para C#
- **REST Client** - Testar endpoints
- **SQLTools** - Gerenciar banco de dados
- **GitLens** - Git enhanced

### Ferramentas Úteis
- **Postman** ou **Insomnia** - Testar API
- **SQL Server Management Studio (SSMS)** - Gerenciar SQL Server
- **Azure Data Studio** - Alternativa moderna ao SSMS

## 📝 Próximos Passos

1. ✅ Configuração concluída
2. 📖 Leia a [Arquitetura do Projeto](./ARCHITECTURE.md)
3. 🔐 Entenda a [Autenticação](./AUTHENTICATION.md)
4. 📡 Explore os [Endpoints](./ENDPOINTS.md)
5. 🧪 Execute os testes e veja [Guia de Testes](./TESTING.md)

## 🆘 Problemas Comuns

### Erro: "Unable to connect to database"
- Verifique se o SQL Server está rodando
- Confirme a connection string em `appsettings.Development.json`
- Tente recriar o banco: `dotnet ef database drop` e `dotnet ef database update`

### Erro: "Port 5000 already in use"
- Altere a porta em `launchSettings.json`
- Ou mate o processo: `netstat -ano | findstr :5000` e `taskkill /PID [PID] /F`

### Testes falhando
- Limpe e reconstrua: `dotnet clean && dotnet build`
- Restaure pacotes: `dotnet restore`
- Verifique se todas as migrações foram aplicadas

## 📞 Suporte

Encontrou um problema? Abra uma issue no GitHub ou consulte a documentação completa em `/docs`.
