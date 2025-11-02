# 💼 CleverBudget API

> **Sistema de controle financeiro inteligente desenvolvido em ASP.NET Core 9**

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Status](https://img.shields.io/badge/status-MVP-success)](https://github.com)

---

## 🚀 Sobre o Projeto

O **CleverBudget** é uma API REST completa para gerenciamento de finanças pessoais, desenvolvida seguindo os princípios de **Clean Architecture** e boas práticas de desenvolvimento.

### ✨ Principais Funcionalidades

- 🔐 **Autenticação JWT** com ASP.NET Identity
- 💸 **Gestão de Transações** (receitas e despesas)
- 🗂️ **Categorias Personalizáveis** (9 categorias padrão + customizadas)
- 🎯 **Sistema de Metas** com acompanhamento de progresso
- 📊 **Relatórios Financeiros** detalhados
- 🔍 **Filtros Avançados** por data, tipo e categoria
- 📈 **Histórico Mensal** com análise de tendências

---

## 🚀 **Como Usar**

### **1. Pré-requisitos**
- .NET 9.0 SDK
- Docker (opcional)
- Conta Brevo (para emails)
- Conta Railway (para deploy)

### **2. Configuração Local**
```bash
# Clonar o repositório
git clone <repository-url>
cd CleverBudget

# Configurar variáveis de ambiente
# Editar .env com suas chaves

# Executar migrações
dotnet ef database update

# Executar aplicação
dotnet run --project CleverBudget.Api
```

### **3. Endpoints Principais**
- `POST /api/auth/register` - Registrar usuário
- `POST /api/auth/login` - Fazer login
- `GET /api/transactions` - Listar transações
- `POST /api/transactions` - Criar transação
- `GET /api/reports` - Gerar relatórios
- `GET /api/export/pdf` - Exportar PDF
- `GET /api/export/csv` - Exportar CSV

### **4. Deploy no Railway**
```bash
# O deploy é automático via GitHub
# Configure as variáveis de ambiente no painel Railway
```

---

## 🛠️ Tecnologias Utilizadas

| Categoria | Tecnologia |
|-----------|-----------|
| **Framework** | ASP.NET Core 9.0 |
| **ORM** | Entity Framework Core |
| **Banco de Dados** | SQLite (dev) / PostgreSQL (prod) |
| **Autenticação** | JWT Bearer + Identity |
| **Documentação** | Swagger/OpenAPI |
| **Arquitetura** | Clean Architecture |
| **Testes** | xUnit + Moq |

---

## 📁 Estrutura do Projeto

```
CleverBudget/
├── CleverBudget.Api/          # Camada de apresentação (Controllers, Endpoints)
├── CleverBudget.Core/         # Entidades, DTOs, Interfaces
├── CleverBudget.Application/  # Lógica de negócio
├── CleverBudget.Infrastructure/ # Persistência, Repositórios, Serviços
└── CleverBudget.Tests/        # Testes unitários
```

---

## 🚀 Como Executar

### Pré-requisitos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Git](https://git-scm.com/)

### Instalação

```bash
# Clonar o repositório
git clone https://github.com/higorxyz/cleverbudget.git
cd cleverbudget

# Restaurar dependências
dotnet restore

# Aplicar migrations
dotnet ef database update --project CleverBudget.Infrastructure --startup-project CleverBudget.Api

# Executar a aplicação
dotnet run --project CleverBudget.Api
```

**Para desenvolvimento local:** As configurações sensíveis estão no arquivo `.env` na raiz do projeto. Certifique-se de que o arquivo existe e contém as chaves corretas. O aplicativo carrega automaticamente as variáveis de ambiente do `.env` **apenas em desenvolvimento local**. **Em produção (Railway), as variáveis são lidas do painel e o `.env` não é utilizado.**

A API estará disponível em: **http://localhost:5000**

Documentação Swagger: **http://localhost:5000**

---

## 📚 Endpoints Principais

### Autenticação
- `POST /api/auth/register` - Registrar novo usuário
- `POST /api/auth/login` - Login e geração de token JWT

### Transações
- `GET /api/transactions` - Listar transações (com filtros)
- `POST /api/transactions` - Criar transação
- `PUT /api/transactions/{id}` - Atualizar transação
- `DELETE /api/transactions/{id}` - Deletar transação

### Categorias
- `GET /api/categories` - Listar categorias
- `POST /api/categories` - Criar categoria customizada
- `PUT /api/categories/{id}` - Atualizar categoria
- `DELETE /api/categories/{id}` - Deletar categoria

### Transações Recorrentes
- `GET /api/recurringtransactions` - Listar transações recorrentes
- `POST /api/recurringtransactions` - Criar transação recorrente
- `PUT /api/recurringtransactions/{id}` - Atualizar transação recorrente
- `DELETE /api/recurringtransactions/{id}` - Deletar transação recorrente
- `POST /api/recurringtransactions/{id}/toggle` - Ativar/Desativar
- `POST /api/recurringtransactions/generate` - Gerar transações manualmente

### Orçamentos
- `GET /api/budgets` - Listar orçamentos
- `GET /api/budgets/paged` - Listar orçamentos paginados
- `GET /api/budgets/{id}` - Buscar orçamento por ID
- `GET /api/budgets/category/{categoryId}/period` - Buscar por categoria e período
- `GET /api/budgets/current` - Orçamentos do mês atual
- `GET /api/budgets/summary` - Resumo de orçamentos
- `POST /api/budgets` - Criar orçamento
- `PUT /api/budgets/{id}` - Atualizar orçamento
- `DELETE /api/budgets/{id}` - Deletar orçamento

### Metas
- `GET /api/goals` - Listar metas
- `POST /api/goals` - Criar meta mensal
- `GET /api/goals/status` - Ver progresso das metas

### Relatórios
- `GET /api/reports/summary` - Resumo geral
- `GET /api/reports/categories` - Gastos por categoria
- `GET /api/reports/monthly` - Histórico mensal
- `GET /api/reports/detailed` - Relatório completo

---

## 🔐 Autenticação

A API utiliza **JWT Bearer Token**. Para acessar endpoints protegidos:

1. Faça login em `/api/auth/login`
2. Copie o token retornado
3. No Swagger, clique em **"Authorize"** 🔒
4. Insira: `Bearer {seu_token}`

---

## 🎯 Roadmap — Linha do Tempo Visual

🟢 **Fase 1 — MVP (100% Concluído)** ⭐
- ✅ Autenticação JWT
- ✅ CRUD de Transações
- ✅ CRUD de Categorias
- ✅ Sistema de Metas
- ✅ Relatórios Financeiros

🔵 **Fase 2 — Recursos Avançados (92% Concluído)**
- ✅ Exportação PDF/CSV
- ✅ Notificações por Email (Brevo)
- ✅ Transações Recorrentes (Automáticas)
- ✅ Background Service para geração automática
- ✅ Orçamentos Mensais com alertas
- ⬜ Perfil de Usuário (nome, email, senha, foto)
- ✅ Validações com FluentValidation
- ✅ Testes unitários (293 testes - 70%+ cobertura)
- ✅ Rate Limiting (AspNetCoreRateLimit)
- ✅ Deploy no Railway

🟡 **Fase 3 — Inteligência e SaaS (Planejado)**
- ⬜ Insights financeiros automáticos
- ⬜ Previsão de gastos
- ⬜ Gamificação (conquistas/níveis)
- ⬜ Multi-moeda (USD, EUR)
- ⬜ Painel Admin (usuários, logs, auditoria)
- ⬜ Frontend React + Vercel
- ⬜ Integração com bancos (Open Banking)
- ⬜ Monitoramento e observabilidade
- ⬜ Backup/Restore de dados

---

💡 **Legenda:**  
- ✅ Concluído  
- 🔄 Em andamento  
- ⬜ Pendente / Planejado

---

## 🧪 Testes

```bash
# Executar todos os testes
dotnet test

# Com cobertura de código
dotnet test /p:CollectCoverage=true
```

---

## 📦 Deploy

### Railway (Recomendado)

1. Conecte seu repositório GitHub ao Railway
2. Configure as variáveis de ambiente:
   - `DATABASE_URL` (PostgreSQL - fornecido automaticamente pelo Railway)
   - `JwtSettings__SecretKey`
   - `BREVO__APIKEY`
   - `BREVO__FROMEMAIL` (recomendado - use um email válido)
   - `BREVO__FROMNAME` (recomendado - personalize o nome)
3. Deploy automático a cada push na `main`

---

## 🤝 Contribuindo

Contribuições são bem-vindas! Sinta-se livre para:

1. Fazer um Fork do projeto
2. Criar uma branch para sua feature (`git checkout -b feature/MinhaFeature`)
3. Commit suas mudanças (`git commit -m 'Adiciona MinhaFeature'`)
4. Push para a branch (`git push origin feature/MinhaFeature`)
5. Abrir um Pull Request

---

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

---

## 👨‍💻 Autor

**Higor Batista**

- GitHub: [@higorxyz](https://github.com/higorxyz)
- LinkedIn: [Higor Batista](https://linkedin.com/in/higorbatista)
- Email: dev.higorxyz@gmail.com

---

## 🙏 Agradecimentos

Desenvolvido como projeto de portfólio para demonstrar conhecimentos em:
- ASP.NET Core e C#
- Clean Architecture
- RESTful APIs
- Entity Framework Core
- Autenticação JWT
- Boas práticas de desenvolvimento

---

⭐ **Se este projeto te ajudou, deixe uma estrela!**
