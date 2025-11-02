dotnet ef migrations add NomeDaMigracao --project CleverBudget.Infrastructure --startup-project CleverBudget.Api
dotnet ef database update --project CleverBudget.Infrastructure --startup-project CleverBudget.Api
dotnet ef database update MigracaoAnterior --project CleverBudget.Infrastructure --startup-project CleverBudget.Api
dotnet ef migrations remove --project CleverBudget.Infrastructure --startup-project CleverBudget.Api
dotnet ef migrations script --project CleverBudget.Infrastructure --startup-project CleverBudget.Api -o migration.sql
# 🔌 Serviços Externos - CleverBudget

Esta API depende de alguns serviços/bibliotecas externas opcionais. A tabela abaixo resume o que realmente está em uso no código atual.

| Serviço | Uso na aplicação | Obrigatório | Como habilitar |
|---------|-----------------|-------------|----------------|
| [Brevo](#brevo-email-transacional) | envio de e-mails (boas-vindas, alertas de orçamento) | ❌ | definir `Brevo__ApiKey`, `Brevo__FromEmail`, `Brevo__FromName` |
| [Cloudinary](#cloudinary-upload-de-foto) | upload e moderação de fotos de perfil | ❌ | definir `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret` |
| [QuestPDF](#questpdf-relatorios-pdf) | geração de relatórios em PDF | ✅ biblioteca | já incluída via NuGet |
| [CsvHelper](#csvhelper-exportacao-csv) | exportação de listas em CSV | ✅ biblioteca | já incluída via NuGet |
| [Serilog](#serilog-logging-estruturado) | logging estruturado | ✅ biblioteca | configurado em `Program.cs` |
| [AspNetCoreRateLimit](#rate-limiting) | proteção contra abuso | ✅ biblioteca | configurado em `appsettings.json` |

Integrações não configuradas simplesmente degradam a funcionalidade (por exemplo, sem Brevo a API apenas registra um aviso e segue).

## Brevo (e-mail transacional)

- **O que faz:** `AuthService` dispara e-mails de boas-vindas; `BudgetAlertService` envia alertas quando um orçamento atinge 50%, 80% ou 100% do limite.
- **Pacote:** `sib_api_v3_sdk`.
- **Variáveis:**
  - `Brevo__ApiKey` ou `BREVO__APIKEY`
  - `Brevo__FromEmail` / `Brevo__FromName` (opcional; possuem defaults)
- **Fallback:** se `ApiKey` não estiver configurada, o `EmailService` retorna `false` e loga `⚠️ Brevo API Key não configurada!`.
- **Teste rápido:** cadastre um usuário via `POST /api/auth/register` e verifique os logs; com a chave válida o envio aparece nos logs da Brevo.

## Cloudinary (upload de foto)

- **O que faz:** endpoint `POST /api/profile/photo` envia a imagem para a Cloudinary, aplica transformação `500x500` com `gravity=face` e moderação `aws_rek`. Se a moderação reprovar, o arquivo é apagado e o usuário recebe erro amigável.
- **Variáveis:** `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret` (ou equivalentes em maiúsculas).
- **Comportamento sem credenciais:** a API responde `400` com mensagem "Falha ao processar upload da imagem" e registra um aviso.
- **Limites padrão:** plano gratuito (25 GB armazenamento / 25 GB bandwidth por mês).
- **Requisitos da requisição:**
  - Arquivo até 5 MB
  - Formatos permitidos: `.jpg`, `.jpeg`, `.png`, `.webp`
  - Conteúdo validado por assinatura (magic bytes) antes do upload

## QuestPDF (relatórios PDF)

- **Uso:** serviços de exportação (`ExportService`) geram PDFs para transações e relatórios financeiros (`GET /api/export/transactions/pdf`, `GET /api/export/financial-report/pdf`).
- **Licença:** Community Edition (gratuita) – definido em `Program.cs`: `QuestPDF.Settings.License = LicenseType.Community;`.
- **Dependências extras:** nenhuma. Certifique-se apenas de ter fontes padrão disponíveis no ambiente (Windows/Linux/macOS já possuem).

## CsvHelper (exportação CSV)

- **Uso:** `ExportService` produz arquivos `.csv` para transações, categorias e metas (`/api/export/*/csv`).
- **Configuração:** os mapas (`TransactionCsvMap`, etc.) convertem tipos enum em texto amigável em português.
- **Codificação:** UTF-8 sem BOM, compatível com Excel/Google Sheets.

## Serilog (logging estruturado)

- **Configuração:** controlada por `appsettings.json` (console + arquivo em `logs/cleverbudget-.log`).
- **Campos adicionais:** MachineName, ThreadId, correlação via `Enrich.FromLogContext()`.
- **Boas práticas:** logs podem conter dados sensíveis (token ausente/erros); mantenha o diretório protegido ao rodar em produção.

## Rate limiting

- **Biblioteca:** `AspNetCoreRateLimit`.
- **Configuração:** seções `IpRateLimiting` e `IpRateLimitPolicies` em `appsettings.json` limitam requisições por IP (60/minuto, 1000/hora e 5 tentativas em 15 minutos para `api/auth`).
- **Importante:** ajuste os limites ao expor a API em produção; valores muito baixos podem bloquear usuários legítimos.

## Como desligar funcionalidades

- **Brevo:** omita `Brevo__ApiKey`. Os métodos retornam `false` e nenhuma exceção é lançada.
- **Cloudinary:** omita as variáveis ou não chame o endpoint de upload (há um `PUT /api/profile/photo` legado que aceita URL direta sem Cloudinary).
- **Exportações:** se não precisar de PDF/CSV, basta não usar os endpoints; as dependências continuam, mas não adicionam custo.

## Links úteis

- Brevo: [developers.brevo.com](https://developers.brevo.com/)
- Cloudinary: [cloudinary.com/documentation](https://cloudinary.com/documentation)
- QuestPDF: [questpdf.com](https://www.questpdf.com/)
- CsvHelper: [joshclose.github.io/CsvHelper](https://joshclose.github.io/CsvHelper/)
- Serilog: [serilog.net](https://serilog.net/)
- AspNetCoreRateLimit: [github.com/stefanprodan/AspNetCoreRateLimit](https://github.com/stefanprodan/AspNetCoreRateLimit)

## Referências no código

- `CleverBudget.Infrastructure/Services/EmailService.cs`
- `CleverBudget.Infrastructure/Services/CloudinaryImageUploadService.cs`
- `CleverBudget.Infrastructure/Services/ExportService.cs`
- `CleverBudget.Api/Program.cs`
- `CleverBudget.Api/Controllers/*`
