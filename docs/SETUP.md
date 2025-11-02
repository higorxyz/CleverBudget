# 🚀 Guia de Configuração - CleverBudget API

## Requisitos

- **.NET SDK 9.0** ou superior ([download](https://dotnet.microsoft.com/download))
- **Git** e um editor de sua preferência
- **SQLite** já vem pelo provider do Entity Framework (nenhum servidor adicional necessário)
- Opcional: Docker para empacotar a API

> Em desenvolvimento usamos SQLite por padrão. Para usar outro banco, ajuste `ConnectionStrings__DefaultConnection`.

## 1. Clonar e restaurar dependências

```bash
git clone https://github.com/higorxyz/CleverBudget.git
cd CleverBudget
dotnet restore
```

## 2. Configurar variáveis locais

Crie um arquivo `.env` na raiz do repositório (mesmo nível do `.sln`):

```bash
ConnectionStrings__DefaultConnection=Data Source=cleverbudget.db
JwtSettings__SecretKey=development-secret-key-with-32-chars
ASPNETCORE_ENVIRONMENT=Development
```

Adicione integrações opcionais conforme necessidade (`Cloudinary__*`, `Brevo__*`). Consulte `docs/ENVIRONMENT_VARIABLES.md` para a lista completa.

## 3. Aplicar migrações (opcional)

O `Program.cs` garante `db.Database.Migrate()` na inicialização. Execute manualmente se quiser validar antes:

```bash
dotnet ef database update --project CleverBudget.Infrastructure --startup-project CleverBudget.Api
```

## 4. Rodar a API

```bash
dotnet run --project CleverBudget.Api
```

Rotas padrão:
- `http://localhost:5000`
- `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## 5. Executar testes

```bash
dotnet test
# cobertura opcional
dotnet test --collect:"XPlat Code Coverage"
```

Relatórios ficam em `CleverBudget.Tests/TestResults`.

## Docker (opcional)

```bash
docker build -t cleverbudget-api .
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e JwtSettings__SecretKey="change-me" \
  -e DATABASE_URL="postgresql://user:pass@host:5432/cleverbudget" \
  cleverbudget-api
```

## Dicas úteis

- `dotnet watch run --project CleverBudget.Api` para hot reload.
- Logs estruturados ficam em `logs/cleverbudget-*.log` (Serilog).
- Configure `Brevo__ApiKey` e `Cloudinary__*` apenas se for testar e-mails e upload de foto.

## Erros comuns

| Sintoma | Correção |
|---------|----------|
| `PostgreSQL é obrigatório em produção` | Defina `DATABASE_URL` ou o conjunto `PG*`. |
| Falha ao gerar token | Garanta `JwtSettings__SecretKey` com 32+ caracteres. |
| Upload de foto retorna 400 | Forneça credenciais Cloudinary válidas ou desative o recurso. |

## E depois?

1. Leia a [Arquitetura](./ARCHITECTURE.md) para entender as camadas.
2. Revise [Autenticação](./AUTHENTICATION.md) e [ENVIRONMENT_VARIABLES](./ENVIRONMENT_VARIABLES.md).
3. Use o [catálogo de endpoints](./ENDPOINTS.md) para testar via Swagger/Postman.

## Ajuda

Contribuições são bem-vindas. Abra issues ou PRs e consulte os demais arquivos de `/docs`.
