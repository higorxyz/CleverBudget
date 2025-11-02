# 🧪 Guia de Testes - CleverBudget

## 📋 Visão Geral

A solução conta com testes automatizados para os principais serviços, controllers e validadores. O objetivo desta página é descrever como executar e evoluir essa suíte.

### Stack utilizada

- **xUnit** – framework base
- **Moq** – mocks para dependências (`UserManager`, `IEmailService`, etc.)
- **Microsoft.EntityFrameworkCore.InMemory** – apoio a cenários com `AppDbContext`
- **Coverlet / ReportGenerator** (opcional) – geração de relatórios de cobertura

## 🏃 Como executar

Executa toda a suíte de testes:

```bash
dotnet test
```

Saída verbosa (útil para diagnosticar falhas):

```bash
dotnet test --logger "console;verbosity=detailed"
```

Filtrando por classe, método ou namespace:

```bash
# Apenas serviços
dotnet test --filter "FullyQualifiedName~CleverBudget.Tests.Services"

# Classe específica
dotnet test --filter "ClassName=TransactionServiceTests"

# Método específico
dotnet test --filter "FullyQualifiedName~CreateAsync_ValidTransaction"
```

### Cobertura de código (opcional)

```bash
dotnet test --collect:"XPlat Code Coverage"

# Relatório HTML (executar após o comando acima)
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

## 📁 Estrutura atual

```
CleverBudget.Tests/
├── Controllers/
│   ├── AuthControllerTests.cs
│   ├── BudgetsControllerTests.cs
│   ├── CategoriesControllerTests.cs
│   ├── ExportControllerTests.cs
│   ├── GoalsControllerTests.cs
│   ├── ProfileControllerTests.cs
│   ├── RecurringTransactionsControllerTests.cs
│   ├── ReportsControllerTests.cs
│   └── TransactionsControllerTests.cs
├── Services/
│   ├── AuthServiceTests.cs
│   ├── BudgetServiceTests.cs
│   ├── CategoryServiceTests.cs
│   ├── EmailServiceTests.cs
│   ├── ExportServiceTests.cs
│   ├── GoalServiceTests.cs
│   ├── RecurringTransactionServiceTests.cs
│   ├── ReportServiceTests.cs
│   ├── TransactionServiceTests.cs
│   └── UserProfileServiceTests.cs
└── Validators/
    ├── CreateBudgetDtoValidatorTests.cs
    ├── CreateCategoryDtoValidatorTests.cs
    ├── CreateTransactionDtoValidatorTests.cs
    ├── RegisterDtoValidatorTests.cs
    └── UserProfileDtoValidatorTests.cs
```

## ✍️ Escrevendo novos testes

### Padrão AAA

```csharp
[Fact]
public async Task UpdateAsync_WhenCategoryDoesNotBelongToUser_ReturnsNull()
{
    // Arrange
    using var context = TestDbContextFactory.Create();
    var service = new TransactionService(context);

    var dto = new UpdateTransactionDto { CategoryId = 99 };

    // Act
    var result = await service.UpdateAsync(1, dto, "user-id");

    // Assert
    Assert.Null(result);
}
```

### Trabalhando com `AppDbContext`

```csharp
public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
```

Lembre-se de chamar `EnsureDeleted()` ao final do teste (ou implementar `IDisposable`) para evitar vazamento de dados entre execuções.

### Mockando `UserManager<User>`

```csharp
private static Mock<UserManager<User>> BuildUserManager()
{
    var store = new Mock<IUserStore<User>>();
    return new Mock<UserManager<User>>(store.Object,
        null, null, null, null, null, null, null, null);
}
```

Chaves usadas em produção (como `JwtSettings:SecretKey`) podem ser configuradas nos testes via `ConfigurationBuilder` ou `IConfiguration` mockado conforme a necessidade.

## 📊 Cobertura e metas

Ainda não há meta formal, mas recomenda-se priorizar:

1. **Serviços críticos** (autenticação, transações, orçamentos, metas).
2. **Controllers que expõem lógica condicional** (tratamento de erros, respostas específicas).
3. **Validadores** que encapsulam regras de negócio.

Quando novos módulos forem adicionados, inclua testes em paralelo sempre que possível.

## ✅ Boas práticas

- Nomeie métodos no formato `Metodo_Cenario_ResultadoEsperado`.
- Evite dependência entre testes; cada um precisa montar e limpar seus próprios dados.
- Prefira dados explícitos em vez de mocks excessivos; use InMemory para cenários EF Core.
- Utilize `Theory` + `InlineData` para cobrir múltiplos casos simples.
- Se um teste depende de chrono, congele o tempo com helpers ou encapsule `DateTime.UtcNow`.

## ⚙️ Dicas de debug

- **Visual Studio / VS Code**: execute o teste em modo debug e use breakpoints.
- **CLI**: `dotnet test --filter "FullyQualifiedName~NomeDoTeste" --logger "console;verbosity=detailed"`.
- Use `Assert.Record` ou `try/catch` para capturar exceções e validar mensagens específicas.

## 📚 Recursos úteis

- [xUnit](https://xunit.net/)
- [Moq](https://github.com/moq/moq4)
- [Testes com EF Core](https://learn.microsoft.com/ef/core/testing/)

## Próximos passos sugeridos

- Expandir testes de background services.
- Medir cobertura periodicamente (Coverlet) e definir metas graduais.
- Integrar os testes ao pipeline CI/CD para execução automática.

---

**Veja também:** [Arquitetura](./ARCHITECTURE.md) • [Contribuindo](./CONTRIBUTING.md)
