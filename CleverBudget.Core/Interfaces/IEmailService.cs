namespace CleverBudget.Core.Interfaces;

public interface IEmailService
{
    Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);

    /// <summary>
    /// Envia alerta quando meta atinge 80% ou é excedida
    /// </summary>
    Task<bool> SendGoalAlertEmailAsync(string toEmail, string userName, string categoryName, decimal currentAmount, decimal targetAmount, decimal percentage);

    /// <summary>
    /// Envia relatório financeiro mensal
    /// </summary>
    Task<bool> SendMonthlyReportEmailAsync(string toEmail, string userName, byte[] pdfReport, string month, int year);

    /// <summary>
    /// Envia email genérico
    /// </summary>
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, byte[]? attachment = null, string? attachmentName = null);
}