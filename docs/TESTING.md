# 🧪 Guia de Testes - CleverBudget

## 📋 Visão Geral

O CleverBudget utiliza testes unitários para garantir a qualidade e confiabilidade do código.

### Stack de Testes

- **xUnit** - Framework de testes
- **Moq** - Biblioteca de mocking
- **FluentAssertions** - Assertions expressivas (opcional)
- **Microsoft.EntityFrameworkCore.InMemory** - Banco em memória para testes

## 🏃 Executando Testes

### Todos os Testes

```bash
dotnet test
```

### Com Detalhes Verbosos

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Testes Específicos

```bash
# Por classe
dotnet test --filter "FullyQualifiedName~AuthServiceTests"

# Por método
dotnet test --filter "FullyQualifiedName~RegisterAsync_ValidData_ReturnsSuccess"

# Por namespace
dotnet test --filter "FullyQualifiedName~CleverBudget.Tests.Services"
```

### Com Cobertura de Código

```bash
# Instalar ferramenta (uma vez)
dotnet tool install --global dotnet-coverage

# Executar com cobertura
dotnet test --collect:"XPlat Code Coverage"

# Gerar relatório HTML
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

## 📁 Estrutura de Testes

```
CleverBudget.Tests/
├── Services/
│   ├── AuthServiceTests.cs              # Testes de autenticação
│   ├── TransactionServiceTests.cs       # Testes de transações
│   ├── CategoryServiceTests.cs          # Testes de categorias
│   ├── GoalServiceTests.cs              # Testes de metas
│   └── UserProfileServiceTests.cs       # Testes de perfil
├── Controllers/
│   ├── AuthControllerTests.cs           # Testes de controller de auth
│   ├── TransactionsControllerTests.cs   # Testes de controller de transactions
│   └── ProfileControllerTests.cs        # Testes de controller de perfil
└── CleverBudget.Tests.csproj
```

## ✍️ Escrevendo Testes

### Padrão AAA (Arrange-Act-Assert)

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Configurar dados e mocks
    var mockUserManager = new Mock<UserManager<User>>(...);
    var service = new AuthService(mockUserManager.Object, ...);
    var registerDto = new RegisterDto 
    { 
        Email = "test@example.com",
        Password = "Test123!",
        ConfirmPassword = "Test123!"
    };

    // Act - Executar a ação
    var result = await service.RegisterAsync(registerDto);

    // Assert - Verificar resultados
    Assert.True(result.Success);
    Assert.Equal("test@example.com", result.Data.Email);
}
```

### Exemplo Real: AuthServiceTests

```csharp
public class AuthServiceTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // Setup comum para todos os testes
        var userStoreMock = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("test-key-minimum-32-characters-long");
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _mockConfiguration.Setup(c => c["Jwt:ExpiryInMinutes"]).Returns("60");

        _authService = new AuthService(_mockUserManager.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsSuccess()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("test@example.com", result.Data.Email);
        Assert.NotEmpty(result.Data.Token);
    }

    [Fact]
    public async Task RegisterAsync_PasswordMismatch_ReturnsError()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test123!",
            ConfirmPassword = "DifferentPassword123!"
        };

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("PASSWORD_MISMATCH", result.ErrorCode);
        Assert.Contains("não conferem", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Test123!"
        };

        var user = new User { Id = "user-id", Email = "test@example.com" };

        _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, loginDto.Password))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("test@example.com", result.Data.Email);
    }
}
```

## 🎭 Mocking com Moq

### Mock de UserManager

```csharp
var userStoreMock = new Mock<IUserStore<User>>();
var mockUserManager = new Mock<UserManager<User>>(
    userStoreMock.Object,  // IUserStore
    null,                  // IOptions<IdentityOptions>
    null,                  // IPasswordHasher
    null,                  // IEnumerable<IUserValidator>
    null,                  // IEnumerable<IPasswordValidator>
    null,                  // ILookupNormalizer
    null,                  // IdentityErrorDescriber
    null,                  // IServiceProvider
    null                   // ILogger
);

// Setup de comportamento
mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
    .ReturnsAsync(IdentityResult.Success);
```

### Mock de DbContext

```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: "TestDatabase")
    .Options;

var context = new ApplicationDbContext(options);

// Seed de dados
context.Categories.Add(new Category { Id = 1, Name = "Test", UserId = "user-id" });
context.SaveChanges();
```

### Mock de IConfiguration

```csharp
var mockConfiguration = new Mock<IConfiguration>();
mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("test-key-minimum-32-characters");
mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
```

## 📊 Cobertura de Testes

### Meta de Cobertura

- **Mínimo:** 70% de cobertura de código
- **Ideal:** 85%+ de cobertura
- **Crítico:** 100% para lógica de autenticação e transações

### Áreas Prioritárias

1. **AuthService** - 100% ✅
   - Registro de usuário
   - Login
   - Geração de JWT
   - Validações de senha

2. **TransactionService** - 90%+ ✅
   - CRUD completo
   - Filtros e paginação
   - Validações de negócio

3. **CategoryService** - 85%+ ✅
   - CRUD de categorias
   - Validação de duplicatas

4. **Controllers** - 80%+ ✅
   - Respostas HTTP corretas
   - Tratamento de erros
   - Validação de inputs

### Gerando Relatório de Cobertura

```bash
# 1. Executar testes com cobertura
dotnet test --collect:"XPlat Code Coverage"

# 2. Instalar ReportGenerator (primeira vez)
dotnet tool install --global dotnet-reportgenerator-globaltool

# 3. Gerar relatório HTML
reportgenerator \
  -reports:"CleverBudget.Tests/TestResults/**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html

# 4. Abrir relatório
start coveragereport/index.html  # Windows
open coveragereport/index.html   # macOS
```

## 🔍 Tipos de Testes

### 1. Testes Unitários

Testam unidades isoladas de código (métodos, classes).

```csharp
[Fact]
public void CalculateProgressPercentage_ReturnsCorrectValue()
{
    // Arrange
    var goal = new Goal
    {
        TargetAmount = 1000,
        CurrentAmount = 250
    };

    // Act
    var progress = goal.CalculateProgressPercentage();

    // Assert
    Assert.Equal(25.0m, progress);
}
```

### 2. Testes de Integração

Testam a integração entre componentes (com banco real/in-memory).

```csharp
[Fact]
public async Task CreateTransaction_WithCategory_SavesSuccessfully()
{
    // Arrange - Usa banco in-memory
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase("TestDb")
        .Options;

    using var context = new ApplicationDbContext(options);
    var service = new TransactionService(context);

    // Act
    var transaction = new CreateTransactionDto
    {
        CategoryId = 1,
        Amount = 100,
        Description = "Test"
    };
    var result = await service.CreateAsync("user-id", transaction);

    // Assert
    Assert.NotNull(result);
    var saved = await context.Transactions.FindAsync(result.Id);
    Assert.NotNull(saved);
}
```

### 3. Testes de Controller

Testam o comportamento dos endpoints HTTP.

```csharp
[Fact]
public async Task Register_ValidData_Returns200()
{
    // Arrange
    var mockAuthService = new Mock<IAuthService>();
    mockAuthService.Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
        .ReturnsAsync(new AuthResult
        {
            Success = true,
            Data = new AuthResponseDto
            {
                Token = "test-token",
                Email = "test@example.com",
                ExpiresIn = 3600
            }
        });

    var controller = new AuthController(mockAuthService.Object);

    // Act
    var result = await controller.Register(new RegisterDto
    {
        Email = "test@example.com",
        Password = "Test123!",
        ConfirmPassword = "Test123!"
    });

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    Assert.Equal(200, okResult.StatusCode);
}
```

## ✅ Boas Práticas

### 1. Nomenclatura

```csharp
// Padrão: MethodName_Scenario_ExpectedBehavior
[Fact]
public async Task CreateTransaction_WithInvalidAmount_ThrowsException()
{
    // ...
}
```

### 2. Independência

```csharp
// ❌ ERRADO - Testes dependentes
[Fact]
public void Test1() { /* cria dados */ }

[Fact]
public void Test2() { /* usa dados do Test1 */ }

// ✅ CORRETO - Testes independentes
[Fact]
public void Test1() { /* cria e limpa próprios dados */ }

[Fact]
public void Test2() { /* cria e limpa próprios dados */ }
```

### 3. Setup e Cleanup

```csharp
public class TransactionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public TransactionServiceTests()
    {
        // Setup antes de cada teste
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        // Cleanup após cada teste
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

### 4. Theory com InlineData

```csharp
[Theory]
[InlineData("", "Test123!", false)]  // Email vazio
[InlineData("test@example.com", "", false)]  // Senha vazia
[InlineData("test@example.com", "Test123!", true)]  // Válido
public async Task Register_VariousInputs_ReturnsExpectedResult(
    string email, string password, bool shouldSucceed)
{
    // Arrange
    var dto = new RegisterDto
    {
        Email = email,
        Password = password,
        ConfirmPassword = password
    };

    // Act
    var result = await _authService.RegisterAsync(dto);

    // Assert
    Assert.Equal(shouldSucceed, result.Success);
}
```

## 🚨 Debugging de Testes

### No Visual Studio

1. Coloque breakpoints no código de teste
2. Clique com botão direito no teste
3. Selecione **Debug Test**

### No VS Code

1. Instale a extensão **.NET Core Test Explorer**
2. Coloque breakpoints
3. Clique em **Debug** ao lado do teste

### Via Linha de Comando

```bash
# Executar teste específico em modo debug
dotnet test --filter "FullyQualifiedName~RegisterAsync_ValidData" --logger "console;verbosity=detailed"
```

## 📚 Recursos Adicionais

### Documentação Oficial

- [xUnit](https://xunit.net/)
- [Moq](https://github.com/moq/moq4)
- [EF Core Testing](https://learn.microsoft.com/en-us/ef/core/testing/)

### Próximos Passos

- [ ] Adicionar testes de integração com banco real
- [ ] Implementar testes de performance
- [ ] Adicionar testes E2E com Playwright
- [ ] Configurar CI/CD para executar testes automaticamente

## 📊 Estatísticas Atuais

```
Total de Testes: 354
✅ Passando: 354 (100%)
❌ Falhando: 0
⏱️ Tempo médio: 3.5s
📈 Cobertura: ~85%
```

---

## 📚 Documentos Relacionados

- [Padrões de Código](./CODING_STANDARDS.md)
- [Arquitetura](./ARCHITECTURE.md)
- [Contribuindo](./CONTRIBUTING.md)
