using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleverBudget.Infrastructure.Services;

/// <summary>
/// Background Service que gera transações recorrentes automaticamente
/// </summary>
public class RecurringTransactionGeneratorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecurringTransactionGeneratorService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Verifica a cada hora

    public RecurringTransactionGeneratorService(
        IServiceProvider serviceProvider,
        ILogger<RecurringTransactionGeneratorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔄 Serviço de transações recorrentes iniciado");

        // Aguardar 10 segundos antes da primeira execução (evitar corrida no startup)
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateRecurringTransactionsAsync();
                await Task.Delay(_interval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao gerar transações recorrentes");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Retry em 5 min
            }
        }

        _logger.LogInformation("🛑 RecurringTransactionGenerator finalizado");
    }

    private async Task GenerateRecurringTransactionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var today = DateTime.UtcNow.Date;

        var recurringTransactions = await context.RecurringTransactions
            .Include(r => r.Category)
            .Where(r => r.IsActive &&
                       r.StartDate <= today &&
                       (r.EndDate == null || r.EndDate >= today))
            .ToListAsync();

        if (!recurringTransactions.Any())
        {
            _logger.LogDebug("ℹ️ Nenhuma transação recorrente ativa encontrada");
            return;
        }

        int generatedCount = 0;
        int skippedCount = 0;

        foreach (var recurring in recurringTransactions)
        {
            if (ShouldGenerateTransaction(recurring, today))
            {
                var transaction = new Transaction
                {
                    UserId = recurring.UserId,
                    CategoryId = recurring.CategoryId,
                    Amount = recurring.Amount,
                    Type = recurring.Type,
                    Description = $"{recurring.Description}",
                    Date = today,
                    CreatedAt = DateTime.UtcNow
                };

                context.Transactions.Add(transaction);
                recurring.LastGeneratedDate = today;
                generatedCount++;

                _logger.LogInformation(
                    "✅ Transação recorrente gerada: {Description} | Valor: R$ {Amount} | Tipo: {Type}",
                    recurring.Description,
                    recurring.Amount,
                    recurring.Type == TransactionType.Income ? "Receita" : "Despesa"
                );
            }
            else
            {
                skippedCount++;
            }
        }

        if (generatedCount > 0)
        {
            await context.SaveChangesAsync();
            _logger.LogInformation(
                "✅ {GeneratedCount} transações recorrentes geradas com sucesso ({SkippedCount} ignoradas)",
                generatedCount,
                skippedCount
            );
        }
        else
        {
            _logger.LogDebug("ℹ️ Nenhuma transação recorrente a ser gerada hoje");
        }
    }

    private bool ShouldGenerateTransaction(RecurringTransaction recurring, DateTime today)
    {
        // Se já gerou hoje, não gera novamente
        if (recurring.LastGeneratedDate?.Date == today)
            return false;

        // Se está antes da data de início, não gera
        if (today < recurring.StartDate.Date)
            return false;

        // Se passou da data de término, não gera
        if (recurring.EndDate.HasValue && today > recurring.EndDate.Value.Date)
            return false;

        return recurring.Frequency switch
        {
            RecurrenceFrequency.Daily => true,

            RecurrenceFrequency.Weekly =>
                recurring.DayOfWeek.HasValue && today.DayOfWeek == recurring.DayOfWeek.Value,

            RecurrenceFrequency.Monthly =>
                recurring.DayOfMonth.HasValue &&
                today.Day == Math.Min(recurring.DayOfMonth.Value, DateTime.DaysInMonth(today.Year, today.Month)),

            RecurrenceFrequency.Yearly =>
                recurring.StartDate.Month == today.Month &&
                recurring.StartDate.Day == today.Day,

            _ => false
        };
    }
}