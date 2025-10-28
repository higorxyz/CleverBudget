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
        _fromEmail = _configuration["Brevo:FromEmail"] ?? "noreply@cleverbudget.com";
        _fromName = _configuration["Brevo:FromName"] ?? "CleverBudget";

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
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h1 style='color: #4A90E2;'>💼 Bem-vindo ao CleverBudget!</h1>
                    <p>Olá <strong>{userName}</strong>,</p>
                    <p>Estamos muito felizes em tê-lo(a) conosco! 🎊</p>
                    <p>O CleverBudget é sua ferramenta completa para controle financeiro inteligente. Com ele, você pode:</p>
                    <ul>
                        <li>📊 Registrar receitas e despesas</li>
                        <li>🎯 Definir metas financeiras</li>
                        <li>📈 Gerar relatórios detalhados</li>
                        <li>💡 Receber insights sobre seus gastos</li>
                    </ul>
                    <p style='margin-top: 30px;'>
                        <a href='http://localhost:5220' style='background-color: #4A90E2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                            Começar Agora
                        </a>
                    </p>
                    <p style='margin-top: 30px; color: #666; font-size: 12px;'>
                        Se você não se cadastrou no CleverBudget, ignore este email.
                    </p>
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
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h1 style='color: {color};'>{emoji} Alerta de Meta!</h1>
                    <p>Olá <strong>{userName}</strong>,</p>
                    <p>Sua meta para <strong>{categoryName}</strong> está {status}:</p>
                    <div style='background-color: #f5f5f5; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                        <p style='margin: 5px 0;'><strong>Gasto atual:</strong> R$ {currentAmount:F2}</p>
                        <p style='margin: 5px 0;'><strong>Meta estabelecida:</strong> R$ {targetAmount:F2}</p>
                        <p style='margin: 5px 0;'><strong>Percentual:</strong> {percentage:F1}%</p>
                        <div style='background-color: #ddd; height: 20px; border-radius: 10px; margin-top: 10px; overflow: hidden;'>
                            <div style='background-color: {color}; height: 100%; width: {Math.Min(percentage, 100)}%;'></div>
                        </div>
                    </div>
                    {(percentage >= 100 
                        ? "<p style='color: #E74C3C; font-weight: bold;'>⚠️ Você ultrapassou sua meta! Considere revisar seus gastos.</p>" 
                        : "<p style='color: #F39C12; font-weight: bold;'>⚠️ Atenção! Você está próximo do limite da sua meta.</p>")}
                    <p style='margin-top: 30px; color: #666; font-size: 12px;'>
                        Acesse o CleverBudget para ver mais detalhes sobre suas finanças.
                    </p>
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
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h1 style='color: #4A90E2;'>📊 Relatório Financeiro</h1>
                    <p>Olá <strong>{userName}</strong>,</p>
                    <p>Seu relatório financeiro de <strong>{month}/{year}</strong> está pronto!</p>
                    <p>Confira no anexo deste email um resumo completo de suas movimentações financeiras:</p>
                    <ul>
                        <li>💰 Total de receitas e despesas</li>
                        <li>📈 Gastos por categoria</li>
                        <li>🎯 Status das suas metas</li>
                        <li>💡 Insights sobre seus hábitos financeiros</li>
                    </ul>
                    <p style='margin-top: 30px;'>
                        <a href='http://localhost:5220' style='background-color: #4A90E2; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                            Ver Detalhes na Plataforma
                        </a>
                    </p>
                    <p style='margin-top: 30px; color: #666; font-size: 12px;'>
                        Este é um relatório automático gerado pelo CleverBudget.
                    </p>
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