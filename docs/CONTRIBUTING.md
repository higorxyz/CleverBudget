# 🤝 Guia de Contribuição - CleverBudget

## 👋 Bem-vindo!

Obrigado por considerar contribuir com o CleverBudget! Este documento fornece diretrizes para contribuições.

## 🎯 Como Posso Contribuir?

### 🐛 Reportar Bugs

Encontrou um bug? Abra uma issue com:

**Template de Bug Report:**
```markdown
**Descrição do Bug**
Descrição clara e concisa do problema.

**Como Reproduzir**
1. Vá para '...'
2. Clique em '...'
3. Execute '...'
4. Veja o erro

**Comportamento Esperado**
O que deveria acontecer.

**Screenshots**
Se aplicável, adicione screenshots.

**Ambiente**
- OS: [Windows 11, macOS 14, Ubuntu 22.04]
- .NET Version: [9.0]
- Browser: [Chrome 119, Firefox 120]

**Logs**
```
Cole logs relevantes aqui
```
```

### 💡 Sugerir Funcionalidades

Tem uma ideia? Abra uma issue com:

**Template de Feature Request:**
```markdown
**Problema a Resolver**
Descrição clara do problema que a feature resolve.

**Solução Proposta**
Como você imagina que isso deveria funcionar.

**Alternativas Consideradas**
Outras soluções que você pensou.

**Contexto Adicional**
Qualquer outro contexto, screenshots, exemplos.
```

### 🔧 Contribuir com Código

1. **Fork** o repositório
2. **Clone** seu fork localmente
3. **Crie uma branch** para sua feature
4. **Faça commits** seguindo convenções
5. **Envie um Pull Request**

## 🌿 Workflow de Git

### Branches

```bash
# Feature nova
git checkout -b feature/nome-da-feature

# Correção de bug
git checkout -b fix/descricao-do-bug

# Documentação
git checkout -b docs/descricao-da-doc

# Refatoração
git checkout -b refactor/descricao
```

### Commits Convencionais

Seguimos a convenção [Conventional Commits](https://www.conventionalcommits.org/):

```bash
# Feature
git commit -m "feat: adiciona endpoint de exportação PDF"

# Bug fix
git commit -m "fix: corrige cálculo de porcentagem em metas"

# Documentação
git commit -m "docs: atualiza README com instruções de deploy"

# Refatoração
git commit -m "refactor: simplifica lógica de autenticação"

# Testes
git commit -m "test: adiciona testes para TransactionService"

# Chore (manutenção)
git commit -m "chore: atualiza dependências do projeto"

# Breaking change
git commit -m "feat!: altera schema de User (BREAKING CHANGE)"
```

**Formato:**
```
<type>[optional scope]: <description>

[optional body]

[optional footer]
```

**Types:**
- `feat` - Nova funcionalidade
- `fix` - Correção de bug
- `docs` - Documentação
- `style` - Formatação (sem mudança de código)
- `refactor` - Refatoração
- `test` - Testes
- `chore` - Manutenção
- `perf` - Performance

## 📝 Padrões de Código

### C# Coding Standards

#### Nomenclatura

```csharp
// Classes e Interfaces - PascalCase
public class TransactionService { }
public interface ITransactionService { }

// Métodos - PascalCase
public async Task<Transaction> GetByIdAsync(int id) { }

// Propriedades - PascalCase
public string Email { get; set; }

// Campos privados - _camelCase
private readonly ITransactionService _transactionService;

// Parâmetros e variáveis - camelCase
public void ProcessTransaction(Transaction transaction)
{
    var userId = GetCurrentUserId();
}

// Constantes - PascalCase
public const int MaxTransactionsPerPage = 100;

// Enums - PascalCase
public enum TransactionType
{
    Expense,  // Valores também PascalCase
    Income
}
```

#### Organização de Código

```csharp
public class TransactionService : ITransactionService
{
    // 1. Campos privados
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TransactionService> _logger;

    // 2. Construtor
    public TransactionService(
        ApplicationDbContext context,
        ILogger<TransactionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // 3. Propriedades públicas
    public int MaxPageSize { get; } = 100;

    // 4. Métodos públicos
    public async Task<Transaction> GetByIdAsync(int id)
    {
        return await _context.Transactions.FindAsync(id);
    }

    // 5. Métodos privados
    private bool ValidateTransaction(Transaction transaction)
    {
        return transaction.Amount > 0;
    }
}
```

#### Async/Await

```csharp
// ✅ CORRETO - Métodos async terminam com Async
public async Task<Transaction> GetTransactionAsync(int id)
{
    return await _context.Transactions.FindAsync(id);
}

// ❌ ERRADO - Sem sufixo Async
public async Task<Transaction> GetTransaction(int id)
{
    return await _context.Transactions.FindAsync(id);
}

// ✅ CORRETO - ConfigureAwait(false) em bibliotecas
public async Task<Transaction> GetTransactionAsync(int id)
{
    return await _context.Transactions
        .FindAsync(id)
        .ConfigureAwait(false);
}
```

#### Tratamento de Erros

```csharp
// ✅ CORRETO - Exceções específicas
public async Task<Transaction> GetByIdAsync(int id)
{
    var transaction = await _context.Transactions.FindAsync(id);
    
    if (transaction == null)
        throw new NotFoundException($"Transaction {id} not found");
    
    return transaction;
}

// ✅ CORRETO - Retornar Result
public async Task<OperationResult<Transaction>> GetByIdAsync(int id)
{
    var transaction = await _context.Transactions.FindAsync(id);
    
    if (transaction == null)
        return OperationResult<Transaction>.Failure(
            "Transaction not found", 
            "TRANSACTION_NOT_FOUND"
        );
    
    return OperationResult<Transaction>.Success(transaction);
}
```

### Validações com FluentValidation

```csharp
public class CreateTransactionDtoValidator : AbstractValidator<CreateTransactionDto>
{
    public CreateTransactionDtoValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("O valor deve ser maior que zero");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("A descrição é obrigatória")
            .MaximumLength(500)
            .WithMessage("A descrição não pode ter mais de 500 caracteres");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("A data é obrigatória")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("A data não pode ser futura");
    }
}
```

## 🧪 Testes

### Obrigatório

Toda contribuição de código **deve** incluir testes:

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var service = CreateService();
    var input = new SomeDto { /* ... */ };

    // Act
    var result = await service.SomeMethodAsync(input);

    // Assert
    Assert.True(result.Success);
    Assert.Equal(expectedValue, result.Data);
}
```

### Executar Testes

```bash
# Antes de enviar PR
dotnet test

# Com cobertura
dotnet test --collect:"XPlat Code Coverage"
```

**Critério de Aceitação:**
- ✅ Todos os testes passando
- ✅ Cobertura mínima de 70% nas linhas adicionadas

## 📋 Checklist de Pull Request

Antes de enviar seu PR, verifique:

- [ ] ✅ Código segue os padrões do projeto
- [ ] ✅ Testes adicionados/atualizados
- [ ] ✅ Todos os testes passando localmente
- [ ] ✅ Documentação atualizada (se necessário)
- [ ] ✅ Commit messages seguem convenção
- [ ] ✅ Branch atualizada com `main`
- [ ] ✅ Sem conflitos de merge
- [ ] ✅ Build passou no CI/CD

## 📤 Enviando Pull Request

1. **Faça push** da sua branch

```bash
git push origin feature/minha-feature
```

2. **Abra Pull Request** no GitHub

3. **Preencha o template:**

```markdown
## Descrição
Descrição clara das mudanças.

## Tipo de Mudança
- [ ] Bug fix (mudança que corrige um problema)
- [ ] Nova feature (mudança que adiciona funcionalidade)
- [ ] Breaking change (correção ou feature que quebra compatibilidade)
- [ ] Documentação

## Como Testar
1. Execute `dotnet run`
2. Acesse endpoint X
3. Verifique resultado Y

## Screenshots (se aplicável)
![screenshot](url)

## Checklist
- [x] Meu código segue os padrões do projeto
- [x] Realizei self-review do código
- [x] Comentei partes complexas do código
- [x] Atualizei a documentação
- [x] Minhas mudanças não geram novos warnings
- [x] Adicionei testes que provam que minha correção funciona
- [x] Testes unitários novos e existentes passam localmente
```

## 🔍 Code Review

### Para Reviewers

- ✅ Código segue padrões
- ✅ Testes cobrem mudanças
- ✅ Documentação atualizada
- ✅ Performance não foi afetada
- ✅ Segurança não foi comprometida

### Para Contributors

- ✅ Responda feedbacks educadamente
- ✅ Faça ajustes solicitados
- ✅ Marque conversas como resolvidas
- ✅ Agradeça aos reviewers

## 🎨 Estilo de Código

### EditorConfig

O projeto usa `.editorconfig`:

```ini
[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true
```

### Formatação Automática

```bash
# Formatar código
dotnet format

# Verificar formatação (CI)
dotnet format --verify-no-changes
```

## 📚 Recursos

### Documentação do Projeto

- [Setup](./SETUP.md) - Como configurar ambiente
- [Arquitetura](./ARCHITECTURE.md) - Estrutura do projeto
- [Testes](./TESTING.md) - Como escrever testes
- [Database](./DATABASE_SCHEMA.md) - Schema do banco

### Recursos Externos

- [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [xUnit](https://xunit.net/)

## 🆘 Precisa de Ajuda?

- 💬 **Discussões:** Abra uma discussion no GitHub
- 🐛 **Issues:** Para bugs e features
- 📧 **Email:** (adicionar email de contato)

## 📜 Código de Conduta

### Nossos Valores

- **Respeito:** Trate todos com respeito
- **Colaboração:** Trabalhe em equipe
- **Qualidade:** Preze por código de qualidade
- **Aprendizado:** Compartilhe conhecimento

### Comportamentos Inaceitáveis

- ❌ Linguagem ou imagens ofensivas
- ❌ Trolling ou comentários insultuosos
- ❌ Assédio público ou privado
- ❌ Publicar informações privadas sem permissão

## 📝 Licença

Ao contribuir, você concorda que suas contribuições serão licenciadas sob a mesma licença do projeto (MIT).

---

**Obrigado por contribuir! 🎉**

Sua ajuda torna o CleverBudget melhor para todos.
