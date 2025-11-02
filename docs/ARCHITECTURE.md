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
## 📐 Visão Geral

O CleverBudget utiliza uma arquitetura em camadas. Cada projeto cumpre um papel específico e depende apenas do que realmente precisa para entregar a funcionalidade.

```
┌─────────────────────────────────────────┐
│         CleverBudget.Api                │  ← Camada de Apresentação
│      (Controllers, Configuração)        │
└─────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────┐
│      CleverBudget.Application           │  ← Camada de Aplicação
│          (Validadores)                  │
└─────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────┐
│         CleverBudget.Core               │  ← Camada de Domínio
│    (Entidades, DTOs, Interfaces)        │
└─────────────────────────────────────────┘
                         ↑
┌─────────────────────────────────────────┐
│    CleverBudget.Infrastructure          │  ← Camada de Infraestrutura
│ (EF Core, Serviços, Integrações externas)│
└─────────────────────────────────────────┘
```

## 📦 Camadas do Projeto

### 1️⃣ **CleverBudget.Api** (Apresentação)

**Responsabilidade:** expor endpoints HTTP, aplicar autenticação/autorização e compor a pipeline da aplicação.

```
CleverBudget.Api/
├── Controllers/
│   ├── AuthController.cs
│   ├── BudgetsController.cs
│   ├── CategoriesController.cs
│   ├── ExportController.cs
│   ├── GoalsController.cs
│   ├── ProfileController.cs
│   ├── RecurringTransactionsController.cs
│   ├── ReportsController.cs
│   └── TransactionsController.cs
├── Program.cs
├── appsettings*.json
└── DataProtection-Keys/
```

**Highlights:**
- ASP.NET Core 9, Minimal Hosting Model.
- Configuração de Swagger, JWT Bearer, Rate Limiting, Logging (Serilog) e CORS.
- Responsável apenas por orquestrar requisições; regras de negócio vivem nas camadas inferiores.

---

### 2️⃣ **CleverBudget.Application** (Aplicação)

**Responsabilidade:** centralizar validações de entrada reutilizáveis. Atualmente a pasta contém validadores FluentValidation para DTOs expostos pela API.

```
CleverBudget.Application/
└── Validators/
     ├── CreateBudgetDtoValidator.cs
     ├── CreateCategoryDtoValidator.cs
     ├── CreateGoalDtoValidator.cs
     ├── CreateRecurringTransactionDtoValidator.cs
     ├── CreateTransactionDtoValidator.cs
     ├── RegisterDtoValidator.cs
     └── UserProfileDtoValidator.cs
```

> Nota: não há serviços ou handlers nesta camada no momento. A intenção futura é mover orquestrações mais complexas para cá.

---

### 3️⃣ **CleverBudget.Core** (Domínio)

**Responsabilidade:** definir o contrato da aplicação (entidades, DTOs, enums e interfaces). Nenhuma dependência externa é utilizada aqui.

```
CleverBudget.Core/
├── Common/              # Tipos utilitários (PagedResult, PaginationParams)
├── DTOs/                # Contratos usados na fronteira da aplicação
├── Entities/            # Modelos de domínio
├── Enums/               # Enumerações compartilhadas
└── Interfaces/          # Interfaces consumidas pela API
```

Interfaces como `ITransactionService`, `IGoalService` ou `IExportService` são implementadas na camada de infraestrutura.

---

### 4️⃣ **CleverBudget.Infrastructure** (Infraestrutura)

**Responsabilidade:** implementar as interfaces definidas no Core, coordenar o Entity Framework Core, integrar com serviços externos (Cloudinary, Brevo, QuestPDF) e hospedar os background services.

```
CleverBudget.Infrastructure/
├── Data/
│   └── AppDbContext.cs            # DbContext baseado em IdentityDbContext
├── Extensions/
│   └── QueryableExtensions.cs     # Helpers de paginação
├── Helpers/
│   └── PdfHelper.cs               # Elementos visuais padrão para PDFs
├── Services/
│   ├── AuthService.cs
│   ├── BudgetAlertService.cs      # BackgroundService
│   ├── BudgetService.cs
│   ├── CategoryService.cs
│   ├── CloudinaryImageUploadService.cs
│   ├── EmailService.cs
│   ├── ExportService.cs
│   ├── GoalService.cs
│   ├── RecurringTransactionGeneratorService.cs # BackgroundService
│   ├── RecurringTransactionService.cs
│   ├── ReportService.cs
│   ├── TransactionService.cs
│   └── UserProfileService.cs
└── Migrations/           # (gerado ao aplicar migrations via EF Core CLI)
```

> Não há camada de repositórios dedicada: os serviços usam diretamente o `AppDbContext` para consultar e persistir dados.

---

### 5️⃣ **CleverBudget.Tests** (Testes)

**Responsabilidade:** garantir que controllers e serviços respeitem os contratos definidos. A suíte cobre cenários principais de autenticação, transações, orçamentos, metas, relatórios, exportação, perfil e integrações externas simuladas.

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

## 🔄 Fluxo de Uma Requisição

```
1. Cliente HTTP
    ↓
2. Controller (CleverBudget.Api)
    - Valida JWT e o modelo recebido
    - Encaminha para a interface correspondente
    ↓
3. Interface definida no Core (ex.: ITransactionService)
    ↓
4. Implementação na Infrastructure
    - Usa AppDbContext (EF Core) para ler/escrever dados
    - Chama serviços auxiliares (email, exportação, storage)
    ↓
5. Banco de dados / serviços externos
    ↓
6. Resultado retorna como DTO para o controller → resposta HTTP
```

## 🎯 Padrões Utilizados

- **Injeção de dependências** para isolar contratos e implementações.
- **DTO Pattern** para controlar o que trafega pela API.
- **Result Pattern** (`AuthResult`, `OperationResult<T>`) para mensagens ricas de erro.
- **FluentValidation** para validar DTOs antes da execução de regras de negócio.
- **BackgroundService** para rotinas recorrentes (transações recorrentes e alertas de orçamento).

## 🔐 Considerações de Segurança

- Autenticação baseada em JWT com ASP.NET Identity.
- Políticas de senha configuradas via `IdentityOptions`.
- `AspNetCoreRateLimit` para mitigar abuso de endpoints públicos.
- Data Protection com persistência de chaves em disco (desenvolvimento) e proteção adicional em produção.
- Upload de imagens com moderação automática (Cloudinary + AWS Rekognition).

## � Diagrama de Dependências

```
CleverBudget.Api
     ├── depende → CleverBudget.Application
     ├── depende → CleverBudget.Infrastructure
     └── depende → CleverBudget.Core

CleverBudget.Application → CleverBudget.Core
CleverBudget.Infrastructure → CleverBudget.Core
CleverBudget.Tests → (todas as camadas conforme o cenário)

CleverBudget.Core → sem dependências internas
```

## 🔮 Próximos Passos

- Migrar orquestrações mais complexas para serviços na camada Application quando necessário.
- Reduzir consultas N+1 em serviços de orçamento e metas com consultas agregadas.
- Introduzir cache ou CQRS caso surja necessidade de escalabilidade.

## 📚 Documentos Relacionados

- [Guia de Configuração](./SETUP.md)
- [Autenticação](./AUTHENTICATION.md)
- [Database Schema](./DATABASE_SCHEMA.md)
- [Testes](./TESTING.md)
- **Clean Architecture** - Separação de camadas
