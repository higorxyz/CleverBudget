using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleverBudget.Infrastructure.Services;

public class FinancialInsightService : IFinancialInsightService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FinancialInsightService> _logger;

    public FinancialInsightService(AppDbContext context, ILogger<FinancialInsightService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FinancialInsightDto>> GenerateInsightsAsync(
        string userId,
        FinancialInsightFilter filter,
        CancellationToken cancellationToken = default)
    {
    var normalizedEndDate = NormalizeDate(filter.EndDate) ?? DateTime.UtcNow.Date;
    var normalizedStartDate = NormalizeDate(filter.StartDate) ?? normalizedEndDate.AddMonths(-3).AddDays(1 - normalizedEndDate.Day);

        if (normalizedEndDate.Kind == DateTimeKind.Unspecified)
        {
            normalizedEndDate = DateTime.SpecifyKind(normalizedEndDate, DateTimeKind.Utc);
        }

        if (normalizedStartDate.Kind == DateTimeKind.Unspecified)
        {
            normalizedStartDate = DateTime.SpecifyKind(normalizedStartDate, DateTimeKind.Utc);
        }

        var currentPeriodStart = new DateTime(normalizedEndDate.Year, normalizedEndDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        if (normalizedStartDate > currentPeriodStart)
        {
            currentPeriodStart = normalizedStartDate;
        }

        var historicalStart = normalizedStartDate.AddMonths(-6);

        var relevantTransactions = await _context.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Date >= historicalStart && t.Date <= normalizedEndDate.AddDays(1).AddTicks(-1))
            .ToListAsync(cancellationToken);

        if (filter.CategoryId.HasValue)
        {
            relevantTransactions = relevantTransactions
                .Where(t => t.CategoryId == filter.CategoryId.Value)
                .ToList();
        }

        if (!filter.IncludeExpenseInsights)
        {
            relevantTransactions = relevantTransactions
                .Where(t => t.Type != TransactionType.Expense)
                .ToList();
        }

        if (!filter.IncludeIncomeInsights)
        {
            relevantTransactions = relevantTransactions
                .Where(t => t.Type != TransactionType.Income)
                .ToList();
        }

        var budgets = await LoadBudgetsAsync(userId, currentPeriodStart, normalizedEndDate, cancellationToken);

        var insights = new List<FinancialInsightDto>();

        try
        {
            insights.AddRange(DetectCategoryOverspendInsights(relevantTransactions, currentPeriodStart, normalizedEndDate));
            insights.AddRange(DetectBurnRateInsight(relevantTransactions, currentPeriodStart, normalizedEndDate));
            insights.AddRange(DetectBudgetRiskInsights(relevantTransactions, budgets, currentPeriodStart, normalizedEndDate));
            insights.AddRange(DetectIncomeChangeInsights(relevantTransactions, currentPeriodStart, normalizedEndDate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar insights financeiros para o usuário {UserId}", userId);
        }

        return insights
            .OrderByDescending(i => i.Severity)
            .ThenByDescending(i => i.ImpactAmount ?? 0m)
            .ThenByDescending(i => i.GeneratedAt)
            .ToList();
    }

    private static DateTime? NormalizeDate(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var date = value.Value.Date;
        return date.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
            : date;
    }

    private static IEnumerable<FinancialInsightDto> DetectCategoryOverspendInsights(
        IReadOnlyCollection<Transaction> transactions,
        DateTime currentPeriodStart,
        DateTime currentPeriodEnd)
    {
        var insights = new List<FinancialInsightDto>();

        var currentExpenses = transactions
            .Where(t => t.Type == TransactionType.Expense && t.Date >= currentPeriodStart && t.Date <= currentPeriodEnd)
            .GroupBy(t => t.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                Category = g.First().Category,
                Total = g.Sum(x => x.Amount)
            })
            .ToList();

        var historicalMonthlyTotals = transactions
            .Where(t => t.Type == TransactionType.Expense && t.Date < currentPeriodStart)
            .GroupBy(t => new { t.CategoryId, t.Date.Year, t.Date.Month })
            .Select(g => new
            {
                g.Key.CategoryId,
                Total = g.Sum(x => x.Amount)
            })
            .ToList();

        var historicalAverages = historicalMonthlyTotals
            .GroupBy(x => x.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                Average = g.Average(x => x.Total),
                Observations = g.Count()
            })
            .ToDictionary(x => x.CategoryId, x => x);

        foreach (var expense in currentExpenses)
        {
            if (!historicalAverages.TryGetValue(expense.CategoryId, out var historical) || historical.Observations < 2)
            {
                continue;
            }

            if (historical.Average <= 0)
            {
                continue;
            }

            var delta = expense.Total - historical.Average;
            var ratio = expense.Total / historical.Average;

            if (ratio < 1.2m || delta < 50m)
            {
                continue;
            }

            var severity = ratio switch
            {
                >= 2.0m => InsightSeverity.Critical,
                >= 1.6m => InsightSeverity.High,
                >= 1.3m => InsightSeverity.Medium,
                _ => InsightSeverity.Low
            };

            var recommendation = severity switch
            {
                InsightSeverity.Critical => "Revise despesas desta categoria imediatamente, considere limites mais rígidos ou corte de gastos não essenciais.",
                InsightSeverity.High => "Analise quais transações são excepcionais e, se possível, distribua esse custo ao longo dos próximos meses.",
                InsightSeverity.Medium => "Monitore os próximos gastos e avalie se um ajuste no orçamento é necessário.",
                _ => "Acompanhe a categoria e limite novos gastos até o final do mês."
            };

            insights.Add(new FinancialInsightDto
            {
                Category = InsightCategory.SpendingPattern,
                Severity = severity,
                Title = $"Gastos elevados em {expense.Category.Name}",
                Summary = $"Os gastos atuais estão {ratio:P0} acima da média dos últimos meses.",
                Recommendation = recommendation,
                ImpactAmount = decimal.Round(delta, 2),
                BenchmarkAmount = decimal.Round(historical.Average, 2),
                GeneratedAt = DateTime.UtcNow,
                DataPoints = new[]
                {
                    new InsightDataPointDto
                    {
                        Label = "Mês atual",
                        Value = decimal.Round(expense.Total, 2),
                        Benchmark = decimal.Round(historical.Average, 2)
                    }
                }
            });
        }

        return insights;
    }

    private static IEnumerable<FinancialInsightDto> DetectBurnRateInsight(
        IReadOnlyCollection<Transaction> transactions,
        DateTime currentPeriodStart,
        DateTime currentPeriodEnd)
    {
        var expensesThisMonth = transactions
            .Where(t => t.Type == TransactionType.Expense && t.Date >= currentPeriodStart && t.Date <= currentPeriodEnd)
            .ToList();

        if (!expensesThisMonth.Any())
        {
            return Array.Empty<FinancialInsightDto>();
        }

        var historicalExpenses = transactions
            .Where(t => t.Type == TransactionType.Expense && t.Date < currentPeriodStart)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => g.Sum(x => x.Amount))
            .ToList();

        if (historicalExpenses.Count < 2)
        {
            return Array.Empty<FinancialInsightDto>();
        }

        var historicalAverage = historicalExpenses.Average();
        if (historicalAverage <= 0)
        {
            return Array.Empty<FinancialInsightDto>();
        }

        var daysElapsed = Math.Max(1, (currentPeriodEnd - currentPeriodStart).Days + 1);
        var totalDaysInMonth = DateTime.DaysInMonth(currentPeriodStart.Year, currentPeriodStart.Month);
        var projectedExpenses = expensesThisMonth.Sum(t => t.Amount) / daysElapsed * totalDaysInMonth;

        if (projectedExpenses <= historicalAverage * 1.25m)
        {
            return Array.Empty<FinancialInsightDto>();
        }

        var severity = projectedExpenses switch
        {
            var p when p >= historicalAverage * 1.8m => InsightSeverity.Critical,
            var p when p >= historicalAverage * 1.5m => InsightSeverity.High,
            _ => InsightSeverity.Medium
        };

        var recommendation = severity switch
        {
            InsightSeverity.Critical => "Reduza gastos imediatos e considere congelar despesas discricionárias até o próximo ciclo.",
            InsightSeverity.High => "Priorize categorias essenciais e adie compras de menor prioridade.",
            _ => "Monitore o restante do mês e mantenha o foco no orçamento planejado."
        };

        var insight = new FinancialInsightDto
        {
            Category = InsightCategory.SpendingPattern,
            Severity = severity,
            Title = "Ritmo de gastos acima do padrão",
            Summary = "O ritmo atual projeta um encerramento do mês significativamente acima da média histórica.",
            Recommendation = recommendation,
            ImpactAmount = decimal.Round(projectedExpenses - (decimal)historicalAverage, 2),
            BenchmarkAmount = decimal.Round((decimal)historicalAverage, 2),
            GeneratedAt = DateTime.UtcNow,
            DataPoints = new[]
            {
                new InsightDataPointDto
                {
                    Label = "Gasto projetado",
                    Value = decimal.Round(projectedExpenses, 2),
                    Benchmark = decimal.Round((decimal)historicalAverage, 2)
                },
                new InsightDataPointDto
                {
                    Label = "Dias decorridos",
                    Value = daysElapsed
                }
            }
        };

        return new[] { insight };
    }

    private static IEnumerable<FinancialInsightDto> DetectBudgetRiskInsights(
        IReadOnlyCollection<Transaction> transactions,
        IReadOnlyCollection<Budget> budgets,
        DateTime currentPeriodStart,
        DateTime currentPeriodEnd)
    {
        if (budgets.Count == 0)
        {
            return Array.Empty<FinancialInsightDto>();
        }

        var daysElapsed = Math.Max(1, (currentPeriodEnd - currentPeriodStart).Days + 1);
        var totalDaysInMonth = DateTime.DaysInMonth(currentPeriodStart.Year, currentPeriodStart.Month);
        var progressRatio = (decimal)daysElapsed / totalDaysInMonth;

        var expenseTotalsByCategory = transactions
            .Where(t => t.Type == TransactionType.Expense && t.Date >= currentPeriodStart && t.Date <= currentPeriodEnd)
            .GroupBy(t => t.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                Total = g.Sum(x => x.Amount)
            })
            .ToDictionary(x => x.CategoryId, x => x.Total);

        var insights = new List<FinancialInsightDto>();

        foreach (var budget in budgets)
        {
            expenseTotalsByCategory.TryGetValue(budget.CategoryId, out var spent);
            if (spent <= 0 || budget.Amount <= 0)
            {
                continue;
            }

            var usageRatio = spent / budget.Amount;
            var expectedSpend = budget.Amount * progressRatio;
            var rawDelta = spent - expectedSpend;
            var delta = rawDelta < 0 ? 0 : rawDelta;

            if (delta < budget.Amount * 0.1m && usageRatio < progressRatio + 0.15m)
            {
                continue;
            }

            var severity = usageRatio switch
            {
                >= 1.0m => InsightSeverity.Critical,
                >= 0.85m => InsightSeverity.High,
                >= 0.7m => InsightSeverity.Medium,
                _ => InsightSeverity.Low
            };

            var recommendation = severity switch
            {
                InsightSeverity.Critical => "Você ultrapassou o orçamento desta categoria. Considere mover o excedente para outra categoria ou cortar gastos imediatamente.",
                InsightSeverity.High => "Reduza gastos nesta categoria para evitar ultrapassar o orçamento até o fim do mês.",
                InsightSeverity.Medium => "Ajuste os próximos gastos ou revise o valor planejado para esta categoria.",
                _ => "Monitore a categoria para manter o orçamento equilibrado."
            };

            insights.Add(new FinancialInsightDto
            {
                Category = InsightCategory.BudgetRisk,
                Severity = severity,
                Title = $"Orçamento de {budget.Category.Name} em risco",
                Summary = $"{usageRatio:P0} do orçamento já foi utilizado com {progressRatio:P0} do mês transcorrido.",
                Recommendation = recommendation,
                ImpactAmount = decimal.Round(delta, 2),
                BenchmarkAmount = decimal.Round(expectedSpend, 2),
                GeneratedAt = DateTime.UtcNow,
                DataPoints = new[]
                {
                    new InsightDataPointDto
                    {
                        Label = "Gasto acumulado",
                        Value = decimal.Round(spent, 2),
                        Benchmark = decimal.Round(budget.Amount, 2)
                    },
                    new InsightDataPointDto
                    {
                        Label = "Progresso esperado",
                        Value = decimal.Round(progressRatio * 100, 1)
                    }
                }
            });
        }

        return insights;
    }

    private static IEnumerable<FinancialInsightDto> DetectIncomeChangeInsights(
        IReadOnlyCollection<Transaction> transactions,
        DateTime currentPeriodStart,
        DateTime currentPeriodEnd)
    {
        var currentIncome = transactions
            .Where(t => t.Type == TransactionType.Income && t.Date >= currentPeriodStart && t.Date <= currentPeriodEnd)
            .Sum(t => t.Amount);

        if (currentIncome <= 0)
        {
            return Array.Empty<FinancialInsightDto>();
        }

        var historicalIncome = transactions
            .Where(t => t.Type == TransactionType.Income && t.Date < currentPeriodStart)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => g.Sum(x => x.Amount))
            .ToList();

        if (historicalIncome.Count < 2)
        {
            return Array.Empty<FinancialInsightDto>();
        }

        var historicalAverage = historicalIncome.Average();
        if (historicalAverage <= 0)
        {
            return Array.Empty<FinancialInsightDto>();
        }

        var ratio = (decimal)(currentIncome / historicalAverage);
        if (ratio >= 0.85m && ratio <= 1.15m)
        {
            return Array.Empty<FinancialInsightDto>();
        }

        var isDecrease = ratio < 1;
        var severity = isDecrease switch
        {
            true when ratio <= 0.6m => InsightSeverity.High,
            true when ratio <= 0.75m => InsightSeverity.Medium,
            true => InsightSeverity.Low,
            false when ratio >= 1.5m => InsightSeverity.Medium,
            false => InsightSeverity.Low
        };

        var summary = isDecrease
            ? $"Receita do período está {ratio:P0} da média recente."
            : $"Receita do período está {ratio:P0} acima da média recente.";

        var recommendation = isDecrease
            ? "Aloque uma reserva extra e reduza gastos discricionários até confirmar se a receita voltará ao normal."
            : "Considere direcionar o excedente para reservas ou antecipar pagamentos planejados.";

        var insight = new FinancialInsightDto
        {
            Category = InsightCategory.IncomePattern,
            Severity = severity,
            Title = isDecrease ? "Queda de receita detectada" : "Receita acima do esperado",
            Summary = summary,
            Recommendation = recommendation,
            ImpactAmount = decimal.Round(Math.Abs((decimal)currentIncome - (decimal)historicalAverage), 2),
            BenchmarkAmount = decimal.Round((decimal)historicalAverage, 2),
            GeneratedAt = DateTime.UtcNow,
            DataPoints = new[]
            {
                new InsightDataPointDto
                {
                    Label = "Receita atual",
                    Value = decimal.Round((decimal)currentIncome, 2),
                    Benchmark = decimal.Round((decimal)historicalAverage, 2)
                }
            }
        };

        return new[] { insight };
    }

    private async Task<IReadOnlyCollection<Budget>> LoadBudgetsAsync(
        string userId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken)
    {
        var month = periodStart.Month;
        var year = periodStart.Year;

        return await _context.Budgets
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.UserId == userId && b.Month == month && b.Year == year)
            .ToListAsync(cancellationToken);
    }
}
