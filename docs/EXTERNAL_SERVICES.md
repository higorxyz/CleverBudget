# 🔌 Serviços e Dependências Externas - CleverBudget

## 📋 Visão Geral

Este documento descreve **todas as dependências externas** do CleverBudget, incluindo serviços de terceiros, bibliotecas NuGet e ferramentas necessárias para o funcionamento completo da aplicação.

## 🎯 Índice Rápido

| Serviço/Biblioteca | Categoria | Obrigatório | Plano Gratuito |
|-------------------|-----------|-------------|----------------|
| [Brevo](#-brevo-email-service) | Email | ❌ Não | ✅ 300/dia |
| [Cloudinary](#%EF%B8%8F-cloudinary-image-storage) | Armazenamento | ❌ Não | ✅ 25GB |
| [AWS Rekognition](#-aws-rekognition-via-cloudinary) | IA/Moderação | ❌ Não | ✅ Via Cloudinary |
| [QuestPDF](#-questpdf-geração-de-pdf) | Biblioteca | ❌ Não | ✅ Grátis |
| [CsvHelper](#-csvhelper-exportação-csv) | Biblioteca | ❌ Não | ✅ Grátis |
| [Entity Framework Core](#%EF%B8%8F-entity-framework-core) | ORM | ✅ Sim | ✅ Grátis |
| [ASP.NET Core Identity](#-aspnet-core-identity) | Autenticação | ✅ Sim | ✅ Grátis |
| [FluentValidation](#-fluentvalidation) | Validação | ✅ Sim | ✅ Grátis |
| [Serilog](#-serilog-logging) | Logging | ✅ Sim | ✅ Grátis |

---

## 📧 Brevo (Email Service)

### 📝 Descrição

**Brevo** (anteriormente Sendinblue) é um serviço de e-mail transacional usado para enviar notificações automáticas aos usuários.

### ✨ Funcionalidades no CleverBudget

- ✉️ **Alertas de Orçamento Excedido** - Notifica quando gastos ultrapassam limite
- ✉️ **Lembretes de Metas** (futuro) - Notifica sobre progresso de metas
- ✉️ **Resumo Mensal** (futuro) - Relatório financeiro por e-mail
- ✉️ **Confirmação de Alterações** (futuro) - Mudanças no perfil

### � Por que Brevo?

- ✅ **300 e-mails/dia gratuitos** (9.000/mês)
- ✅ **API RESTful simples** e bem documentada
- ✅ **Templates transacionais** profissionais
- ✅ **Entregas rápidas** com alta taxa de sucesso
- ✅ **Dashboard completo** com estatísticas detalhadas
- ✅ **Sem limites de contatos**
- ✅ **Autenticação SPF/DKIM** incluída automaticamente

### 🆓 Plano Gratuito

| Recurso | Limite Gratuito |
|---------|----------------|
| E-mails/dia | 300 |
| E-mails/mês | ~9.000 |
| Contatos | Ilimitado |
| API Access | ✅ Completo |
| Templates | ✅ Incluído |
| Statistics | ✅ Dashboard completo |
| Suporte | Email |

### 🚀 Configuração Passo a Passo

#### 1️⃣ Criar Conta no Brevo

1. Acesse [https://www.brevo.com/](https://www.brevo.com/)
2. Clique em **Sign up free**
3. Preencha os dados:
   - Nome e Sobrenome
   - E-mail profissional
   - Senha forte (mínimo 8 caracteres)
4. Confirme seu e-mail (verifique spam)
5. Complete o perfil da conta (tipo de empresa, objetivo, etc.)

#### 2️⃣ Obter API Key

1. Faça login no [Brevo Dashboard](https://app.brevo.com/)
2. No menu lateral, vá em **SMTP & API**
3. Clique na aba **API Keys**
4. Clique em **Generate a new API key**
5. Dê um nome descritivo (ex: "CleverBudget Production API")
6. Copie a chave gerada (formato: `xkeysib-...`)

⚠️ **IMPORTANTE:** 
- Salve a chave em local seguro (gerenciador de senhas)
- A chave só é exibida uma vez
- Nunca commite a chave no Git
- Gere chaves diferentes para dev/staging/prod

**Exemplo de API Key:**
```
xkeysib-1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef-XyZ123AbC
```

#### 3️⃣ Configurar Remetente Verificado

Para evitar que seus e-mails caiam em spam:

1. No Brevo Dashboard, vá em **Senders**
2. Clique em **Add a sender**
3. Preencha:
   - **Name:** CleverBudget (ou nome da sua empresa)
   - **Email:** noreply@seudominio.com
4. Clique no link de verificação enviado ao seu e-mail
5. Configure SPF/DKIM no seu DNS (opcional mas recomendado):
   - Brevo fornece registros DNS automaticamente
   - Melhora drasticamente a entregabilidade

**Opções de Remetente:**

- **Domínio Próprio (Recomendado):** `noreply@cleverbudget.com`
  - Mais profissional
  - Melhor reputação
  - Configure SPF/DKIM
  
- **E-mail Pessoal (Dev/Teste):** `seuemail@gmail.com`
  - Rápido para testes
  - Menos profissional
  - Pode ter limites menores

#### 4️⃣ Configurar Variáveis de Ambiente

**Development (`.env` ou `appsettings.Development.json`):**
```json
{
  "Brevo": {
    "ApiKey": "xkeysib-sua-chave-de-desenvolvimento",
    "FromEmail": "noreply@localhost.com",
    "FromName": "CleverBudget Dev"
  }
}
```

**Production (Railway):**
```bash
Brevo__ApiKey=xkeysib-sua-chave-de-producao
Brevo__FromEmail=noreply@cleverbudget.com
Brevo__FromName=CleverBudget
```

#### 5️⃣ Testar Envio

Após configurar, teste o envio:

1. Execute a API localmente
2. Crie um orçamento e exceda o limite
3. Verifique se recebeu o e-mail
4. Acesse Brevo → **Statistics** para ver envios

### � E-mails Enviados pelo Sistema

#### 1. Alerta de Orçamento Excedido

**Trigger:** Quando gastos ultrapassam limite do orçamento

**Assunto:** `⚠️ Orçamento Excedido - [Categoria]`

**Exemplo:**
```
Olá João,

Seu orçamento para "Alimentação" foi excedido!

📊 Limite: R$ 500,00
💸 Gasto: R$ 650,00
⚠️ Excedeu: 130% (R$ 150,00 a mais)

Acesse o CleverBudget para revisar seus gastos.

---
CleverBudget - Controle suas finanças com inteligência
```

#### 2. Lembrete de Meta (Futuro)

**Trigger:** Prazo se aproximando ou progresso estagnado

**Assunto:** `🎯 Lembrete de Meta - [Nome da Meta]`

#### 3. Resumo Mensal (Futuro)

**Trigger:** Todo dia 1º do mês

**Assunto:** `📊 Seu Resumo Financeiro de [Mês]`

### 🔍 Monitoramento

#### Dashboard Brevo

Acesse **Statistics** para ver:
- 📊 E-mails enviados (tempo real)
- ✅ Taxa de entrega (delivery rate)
- 📬 Taxa de abertura (open rate)
- 🖱️ Taxa de cliques (click rate)
- ❌ E-mails bloqueados (bounces)
- 🚫 Marcados como spam

#### Logs da Aplicação

```csharp
// Logs automáticos no console/arquivo
[Information] Sending email to user@example.com via Brevo
[Information] Email sent successfully. MessageId: <abc123>
[Error] Failed to send email: Invalid API Key
```

### 🆘 Troubleshooting

#### ❌ Erro: "Invalid API Key"

**Causa:** API Key incorreta, expirada ou inválida

**Soluções:**
1. Verifique se copiou a chave completa (sem espaços)
2. Confirme que está usando a chave correta (dev vs prod)
3. Gere uma nova chave se necessário
4. Verifique variável de ambiente: `echo $Brevo__ApiKey`

#### ❌ Erro: "Sender not verified"

**Causa:** E-mail remetente não verificado no Brevo

**Soluções:**
1. Acesse Brevo → **Senders**
2. Verifique se e-mail em `FromEmail` está listado
3. Clique no link de verificação enviado
4. Aguarde até 10 minutos após verificação

#### ❌ E-mails caindo em spam

**Causas comuns:**
- Remetente não verificado
- SPF/DKIM não configurados
- Conteúdo com palavras suspeitas
- Sem opção de unsubscribe

**Soluções:**
1. Configure SPF/DKIM no seu domínio (via DNS)
2. Use domínio próprio (evite Gmail/Hotmail como remetente)
3. Evite palavras: "grátis", "ganhe", "clique aqui"
4. Inclua footer com opção de descadastramento
5. Teste com [Mail Tester](https://www.mail-tester.com/)

#### ❌ E-mails não chegam

**Checklist de Diagnóstico:**
1. ✅ API Key está correta
2. ✅ Remetente está verificado
3. ✅ Não atingiu limite de 300/dia
4. ✅ E-mail destinatário existe e está correto
5. ✅ Sem bloqueio no firewall/antivírus
6. ✅ Verifique pasta de spam do destinatário
7. ✅ Confira logs: `logs/log-{Date}.txt`
8. ✅ Veja Statistics no Brevo Dashboard

### 💰 Limites e Upgrade

#### Plano Gratuito

- **E-mails/dia:** 300
- **E-mails/mês:** ~9.000
- **Contatos:** Ilimitado
- **API:** Completa
- **Suporte:** Email

#### Quando Fazer Upgrade?

Considere plano pago se:
- Ultrapassar 300 e-mails/dia regularmente
- Precisar de suporte prioritário
- Quiser remover marca "Sent via Brevo"
- Necessitar de recursos avançados (A/B testing, automações)

**Planos Pagos:**
- **Starter:** €25/mês (20.000 e-mails)
- **Business:** €65/mês (100.000 e-mails)
- **Enterprise:** Customizado

### 🔒 Segurança

#### Boas Práticas

```csharp
// ❌ ERRADO - Hardcoded no código
var apiKey = "xkeysib-1234...";

// ✅ CORRETO - Variável de ambiente
var apiKey = _configuration["Brevo:ApiKey"];

// ✅ CORRETO - Validar antes de usar
if (string.IsNullOrEmpty(apiKey))
{
    throw new InvalidOperationException("Brevo API Key not configured");
}
```

#### Checklist de Segurança

- [ ] API Key armazenada em variável de ambiente
- [ ] Chaves diferentes para dev/staging/prod
- [ ] API Key nunca commitada no Git
- [ ] Rotação de chaves a cada 6 meses
- [ ] Monitoramento de uso no dashboard
- [ ] 2FA ativado na conta Brevo
- [ ] Logs de envio auditados regularmente

### 🔗 Links Úteis

- 🌐 [Brevo Website](https://www.brevo.com/)
- 📚 [Documentação API](https://developers.brevo.com/)
- 💬 [Suporte](https://help.brevo.com/)
- 📊 [Status Page](https://status.brevo.com/)
- 🎓 [Brevo Academy](https://academy.brevo.com/)
- 🔑 [Dashboard - API Keys](https://app.brevo.com/settings/keys/api)
- 📧 [Dashboard - Senders](https://app.brevo.com/senders/list/manage)
- 📈 [Dashboard - Statistics](https://app.brevo.com/statistics/email)

---

## ☁️ Cloudinary (Image Storage)

### 📝 Descrição

**Cloudinary** é um serviço de gerenciamento de mídia na nuvem usado para armazenar e processar fotos de perfil dos usuários.

### ✨ Funcionalidades no CleverBudget

- 📸 **Upload de Fotos de Perfil** - Armazenamento seguro na nuvem
- 🔄 **Transformações Automáticas** - Redimensionamento 500x500, crop facial
- 🌍 **CDN Global** - Entrega rápida de imagens
- 🛡️ **Moderação de Conteúdo** - Integração com AWS Rekognition
- 🗑️ **Deleção Automática** - Remove imagens rejeitadas

### 🆓 Plano Gratuito

- **25 GB de armazenamento**
- **25 GB de bandwidth/mês**
- Transformações ilimitadas
- CDN global incluído
- API completa
- Addon gratuito: AWS Rekognition Auto Moderation

### 🚀 Configuração

#### 1. Criar Conta

1. Acesse [https://cloudinary.com/](https://cloudinary.com/)
2. Clique em **Sign up for free**
3. Confirme seu e-mail

#### 2. Obter Credenciais

1. Acesse o [Cloudinary Dashboard](https://console.cloudinary.com/)
2. Na tela inicial, copie:
   - **Cloud Name** (ex: `dz1a2b3c4`)
   - **API Key** (ex: `123456789012345`)
   - **API Secret** (clique no ícone 👁️ para revelar)

#### 3. Ativar Moderação de Conteúdo (Opcional)

1. No Dashboard, vá em **Settings** → **Security**
2. Role até **Add-ons**
3. Encontre **AWS Rekognition Auto Moderation**
4. Clique em **Activate** (é gratuito!)
5. Configure níveis de moderação:
   - Explicit Nudity: **Block**
   - Suggestive: **Block**
   - Violence: **Block**
   - Visually Disturbing: **Block**
   - Rude Gestures: **Block**
   - Drugs: **Block**
   - Tobacco: **Block**
   - Alcohol: **Block**
   - Gambling: **Block**
   - Hate Symbols: **Block**

#### 4. Configurar Variáveis

**Development (`.env` ou `appsettings.Development.json`):**
```json
{
  "Cloudinary": {
    "CloudName": "seu-cloud-name-dev",
    "ApiKey": "123456789012345",
    "ApiSecret": "abc123def456ghi789"
  }
}
```

**Production (Railway):**
```bash
Cloudinary__CloudName=seu-cloud-name-prod
Cloudinary__ApiKey=123456789012345
Cloudinary__ApiSecret=abc123def456ghi789
```

### 🖼️ Uso na API

**Endpoint:** `POST /api/profile/photo`

**Request:**
```http
POST /api/profile/photo HTTP/1.1
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: [arquivo de imagem]
```

**Response (Sucesso):**
```json
{
  "photoUrl": "https://res.cloudinary.com/your-cloud/image/upload/v123456/users/user-id.jpg"
}
```

**Response (Imagem Rejeitada):**
```json
{
  "message": "Imagem rejeitada: conteúdo impróprio detectado. Por favor, escolha outra imagem."
}
```

### 🔒 Segurança

O sistema implementa **5 camadas de validação**:

1. ✅ **Content-Type** - Apenas `image/jpeg`, `image/png`, `image/webp`
2. ✅ **Extensão** - Apenas `.jpg`, `.jpeg`, `.png`, `.webp`
3. ✅ **Magic Bytes** - Verifica assinatura binária real
4. ✅ **Tamanho** - Máximo 5 MB
5. ✅ **Moderação IA** - AWS Rekognition (se ativado)

### 📚 Documentação Completa

Veja [CLOUDINARY_SETUP.md](../CLOUDINARY_SETUP.md) no diretório raiz.

### 🔗 Links Úteis

- [Website](https://cloudinary.com/)
- [Documentação](https://cloudinary.com/documentation)
- [Dashboard](https://console.cloudinary.com/)
- [API Reference](https://cloudinary.com/documentation/image_upload_api_reference)

---

## 🤖 AWS Rekognition (via Cloudinary)

### 📝 Descrição

**AWS Rekognition** é um serviço de IA da Amazon usado para moderação automática de conteúdo de imagens. É integrado ao Cloudinary via addon.

### ✨ Funcionalidades no CleverBudget

- 🔍 **Detecção de Conteúdo Impróprio** - Nudez, violência, drogas, etc.
- ⚡ **Análise Rápida** - Menos de 1 segundo
- 🗑️ **Deleção Automática** - Remove imagens rejeitadas
- 📊 **Logs de Moderação** - Auditoria completa

### 🆓 Custo

- **Gratuito** quando usado via Cloudinary addon
- Primeiro 1.000 análises/mês: grátis (AWS)
- Sem cobrança adicional no plano gratuito Cloudinary

### 🚀 Configuração

**Não requer configuração separada!** 

A moderação é ativada automaticamente quando você:
1. Ativa o addon no Cloudinary (ver seção Cloudinary acima)
2. Configura as credenciais do Cloudinary

### 🛡️ Conteúdo Detectado e Bloqueado

- ❌ Nudez explícita
- ❌ Conteúdo sexual/sugestivo
- ❌ Violência e sangue
- ❌ Drogas e parafernália
- ❌ Armas
- ❌ Símbolos de ódio
- ❌ Gestos obscenos
- ❌ Conteúdo perturbador

### 📝 Logs

```csharp
// Logs gerados automaticamente
[Information] Uploading image to Cloudinary with moderation
[Information] Image approved by AWS Rekognition
[Warning] Image rejected: Explicit nudity detected
```

### 🔗 Links Úteis

- [AWS Rekognition](https://aws.amazon.com/rekognition/)
- [Cloudinary Moderation Addon](https://cloudinary.com/documentation/aws_rekognition_ai_moderation_addon)

---

## 📄 QuestPDF (Geração de PDF)

### 📝 Descrição

**QuestPDF** é uma biblioteca .NET moderna e poderosa para geração de documentos PDF programaticamente.

### ✨ Funcionalidades no CleverBudget

- 📊 **Relatórios em PDF** - Exportação de relatórios financeiros
- 📈 **Gráficos e Tabelas** - Visualizações profissionais
- 🎨 **Design Customizável** - Layout flexível e responsivo

### 🆓 Licença

- **Community Edition:** Gratuita para uso pessoal e comercial
- **Professional:** Para empresas com receita > $1M/ano

### 📦 Instalação

Já incluído no projeto via NuGet:

```bash
dotnet add package QuestPDF --version 2024.7.3
```

### 💻 Uso no Código

```csharp
// ExportService.cs
public byte[] GenerateTransactionsPdf(List<TransactionDto> transactions)
{
    return Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(12));

            page.Header().Text("Relatório de Transações")
                .SemiBold().FontSize(20);

            page.Content().Column(column =>
            {
                // Tabela de transações
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80);  // Data
                        columns.RelativeColumn();     // Descrição
                        columns.ConstantColumn(80);  // Valor
                    });

                    foreach (var transaction in transactions)
                    {
                        table.Cell().Text(transaction.Date.ToShortDateString());
                        table.Cell().Text(transaction.Description);
                        table.Cell().Text($"R$ {transaction.Amount:N2}");
                    }
                });
            });

            page.Footer().AlignCenter()
                .Text($"Gerado em {DateTime.Now:dd/MM/yyyy}");
        });
    }).GeneratePdf();
}
```

### 🎯 Endpoint na API

**`GET /api/export/transactions/pdf`**

**Query Parameters:**
- `startDate` - Data inicial (opcional)
- `endDate` - Data final (opcional)
- `categoryId` - Filtrar por categoria (opcional)

**Response:**
```
Content-Type: application/pdf
Content-Disposition: attachment; filename="transactions_2024-11-02.pdf"

[PDF Binary Data]
```

### 🔗 Links Úteis

- [Website](https://www.questpdf.com/)
- [Documentação](https://www.questpdf.com/documentation/getting-started.html)
- [GitHub](https://github.com/QuestPDF/QuestPDF)
- [Exemplos](https://www.questpdf.com/documentation/examples.html)

---

## 📊 CsvHelper (Exportação CSV)

### 📝 Descrição

**CsvHelper** é uma biblioteca .NET para leitura e escrita de arquivos CSV de forma fácil e eficiente.

### ✨ Funcionalidades no CleverBudget

- 📥 **Exportação de Transações** - Download de dados em CSV
- 📋 **Formato Padrão** - Compatível com Excel, Google Sheets
- 🔄 **Mapeamento Automático** - Converte DTOs para CSV

### 🆓 Licença

- **MS-PL e Apache 2.0** - Gratuita e open-source

### 📦 Instalação

Já incluído no projeto via NuGet:

```bash
dotnet add package CsvHelper --version 30.0.1
```

### 💻 Uso no Código

```csharp
// ExportService.cs
public byte[] GenerateTransactionsCsv(List<TransactionDto> transactions)
{
    using var memoryStream = new MemoryStream();
    using var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8);
    using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

    // Configurar headers em português
    csvWriter.Context.RegisterClassMap<TransactionCsvMap>();
    
    // Escrever dados
    csvWriter.WriteRecords(transactions);
    streamWriter.Flush();

    return memoryStream.ToArray();
}

// Mapeamento customizado
public class TransactionCsvMap : ClassMap<TransactionDto>
{
    public TransactionCsvMap()
    {
        Map(m => m.Date).Name("Data");
        Map(m => m.Description).Name("Descrição");
        Map(m => m.CategoryName).Name("Categoria");
        Map(m => m.Type).Name("Tipo").Convert(row => 
            row.Value.Type == TransactionType.Expense ? "Despesa" : "Receita");
        Map(m => m.Amount).Name("Valor");
    }
}
```

### 🎯 Endpoint na API

**`GET /api/export/transactions/csv`**

**Query Parameters:**
- `startDate` - Data inicial (opcional)
- `endDate` - Data final (opcional)
- `categoryId` - Filtrar por categoria (opcional)

**Response:**
```
Content-Type: text/csv
Content-Disposition: attachment; filename="transactions_2024-11-02.csv"

Data,Descrição,Categoria,Tipo,Valor
2024-11-01,Almoço no restaurante,Alimentação,Despesa,45.50
2024-11-02,Salário,Salário,Receita,5000.00
```

### 🔗 Links Úteis

- [Website](https://joshclose.github.io/CsvHelper/)
- [Documentação](https://joshclose.github.io/CsvHelper/getting-started)
- [GitHub](https://github.com/JoshClose/CsvHelper)

---

## 🗄️ Entity Framework Core

### 📝 Descrição

**Entity Framework Core** é um ORM (Object-Relational Mapper) moderno para .NET que permite trabalhar com bancos de dados usando objetos C#.

### ✨ Funcionalidades no CleverBudget

- 💾 **Acesso a Dados** - CRUD completo para todas as entidades
- 🔄 **Migrations** - Versionamento do schema do banco
- 🔍 **LINQ Queries** - Consultas fortemente tipadas
- 📊 **Change Tracking** - Rastreamento automático de mudanças
- 🔐 **SQL Injection Protection** - Queries parametrizadas

### 📦 Pacotes Instalados

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
```

### 💻 Configuração

**Connection String (Development):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CleverBudget;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**Connection String (Production - Railway):**
```bash
ConnectionStrings__DefaultConnection="Server=production-server;Database=CleverBudget;User Id=admin;Password=***;TrustServerCertificate=True;"
```

### 🔧 Comandos Úteis

```bash
# Criar migration
dotnet ef migrations add NomeDaMigracao --project CleverBudget.Infrastructure --startup-project CleverBudget.Api

# Aplicar migrations
dotnet ef database update --project CleverBudget.Infrastructure --startup-project CleverBudget.Api

# Reverter migration
dotnet ef database update MigracaoAnterior --project CleverBudget.Infrastructure --startup-project CleverBudget.Api

# Remover última migration (não aplicada)
dotnet ef migrations remove --project CleverBudget.Infrastructure --startup-project CleverBudget.Api

# Gerar script SQL
dotnet ef migrations script --project CleverBudget.Infrastructure --startup-project CleverBudget.Api -o migration.sql
```

### 🔗 Links Úteis

- [Documentação](https://learn.microsoft.com/en-us/ef/core/)
- [Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [LINQ Queries](https://learn.microsoft.com/en-us/ef/core/querying/)

---

## 🔐 ASP.NET Core Identity

### 📝 Descrição

**ASP.NET Core Identity** é um sistema completo de gerenciamento de usuários e autenticação para aplicações .NET.

### ✨ Funcionalidades no CleverBudget

- 👤 **Gerenciamento de Usuários** - Registro, login, perfil
- 🔒 **Hash de Senhas** - PBKDF2 com salt
- 🔑 **Validação de Senha** - Regras configuráveis
- 🛡️ **Security Stamps** - Invalidação de sessões
- 🔐 **Two-Factor Auth** - Suporte a 2FA (futuro)

### 📦 Pacotes Instalados

```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.0" />
```

### 💻 Configuração

```csharp
// Program.cs
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Requisitos de senha
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Configurações de usuário
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
```

### 🔗 Links Úteis

- [Documentação](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [Password Configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration)

---

## ✅ FluentValidation

### 📝 Descrição

**FluentValidation** é uma biblioteca .NET para construir regras de validação fortemente tipadas usando uma API fluente.

### ✨ Funcionalidades no CleverBudget

- ✔️ **Validação de DTOs** - Validação antes de processar requisições
- 🔗 **Integração ASP.NET** - Validação automática em controllers
- 🌍 **Mensagens Customizadas** - Erros em português
- 🎯 **Regras Complexas** - Validações condicionais

### 📦 Instalação

```xml
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
```

### 💻 Exemplo de Uso

```csharp
// CreateTransactionDtoValidator.cs
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

        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .When(x => x.CategoryId.HasValue)
            .WithMessage("ID de categoria inválido");
    }
}
```

### 🔗 Links Úteis

- [Website](https://fluentvalidation.net/)
- [Documentação](https://docs.fluentvalidation.net/)
- [ASP.NET Integration](https://docs.fluentvalidation.net/en/latest/aspnet.html)

---

## 📝 Serilog (Logging)

### 📝 Descrição

**Serilog** é uma biblioteca de logging estruturado para .NET que torna os logs mais úteis e pesquisáveis.

### ✨ Funcionalidades no CleverBudget

- 📊 **Logs Estruturados** - JSON formatado
- 📁 **File Sink** - Salva em `logs/log-{Date}.txt`
- 🖥️ **Console Sink** - Output no terminal
- 🔍 **Log Levels** - Information, Warning, Error, etc.
- 📅 **Rolling Files** - Um arquivo por dia

### 📦 Instalação

```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
```

### 💻 Configuração

```csharp
// Program.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
```

### 📋 Exemplo de Logs

```
[2024-11-02 14:30:15 INF] Application starting up
[2024-11-02 14:30:16 INF] Now listening on: http://localhost:5000
[2024-11-02 14:32:45 INF] HTTP POST /api/auth/register responded 200 in 245ms
[2024-11-02 14:35:12 WRN] Failed login attempt for user@example.com
[2024-11-02 14:40:00 ERR] Database connection failed: Timeout expired
```

### 🔗 Links Úteis

- [Website](https://serilog.net/)
- [GitHub](https://github.com/serilog/serilog)
- [Sinks](https://github.com/serilog/serilog/wiki/Provided-Sinks)

---

## 📊 Resumo de Custos

| Serviço | Plano Gratuito | Limite | Custo Pago |
|---------|---------------|--------|------------|
| **Brevo** | 300 emails/dia | 9.000/mês | €25/mês (25k emails) |
| **Cloudinary** | 25 GB storage | 25 GB bandwidth/mês | $89/mês (plus) |
| **AWS Rekognition** | Via Cloudinary | 1.000 análises/mês | Incluído no Cloudinary |
| **QuestPDF** | Ilimitado | - | $500/ano (pro) |
| **CsvHelper** | Ilimitado | - | Grátis (open-source) |
| **EF Core** | Ilimitado | - | Grátis (open-source) |
| **Identity** | Ilimitado | - | Grátis (Microsoft) |
| **FluentValidation** | Ilimitado | - | Grátis (open-source) |
| **Serilog** | Ilimitado | - | Grátis (open-source) |

**💡 Custo Total Mensal (Plano Gratuito):** R$ 0,00  
**💰 Custo Estimado (Planos Pagos):** ~R$ 600/mês (se exceder limites)

---

## 🔧 Checklist de Configuração

Use este checklist ao configurar um novo ambiente:

### Development

- [ ] ✅ .NET 9.0 SDK instalado
- [ ] ✅ SQL Server LocalDB configurado
- [ ] ✅ Variáveis de ambiente no `.env` ou `appsettings.Development.json`
- [ ] ⬜ Brevo API Key (opcional - para testar e-mails)
- [ ] ⬜ Cloudinary credenciais (opcional - para testar upload)
- [ ] ✅ Migrations aplicadas (`dotnet ef database update`)

### Production (Railway)

- [ ] ✅ Database provisionado (PostgreSQL ou SQL Server)
- [ ] ✅ `ConnectionStrings__DefaultConnection` configurado
- [ ] ✅ `Jwt__Key` configurado (32+ caracteres)
- [ ] ✅ `Jwt__Issuer` e `Jwt__Audience` configurados
- [ ] ⬜ `Brevo__ApiKey` configurado (se usar e-mails)
- [ ] ⬜ `Brevo__FromEmail` e `Brevo__FromName` configurados
- [ ] ⬜ `Cloudinary__CloudName`, `ApiKey`, `ApiSecret` configurados (se usar upload)
- [ ] ✅ `ASPNETCORE_ENVIRONMENT=Production`
- [ ] ✅ Migrations aplicadas automaticamente (via `Program.cs`)

---

## 🆘 Troubleshooting

### Problema: Erro ao enviar e-mail

**Sintomas:** Exception ao tentar enviar notificações

**Soluções:**
1. Verifique se `Brevo__ApiKey` está configurado
2. Confirme que a API Key é válida
3. Verifique se `FromEmail` está verificado no Brevo
4. Confira logs: `logs/log-{Date}.txt`

### Problema: Erro ao fazer upload de imagem

**Sintomas:** 500 Internal Server Error no `POST /api/profile/photo`

**Soluções:**
1. Verifique se `Cloudinary__*` variáveis estão configuradas
2. Confirme que as credenciais estão corretas
3. Verifique tamanho da imagem (máx 5 MB)
4. Verifique formato (apenas JPEG, PNG, WebP)

### Problema: PDF não é gerado

**Sintomas:** Endpoint `/api/export/transactions/pdf` retorna erro

**Soluções:**
1. Confirme que `QuestPDF` está instalado
2. Verifique se há transações para exportar
3. Confira logs para erros específicos

### Problema: Migration falha

**Sintomas:** `dotnet ef database update` com erro

**Soluções:**
1. Verifique connection string
2. Confirme que SQL Server está rodando
3. Tente recriar database: `dotnet ef database drop` e `update`
4. Verifique permissões do usuário do banco

---

## 📚 Documentos Relacionados

- [Setup Completo](./SETUP.md) - Configuração inicial do projeto
- [Brevo Setup](./BREVO_SETUP.md) - Guia detalhado do Brevo
- [Cloudinary Setup](../CLOUDINARY_SETUP.md) - Guia detalhado do Cloudinary
- [Variáveis de Ambiente](./ENVIRONMENT_VARIABLES.md) - Referência completa
- [Deploy](./DEPLOYMENT.md) - Como fazer deploy

---

**🎉 Todas as dependências documentadas!**

Com este guia, você tem uma visão completa de todos os serviços e bibliotecas usados no CleverBudget.
