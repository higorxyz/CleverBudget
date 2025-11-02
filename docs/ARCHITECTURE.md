# 🏗️ Arquitetura do Projeto - CleverBudget

## 📐 Visão Geral

O CleverBudget segue a **Clean Architecture** com separação clara de responsabilidades em 4 camadas principais.

```
┌─────────────────────────────────────────┐
│         CleverBudget.Api                │  ← Camada de Apresentação
│      (Controllers, Middlewares)         │
└─────────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────────┐
│      CleverBudget.Application           │  ← Camada de Aplicação
│    (Services, Validators, DTOs)         │
└─────────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────────┐
│         CleverBudget.Core               │  ← Camada de Domínio
│   (Entities, Interfaces, Enums)         │
└─────────────────────────────────────────┘
                   ↑
┌─────────────────────────────────────────┐
│    CleverBudget.Infrastructure          │  ← Camada de Infraestrutura
│ (Data Access, External Services, Repos) │
└─────────────────────────────────────────┘
```

## 📦 Camadas do Projeto

### 1️⃣ **CleverBudget.Api** (Apresentação)

**Responsabilidade:** Expor endpoints HTTP e gerenciar requisições/respostas.

```
CleverBudget.Api/
├── Controllers/           # Endpoints da API
│   ├── AuthController.cs          # Autenticação (Register, Login)
│   ├── TransactionsController.cs  # CRUD de transações
│   ├── CategoriesController.cs    # CRUD de categorias
│   ├── GoalsController.cs         # CRUD de metas financeiras
│   ├── ReportsController.cs       # Relatórios e analytics
│   ├── RecurringTransactionsController.cs
│   └── ExportController.cs        # Exportação de dados
├── Middlewares/           # Middlewares customizados (se houver)
├── Program.cs             # Configuração da aplicação
├── appsettings.json       # Configurações gerais
└── appsettings.Development.json
```

**Tecnologias:**
- ASP.NET Core 9.0
- Swagger/OpenAPI
- JWT Bearer Authentication
- CORS

**Responsabilidades:**
- ✅ Validação de entrada (Data Annotations + FluentValidation)
- ✅ Autenticação e Autorização (JWT)
- ✅ Serialização JSON
- ✅ Tratamento de exceções
- ✅ Logging de requisições
- ❌ **NÃO** contém lógica de negócio

---

### 2️⃣ **CleverBudget.Application** (Aplicação)

**Responsabilidade:** Orquestrar casos de uso e validações.

```
CleverBudget.Application/
├── Services/              # (Futuro: implementações de serviços)
└── Validators/            # FluentValidation
    ├── RegisterDtoValidator.cs
    ├── CreateTransactionDtoValidator.cs
    ├── CreateCategoryDtoValidator.cs
    ├── CreateGoalDtoValidator.cs
    └── CreateRecurringTransactionDtoValidator.cs
```

**Tecnologias:**
- FluentValidation

**Responsabilidades:**
- ✅ Validações complexas de negócio
- ✅ Orquestração de múltiplos serviços
- ✅ Mapeamento entre DTOs e Entities
- ❌ **NÃO** acessa banco de dados diretamente

---

### 3️⃣ **CleverBudget.Core** (Domínio)

**Responsabilidade:** Definir as regras de negócio e modelos do domínio.

```
CleverBudget.Core/
├── Entities/              # Modelos de domínio
│   ├── User.cs                    # Usuário (Identity)
│   ├── Transaction.cs             # Transação financeira
│   ├── Category.cs                # Categoria de transação
│   ├── Goal.cs                    # Meta financeira
│   └── RecurringTransaction.cs    # Transação recorrente
├── DTOs/                  # Objetos de transferência
│   ├── AuthResponseDto.cs
│   ├── LoginDto.cs
│   ├── RegisterDto.cs
│   ├── TransactionDto.cs
│   ├── CategoryDto.cs
│   ├── GoalDto.cs
│   ├── RecurringTransactionDto.cs
│   ├── ReportDto.cs
│   └── OperationResult.cs         # Wrapper de resultados
├── Enums/                 # Enumerações
│   ├── TransactionType.cs         # Income/Expense
│   └── RecurrenceFrequency.cs     # Daily/Weekly/Monthly/Yearly
├── Interfaces/            # Contratos de serviços
│   ├── IAuthService.cs
│   ├── ITransactionService.cs
│   ├── ICategoryService.cs
│   ├── IGoalService.cs
│   ├── IRecurringTransactionService.cs
│   ├── IReportService.cs
│   ├── IExportService.cs
│   └── IEmailService.cs
└── Common/
    └── PagedResult.cs     # Resultado paginado
```

**Tecnologias:**
- .NET 9.0 Class Library
- Nenhuma dependência externa (clean!)

**Responsabilidades:**
- ✅ Definir entidades e agregados
- ✅ Definir interfaces (contratos)
- ✅ Enums e Value Objects
- ✅ Regras de negócio no domínio
- ❌ **NÃO** depende de outras camadas

---

### 4️⃣ **CleverBudget.Infrastructure** (Infraestrutura)

**Responsabilidade:** Implementar acesso a dados e serviços externos.

```
CleverBudget.Infrastructure/
├── Data/
│   └── ApplicationDbContext.cs    # DbContext do EF Core
├── Repositories/          # Implementações de repositórios
│   ├── TransactionRepository.cs
│   ├── CategoryRepository.cs
│   ├── GoalRepository.cs
│   └── RecurringTransactionRepository.cs
├── Services/              # Implementações de serviços
│   ├── AuthService.cs             # Autenticação JWT
│   ├── TransactionService.cs      # Lógica de transações
│   ├── CategoryService.cs         # Lógica de categorias
│   ├── GoalService.cs             # Lógica de metas
│   ├── RecurringTransactionService.cs
│   ├── ReportService.cs           # Geração de relatórios
│   ├── ExportService.cs           # Exportação CSV/PDF
│   ├── EmailService.cs            # Envio de e-mails
│   ├── CloudinaryService.cs       # Upload de imagens
│   └── UserProfileService.cs      # Perfil do usuário
├── Migrations/            # Migrações do EF Core
├── Extensions/            # Extension methods
│   └── ServiceCollectionExtensions.cs
└── Helpers/
    └── JwtHelper.cs       # Geração de tokens JWT
```

**Tecnologias:**
- Entity Framework Core 9.0
- SQL Server
- ASP.NET Core Identity
- Cloudinary SDK (imagens)
- MailKit (e-mails)

**Responsabilidades:**
- ✅ Acesso ao banco de dados (EF Core)
- ✅ Implementação de repositórios
- ✅ Integração com APIs externas (Cloudinary, AWS)
- ✅ Serviços de infraestrutura (Email, Storage)
- ❌ **NÃO** expõe detalhes de implementação

---

### 5️⃣ **CleverBudget.Tests** (Testes)

**Responsabilidade:** Testes unitários e de integração.

```
CleverBudget.Tests/
├── Services/              # Testes de serviços
│   ├── AuthServiceTests.cs
│   ├── TransactionServiceTests.cs
│   ├── CategoryServiceTests.cs
│   ├── GoalServiceTests.cs
│   └── UserProfileServiceTests.cs
└── Controllers/           # Testes de controllers
    ├── AuthControllerTests.cs
    ├── TransactionsControllerTests.cs
    └── ProfileControllerTests.cs
```

**Tecnologias:**
- xUnit
- Moq (mocking)
- FluentAssertions (assertions)

---

## 🔄 Fluxo de Uma Requisição

```
1. Cliente HTTP
   ↓
2. Controller (CleverBudget.Api)
   - Valida JWT
   - Valida input (FluentValidation)
   ↓
3. Service Interface (CleverBudget.Core)
   - Define contrato
   ↓
4. Service Implementation (CleverBudget.Infrastructure)
   - Executa lógica de negócio
   - Chama Repository
   ↓
5. Repository (CleverBudget.Infrastructure)
   - Acessa banco via EF Core
   ↓
6. Database (SQL Server)
   - Persiste/Recupera dados
   ↓
7. ← Retorna Result/DTO
   ↓
8. ← Controller serializa para JSON
   ↓
9. ← Cliente recebe resposta
```

## 🎯 Padrões Utilizados

### 🏛️ **Padrões Arquiteturais**
- **Clean Architecture** - Separação de camadas
- **Dependency Injection** - Inversão de controle
- **Repository Pattern** - Abstração de acesso a dados
- **Unit of Work** - Gerenciamento de transações (via DbContext)

### 🛠️ **Padrões de Código**
- **DTO Pattern** - Objetos de transferência
- **Result Pattern** - `OperationResult<T>` e `AuthResult`
- **Validator Pattern** - FluentValidation
- **Factory Methods** - `SuccessResult()`, `FailureResult()`

### 🔐 **Segurança**
- **JWT Authentication** - Tokens stateless
- **Password Hashing** - ASP.NET Core Identity (PBKDF2)
- **Content Moderation** - AWS Rekognition via Cloudinary
- **Data Protection** - Chaves persistidas

## 📊 Diagrama de Dependências

```
CleverBudget.Api
    ├── depende → CleverBudget.Application
    ├── depende → CleverBudget.Infrastructure
    └── depende → CleverBudget.Core

CleverBudget.Application
    └── depende → CleverBudget.Core

CleverBudget.Infrastructure
    └── depende → CleverBudget.Core

CleverBudget.Core
    └── sem dependências externas ✨
```

**Princípio:** Core é independente, Infrastructure e Application dependem do Core, API depende de todos.

## 🧩 Principais Componentes

### Authentication System
- **JWT Tokens** - Autenticação stateless
- **ASP.NET Identity** - Gerenciamento de usuários
- **Password Policies** - Requisitos configuráveis
- **Error Codes** - Mensagens específicas

### Transaction Management
- **CRUD Completo** - Criar, ler, atualizar, deletar
- **Filtros** - Por período, categoria, tipo
- **Paginação** - Performance otimizada
- **Recorrência** - Transações automáticas

### Reporting & Analytics
- **Relatórios** - Gastos por categoria, período
- **Exportação** - CSV, PDF (futuro)
- **Gráficos** - Dados agregados para frontend

### Content Moderation
- **Image Upload** - Via Cloudinary
- **AWS Rekognition** - Moderação automática
- **Fallback** - Se moderação falhar, imagem aceita com warning

## 🔮 Próximas Evoluções

- [ ] CQRS Pattern para queries complexas
- [ ] Event Sourcing para auditoria
- [ ] Redis Cache para performance
- [ ] SignalR para notificações real-time
- [ ] GraphQL API alternativa

## 📚 Documentos Relacionados

- [Guia de Configuração](./SETUP.md)
- [Autenticação](./AUTHENTICATION.md)
- [Database Schema](./DATABASE_SCHEMA.md)
- [Padrões de Código](./CODING_STANDARDS.md)
