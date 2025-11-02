using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleverBudget.Infrastructure.Services;

/// <summary>
/// Serviço em background que verifica os orçamentos e envia alertas por email
/// </summary>
public class BudgetAlertService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BudgetAlertService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);

    public BudgetAlertService(
        IServiceProvider serviceProvider,
        ILogger<BudgetAlertService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔔 BudgetAlertService iniciado - Verificação a cada {Interval}", _checkInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckBudgetAlertsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao verificar alertas de orçamento");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckBudgetAlertsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTime.UtcNow;
        var currentMonth = now.Month;
        var currentYear = now.Year;

        var budgets = await context.Budgets
            .Include(b => b.Category)
            .Include(b => b.User)
            .Where(b => b.Month == currentMonth && b.Year == currentYear)
            .ToListAsync();

        _logger.LogInformation("🔍 Verificando {Count} orçamentos para {Month}/{Year}", 
            budgets.Count, currentMonth, currentYear);

        foreach (var budget in budgets)
        {
            try
            {
                await CheckAndSendAlertsAsync(budget, context, emailService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao verificar orçamento ID {BudgetId}", budget.Id);
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task CheckAndSendAlertsAsync(Budget budget, AppDbContext context, IEmailService emailService)
    {
        var spent = await GetSpentForBudgetAsync(budget, context);
        var percentageUsed = budget.Amount > 0 ? (spent / budget.Amount) * 100 : 0;

        if (percentageUsed >= 100 && budget.AlertAt100Percent && !budget.Alert100Sent)
        {
            await SendAlertEmailAsync(budget, spent, percentageUsed, "100%", emailService);
            budget.Alert100Sent = true;
            _logger.LogInformation("📧 Alerta 100% enviado - Orçamento ID {BudgetId}, Categoria: {Category}", 
                budget.Id, budget.Category.Name);
        }
        else if (percentageUsed >= 80 && budget.AlertAt80Percent && !budget.Alert80Sent)
        {
            await SendAlertEmailAsync(budget, spent, percentageUsed, "80%", emailService);
            budget.Alert80Sent = true;
            _logger.LogInformation("📧 Alerta 80% enviado - Orçamento ID {BudgetId}, Categoria: {Category}", 
                budget.Id, budget.Category.Name);
        }
        else if (percentageUsed >= 50 && budget.AlertAt50Percent && !budget.Alert50Sent)
        {
            await SendAlertEmailAsync(budget, spent, percentageUsed, "50%", emailService);
            budget.Alert50Sent = true;
            _logger.LogInformation("📧 Alerta 50% enviado - Orçamento ID {BudgetId}, Categoria: {Category}", 
                budget.Id, budget.Category.Name);
        }
    }

    private async Task<decimal> GetSpentForBudgetAsync(Budget budget, AppDbContext context)
    {
        var startDate = new DateTime(budget.Year, budget.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await context.Transactions
            .Where(t => 
                t.UserId == budget.UserId && 
                t.CategoryId == budget.CategoryId &&
                t.Type == TransactionType.Expense &&
                t.Date >= startDate && 
                t.Date <= endDate)
            .SumAsync(t => t.Amount);
    }

    private async Task SendAlertEmailAsync(
        Budget budget, 
        decimal spent, 
        decimal percentageUsed, 
        string alertLevel,
        IEmailService emailService)
    {
        var subject = $"🚨 Alerta de Orçamento - {alertLevel} do limite atingido!";
        
        var statusEmoji = alertLevel switch
        {
            "100%" => "🔴",
            "80%" => "🟠",
            "50%" => "🟡",
            _ => "⚠️"
        };

        var message = $@"
            <h2>{statusEmoji} Alerta de Orçamento - {alertLevel}</h2>
            
            <p>Olá,</p>
            
            <p>Você atingiu <strong>{Math.Round(percentageUsed, 2)}%</strong> do seu orçamento para a categoria <strong>{budget.Category.Name}</strong>.</p>
            
            <h3>📊 Detalhes:</h3>
            <ul>
                <li><strong>Categoria:</strong> {budget.Category.Icon} {budget.Category.Name}</li>
                <li><strong>Orçamento:</strong> R$ {budget.Amount:N2}</li>
                <li><strong>Gasto até agora:</strong> R$ {spent:N2}</li>
                <li><strong>Restante:</strong> R$ {(budget.Amount - spent):N2}</li>
                <li><strong>Utilizado:</strong> {Math.Round(percentageUsed, 2)}%</li>
            </ul>
            
            {(percentageUsed >= 100 
                ? "<p style='color: red;'><strong>⚠️ Atenção: Você já ultrapassou o limite do orçamento!</strong></p>"
                : percentageUsed >= 80
                    ? "<p style='color: orange;'><strong>⚠️ Cuidado: Você está próximo do limite!</strong></p>"
                    : "<p style='color: #DAA520;'>💡 Acompanhe seus gastos para não ultrapassar o orçamento.</p>"
            )}
            
            <p>Continue acompanhando suas finanças no CleverBudget! 💼</p>
        ";

        await emailService.SendEmailAsync(budget.User.Email!, subject, message);
    }
}
