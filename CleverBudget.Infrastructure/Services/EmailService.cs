using CleverBudget.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;

namespace CleverBudget.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _apiKey = _configuration["Brevo:ApiKey"] ?? Environment.GetEnvironmentVariable("BREVO_API_KEY") ?? "";
        _fromEmail = _configuration["Brevo:FromEmail"] ?? Environment.GetEnvironmentVariable("BREVO__FROMEMAIL") ?? "noreply@cleverbudget.com";
        _fromName = _configuration["Brevo:FromName"] ?? Environment.GetEnvironmentVariable("BREVO__FROMNAME") ?? "CleverBudget";

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("⚠️ Brevo API Key não configurada! Emails não serão enviados.");
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var subject = "🎉 Bem-vindo ao CleverBudget!";
        var htmlContent = $@"
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Bem-vindo ao CleverBudget</title>
            </head>
            <body style='background: #f4f8fb; font-family: Arial, sans-serif; margin: 0; padding: 0;'>
                <div style='max-width: 600px; margin: 40px auto; background: #fff; border-radius: 12px; box-shadow: 0 2px 8px #0001; overflow: hidden;'>
                    <div style='background: #4A90E2; padding: 32px 0; text-align: center;'>
                        <h1 style='color: #fff; margin: 0; font-size: 2.2em;'>CleverBudget</h1>
                        <p style='color: #eaf6ff; margin: 0; font-size: 1.1em;'>Seu controle financeiro inteligente</p>
                    </div>
                    <div style='padding: 32px;'>
                        <h2 style='color: #4A90E2;'>🎉 Bem-vindo, {userName}!</h2>
                        <p>Estamos muito felizes em tê-lo(a) conosco! 🎊</p>
                        <p>O CleverBudget é sua ferramenta completa para controle financeiro inteligente. Com ele, você pode:</p>
                        <ul style='padding-left: 20px;'>
                            <li>📊 Registrar receitas e despesas</li>
                            <li>🎯 Definir metas financeiras</li>
                            <li>📈 Gerar relatórios detalhados</li>
                            <li>💡 Receber insights sobre seus gastos</li>
                        </ul>
                        <div style='text-align: center; margin: 40px 0;'>
                            <a href='https://cleverbudget.up.railway.app/' style='background: #4A90E2; color: #fff; padding: 16px 36px; border-radius: 6px; text-decoration: none; font-size: 1.1em; font-weight: bold; box-shadow: 0 2px 8px #4A90E233;'>Começar Agora</a>
                        </div>
                        <p style='color: #888; font-size: 13px; margin-top: 32px;'>Se você não se cadastrou no CleverBudget, ignore este email.</p>
                    </div>
                    <div style='background: #f4f8fb; padding: 24px 32px; text-align: center; font-size: 13px; color: #888;'>
                        <p style='margin: 0 0 8px 0;'>Dúvidas ou suporte? <a href='mailto:dev.higorxyz@gmail.com' style='color: #4A90E2; text-decoration: none;'>dev.higorxyz@gmail.com</a></p>
                        <p style='margin: 0;'>
                            <a href='https://github.com/higorxyz' style='color: #4A90E2; text-decoration: none;'>GitHub</a> |
                            <a href='https://linkedin.com/in/higorbatista' style='color: #4A90E2; text-decoration: none;'>LinkedIn</a>
                        </p>
                        <p style='margin: 16px 0 0 0; color: #bbb;'>© {DateTime.Now.Year} CleverBudget</p>
                    </div>
                </div>
            </body>
            </html>
        ";

        return await SendEmailAsync(toEmail, subject, htmlContent);
    }

    public async Task<bool> SendGoalAlertEmailAsync(string toEmail, string userName, string categoryName, decimal currentAmount, decimal targetAmount, decimal percentage)
    {
        var status = percentage >= 100 ? "excedida" : "próxima do limite";
        var emoji = percentage >= 100 ? "🚨" : "⚠️";
        var color = percentage >= 100 ? "#E74C3C" : "#F39C12";

        var subject = $"{emoji} Alerta de Meta: {categoryName}";
        var htmlContent = $@"
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Alerta de Meta</title>
            </head>
            <body style='background: #f4f8fb; font-family: Arial, sans-serif; margin: 0; padding: 0;'>
                <div style='max-width: 600px; margin: 40px auto; background: #fff; border-radius: 12px; box-shadow: 0 2px 8px #0001; overflow: hidden;'>
                    <div style='background: {color}; padding: 32px 0; text-align: center;'>
                        <h1 style='color: #fff; margin: 0; font-size: 2.2em;'>CleverBudget</h1>
                        <p style='color: #fff; margin: 0; font-size: 1.1em;'>Alerta de Meta</p>
                    </div>
                    <div style='padding: 32px;'>
                        <h2 style='color: {color};'>{emoji} Alerta de Meta!</h2>
                        <p>Olá <strong>{userName}</strong>,</p>
                        <p>Sua meta para <strong>{categoryName}</strong> está {status}:</p>
                        <div style='background: #f5f5f5; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                            <p style='margin: 5px 0;'><strong>Gasto atual:</strong> R$ {currentAmount:F2}</p>
                            <p style='margin: 5px 0;'><strong>Meta estabelecida:</strong> R$ {targetAmount:F2}</p>
                            <p style='margin: 5px 0;'><strong>Percentual:</strong> {percentage:F1}%</p>
                            <div style='background: #ddd; height: 20px; border-radius: 10px; margin-top: 10px; overflow: hidden;'>
                                <div style='background: {color}; height: 100%; width: {Math.Min(percentage, 100)}%;'></div>
                            </div>
                        </div>
                        {(percentage >= 100 
                            ? "<p style='color: #E74C3C; font-weight: bold;'>⚠️ Você ultrapassou sua meta! Considere revisar seus gastos.</p>" 
                            : "<p style='color: #F39C12; font-weight: bold;'>⚠️ Atenção! Você está próximo do limite da sua meta.</p>")}
                        <div style='text-align: center; margin: 40px 0;'>
                            <a href='https://cleverbudget.up.railway.app/' style='background: {color}; color: #fff; padding: 14px 32px; border-radius: 6px; text-decoration: none; font-size: 1.1em; font-weight: bold; box-shadow: 0 2px 8px {color}33;'>Acessar CleverBudget</a>
                        </div>
                    </div>
                    <div style='background: #f4f8fb; padding: 24px 32px; text-align: center; font-size: 13px; color: #888;'>
                        <p style='margin: 0 0 8px 0;'>Dúvidas ou suporte? <a href='mailto:dev.higorxyz@gmail.com' style='color: {color}; text-decoration: none;'>dev.higorxyz@gmail.com</a></p>
                        <p style='margin: 0;'>
                            <a href='https://github.com/higorxyz' style='color: {color}; text-decoration: none;'>GitHub</a> |
                            <a href='https://linkedin.com/in/higorbatista' style='color: {color}; text-decoration: none;'>LinkedIn</a>
                        </p>
                        <p style='margin: 16px 0 0 0; color: #bbb;'>© {DateTime.Now.Year} CleverBudget</p>
                    </div>
                </div>
            </body>
            </html>
        ";

        return await SendEmailAsync(toEmail, subject, htmlContent);
    }

    public async Task<bool> SendMonthlyReportEmailAsync(string toEmail, string userName, byte[] pdfReport, string month, int year)
    {
        var subject = $"📊 Relatório Financeiro - {month}/{year}";
        var htmlContent = $@"
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Relatório Financeiro</title>
            </head>
            <body style='background: #f4f8fb; font-family: Arial, sans-serif; margin: 0; padding: 0;'>
                <div style='max-width: 600px; margin: 40px auto; background: #fff; border-radius: 12px; box-shadow: 0 2px 8px #0001; overflow: hidden;'>
                    <div style='background: #4A90E2; padding: 32px 0; text-align: center;'>
                        <h1 style='color: #fff; margin: 0; font-size: 2.2em;'>CleverBudget</h1>
                        <p style='color: #eaf6ff; margin: 0; font-size: 1.1em;'>Relatório Financeiro</p>
                    </div>
                    <div style='padding: 32px;'>
                        <h2 style='color: #4A90E2;'>📊 Relatório Financeiro</h2>
                        <p>Olá <strong>{userName}</strong>,</p>
                        <p>Seu relatório financeiro de <strong>{month}/{year}</strong> está pronto!</p>
                        <p>Confira no anexo deste email um resumo completo de suas movimentações financeiras:</p>
                        <ul style='padding-left: 20px;'>
                            <li>💰 Total de receitas e despesas</li>
                            <li>📈 Gastos por categoria</li>
                            <li>🎯 Status das suas metas</li>
                            <li>💡 Insights sobre seus hábitos financeiros</li>
                        </ul>
                        <div style='text-align: center; margin: 40px 0;'>
                            <a href='https://cleverbudget.up.railway.app/' style='background: #4A90E2; color: #fff; padding: 14px 32px; border-radius: 6px; text-decoration: none; font-size: 1.1em; font-weight: bold; box-shadow: 0 2px 8px #4A90E233;'>Ver Detalhes na Plataforma</a>
                        </div>
                        <p style='color: #888; font-size: 13px; margin-top: 32px;'>Este é um relatório automático gerado pelo CleverBudget.</p>
                    </div>
                    <div style='background: #f4f8fb; padding: 24px 32px; text-align: center; font-size: 13px; color: #888;'>
                        <p style='margin: 0 0 8px 0;'>Dúvidas ou suporte? <a href='mailto:dev.higorxyz@gmail.com' style='color: #4A90E2; text-decoration: none;'>dev.higorxyz@gmail.com</a></p>
                        <p style='margin: 0;'>
                            <a href='https://github.com/higorxyz' style='color: #4A90E2; text-decoration: none;'>GitHub</a> |
                            <a href='https://linkedin.com/in/higorbatista' style='color: #4A90E2; text-decoration: none;'>LinkedIn</a>
                        </p>
                        <p style='margin: 16px 0 0 0; color: #bbb;'>© {DateTime.Now.Year} CleverBudget</p>
                    </div>
                </div>
            </body>
            </html>
        ";

        return await SendEmailAsync(toEmail, subject, htmlContent, pdfReport, $"relatorio-{month}-{year}.pdf");
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, byte[]? attachment = null, string? attachmentName = null)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Email não enviado: API Key do Brevo não configurada");
            return false;
        }

        try
        {
            Configuration.Default.ApiKey["api-key"] = _apiKey;

            var apiInstance = new TransactionalEmailsApi();
            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(_fromName, _fromEmail),
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(toEmail) },
                Subject = subject,
                HtmlContent = htmlContent
            };

            if (attachment != null && !string.IsNullOrEmpty(attachmentName))
            {
                sendSmtpEmail.Attachment = new List<SendSmtpEmailAttachment>
                {
                    new SendSmtpEmailAttachment
                    {
                        Content = attachment,
                        Name = attachmentName
                    }
                };
            }

            var result = await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            
            _logger.LogInformation($"✅ Email enviado com sucesso para {toEmail} | MessageId: {result.MessageId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Erro ao enviar email para {toEmail}");
            return false;
        }
    }
}