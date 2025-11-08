using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Extensions;
using CleverBudget.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CleverBudget.Infrastructure.Services;

public class BudgetService : IBudgetService
{
    private readonly AppDbContext _context;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private static readonly CultureInfo PtCulture = CultureInfo.GetCultureInfo("pt-BR");

    public BudgetService(AppDbContext context)
        : this(context, NullRealtimeNotifier.Instance)
    {
    }

    public BudgetService(AppDbContext context, IRealtimeNotifier realtimeNotifier)
    {
        _context = context;
        _realtimeNotifier = realtimeNotifier ?? NullRealtimeNotifier.Instance;
    }

    public async Task<IEnumerable<BudgetResponseDto>> GetAllAsync(string userId, int? year = null, int? month = null)
    {
        var query = _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId);

        if (year.HasValue)
            query = query.Where(b => b.Year == year.Value);

        if (month.HasValue)
            query = query.Where(b => b.Month == month.Value);

        var budgets = await query
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ThenBy(b => b.Category.Name)
            .ToListAsync();

        var result = new List<BudgetResponseDto>();
        foreach (var budget in budgets)
        {
            result.Add(await MapToDtoAsync(budget, userId));
        }

        return result;
    }

    public async Task<PagedResult<BudgetResponseDto>> GetPagedAsync(
        string userId, 
        PaginationParams paginationParams, 
        int? year = null, 
        int? month = null)
    {
        var query = _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId);

        if (year.HasValue)
            query = query.Where(b => b.Year == year.Value);

        if (month.HasValue)
            query = query.Where(b => b.Month == month.Value);

        query = ApplySorting(query, paginationParams.SortBy, paginationParams.SortOrder);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToListAsync();

        var dtos = new List<BudgetResponseDto>();
        foreach (var budget in items)
        {
            dtos.Add(await MapToDtoAsync(budget, userId));
        }

        return new PagedResult<BudgetResponseDto>(
            dtos,
            paginationParams.Page,
            paginationParams.PageSize,
            totalCount
        );
    }

    public async Task<BudgetResponseDto?> GetByIdAsync(int id, string userId)
    {
        var budget = await _context.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
            return null;

        return await MapToDtoAsync(budget, userId);
    }

    public async Task<BudgetResponseDto?> GetByCategoryAndPeriodAsync(int categoryId, int month, int year, string userId)
    {
        var budget = await _context.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => 
                b.CategoryId == categoryId && 
                b.Month == month && 
                b.Year == year && 
                b.UserId == userId);

        if (budget == null)
            return null;

        return await MapToDtoAsync(budget, userId);
    }

    public async Task<BudgetResponseDto?> CreateAsync(CreateBudgetDto dto, string userId)
    {
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == dto.CategoryId && c.UserId == userId);

        if (!categoryExists)
            return null;

        var existingBudget = await _context.Budgets
            .FirstOrDefaultAsync(b => 
                b.CategoryId == dto.CategoryId && 
                b.Month == dto.Month && 
                b.Year == dto.Year && 
                b.UserId == userId);

        if (existingBudget != null)
            return null;

        var budget = new Budget
        {
            UserId = userId,
            CategoryId = dto.CategoryId,
            Amount = dto.Amount,
            Month = dto.Month,
            Year = dto.Year,
            AlertAt50Percent = dto.AlertAt50Percent,
            AlertAt80Percent = dto.AlertAt80Percent,
            AlertAt100Percent = dto.AlertAt100Percent,
            CreatedAt = DateTime.UtcNow
        };

        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        var created = await GetByIdAsync(budget.Id, userId);

        if (created != null)
        {
            await PublishOverviewAsync(userId, budget.Year, budget.Month);
        }

        return created;
    }

    public async Task<BudgetResponseDto?> UpdateAsync(int id, UpdateBudgetDto dto, string userId)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
            return null;

        if (dto.Amount.HasValue)
            budget.Amount = dto.Amount.Value;

        if (dto.AlertAt50Percent.HasValue)
            budget.AlertAt50Percent = dto.AlertAt50Percent.Value;

        if (dto.AlertAt80Percent.HasValue)
            budget.AlertAt80Percent = dto.AlertAt80Percent.Value;

        if (dto.AlertAt100Percent.HasValue)
            budget.AlertAt100Percent = dto.AlertAt100Percent.Value;

        await _context.SaveChangesAsync();

        var updated = await GetByIdAsync(budget.Id, userId);

        if (updated != null)
        {
            await PublishOverviewAsync(userId, budget.Year, budget.Month);
        }

        return updated;
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null)
            return false;

        var year = budget.Year;
        var month = budget.Month;

        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();

        await PublishOverviewAsync(userId, year, month);

        return true;
    }

    public async Task<IEnumerable<BudgetResponseDto>> GetCurrentMonthBudgetsAsync(string userId)
    {
        var now = DateTime.UtcNow;
        return await GetAllAsync(userId, now.Year, now.Month);
    }

    public async Task<decimal> GetTotalBudgetForMonthAsync(string userId, int month, int year)
    {
        return await _context.Budgets
            .Where(b => b.UserId == userId && b.Month == month && b.Year == year)
            .SumAsync(b => b.Amount);
    }

    public async Task<decimal> GetTotalSpentForMonthAsync(string userId, int month, int year)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await _context.Transactions
            .Where(t => 
                t.UserId == userId && 
                t.Type == TransactionType.Expense &&
                t.Date >= startDate && 
                t.Date <= endDate)
            .SumAsync(t => t.Amount);
    }

    public async Task<IReadOnlyCollection<BudgetTrendPointDto>> GetTrendAsync(string userId, int months = 6)
    {
        var boundedMonths = Math.Clamp(months, 1, 24);

        var now = DateTime.UtcNow;
        var currentPeriodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startPeriod = currentPeriodStart.AddMonths(-(boundedMonths - 1));
        var endPeriod = currentPeriodStart.AddMonths(1).AddTicks(-1);

        var startYear = startPeriod.Year;
        var startMonth = startPeriod.Month;
        var endYear = currentPeriodStart.Year;
        var endMonth = currentPeriodStart.Month;

        var budgetAggregates = await _context.Budgets
            .AsNoTracking()
            .Where(b => b.UserId == userId &&
                        (b.Year > startYear || (b.Year == startYear && b.Month >= startMonth)) &&
                        (b.Year < endYear || (b.Year == endYear && b.Month <= endMonth)))
            .GroupBy(b => new { b.Year, b.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Total = g.Sum(x => x.Amount),
                Categories = g.Count()
            })
            .ToListAsync();

        var expenseAggregates = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId &&
                        t.Type == TransactionType.Expense &&
                        t.Date >= startPeriod &&
                        t.Date <= endPeriod)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Total = g.Sum(x => x.Amount)
            })
            .ToListAsync();

        var budgetsByPeriod = budgetAggregates.ToDictionary(
            x => (x.Year, x.Month),
            x => (x.Total, x.Categories));

        var expensesByPeriod = expenseAggregates.ToDictionary(
            x => (x.Year, x.Month),
            x => x.Total);

        var trend = new List<BudgetTrendPointDto>(boundedMonths);

        for (var i = 0; i < boundedMonths; i++)
        {
            var period = startPeriod.AddMonths(i);
            var key = (period.Year, period.Month);

            budgetsByPeriod.TryGetValue(key, out var budgetData);
            var planned = budgetData.Total;
            var categoriesTracked = budgetData.Categories;

            expensesByPeriod.TryGetValue(key, out var spent);
            var variance = spent - planned;
            var remaining = planned - spent;
            var coverage = planned > 0 ? Math.Round((spent / planned) * 100, 2) : 0m;

            trend.Add(new BudgetTrendPointDto
            {
                Year = period.Year,
                Month = period.Month,
                MonthName = $"{PtCulture.DateTimeFormat.GetMonthName(period.Month)}/{period.Year}",
                Planned = Math.Round(planned, 2),
                Spent = Math.Round(spent, 2),
                Variance = Math.Round(variance, 2),
                Remaining = Math.Round(remaining, 2),
                CoveragePercent = coverage,
                CategoriesTracked = categoriesTracked
            });
        }

        return trend;
    }

    public async Task<BudgetOverviewDto> GetOverviewAsync(string userId, int? year = null, int? month = null)
    {
        var now = DateTime.UtcNow;
        var targetYear = year ?? now.Year;
        var targetMonth = month ?? now.Month;

        var budgets = await _context.Budgets
            .Include(b => b.Category)
            .Where(b =>
                b.UserId == userId &&
                b.Year == targetYear &&
                b.Month == targetMonth)
            .ToListAsync();

        if (budgets.Count == 0)
        {
            return new BudgetOverviewDto
            {
                Month = targetMonth,
                Year = targetYear,
                Recommendation = "Cadastre orçamentos para acompanhar seus gastos neste período."
            };
        }

        var responses = new List<BudgetResponseDto>(budgets.Count);
        foreach (var budget in budgets)
        {
            responses.Add(await MapToDtoAsync(budget, userId));
        }

        var snapshots = responses
            .Select(BuildCategorySnapshot)
            .ToList();

        var totalBudget = snapshots.Sum(x => x.Amount);
        var totalSpent = snapshots.Sum(x => x.Spent);
        var remaining = Math.Max(0m, totalBudget - totalSpent);
        var percentageUsed = totalBudget > 0 ? Math.Round((totalSpent / totalBudget) * 100, 2) : 0m;

        var atRisk = snapshots
            .Where(x =>
                x.Status == "Crítico" ||
                x.Status == "Excedido" ||
                x.ProjectedVariance > 0 ||
                x.BurnRateVariance > 0)
            .OrderByDescending(x => x.ProjectedVariance)
            .ThenByDescending(x => x.PercentageUsed)
            .ToList();

        var comfortable = snapshots
            .Where(x =>
                x.Status == "Normal" &&
                x.ProjectedVariance <= 0)
            .OrderBy(x => x.PercentageUsed)
            .ToList();

        var suggestedReallocation = Math.Round(comfortable.Sum(x => x.PotentialReallocation), 2);

        return new BudgetOverviewDto
        {
            Month = targetMonth,
            Year = targetYear,
            TotalBudget = Math.Round(totalBudget, 2),
            TotalSpent = Math.Round(totalSpent, 2),
            Remaining = Math.Round(remaining, 2),
            PercentageUsed = percentageUsed,
            SuggestedReallocation = suggestedReallocation,
            Categories = snapshots,
            AtRisk = atRisk,
            Comfortable = comfortable,
            Recommendation = BuildOverviewRecommendation(atRisk, comfortable, suggestedReallocation, targetMonth, targetYear)
        };
    }

    private async Task<BudgetResponseDto> MapToDtoAsync(Budget budget, string userId)
    {
        var metrics = await CalculateMetricsAsync(budget, userId);

        var spent = metrics.Spent;
        var remaining = budget.Amount - spent;
        var percentageUsed = budget.Amount > 0 ? (spent / budget.Amount) * 100 : 0;

        var status = DetermineStatus(percentageUsed);
        var recommendation = BuildRecommendation(
            status,
            budget.Amount,
            metrics.ProjectedVariance,
            metrics.BudgetVariance,
            metrics.HistoricalAverage,
            metrics.SuggestedBudget,
            metrics.DaysRemaining,
            metrics.TransactionsCount,
            metrics.BurnRateVariance);

    var monthName = PtCulture.DateTimeFormat.GetMonthName(budget.Month);

        return new BudgetResponseDto
        {
            Id = budget.Id,
            CategoryId = budget.CategoryId,
            CategoryName = budget.Category.Name,
            CategoryIcon = budget.Category.Icon ?? "",
            CategoryColor = budget.Category.Color ?? "",
            Amount = budget.Amount,
            Month = budget.Month,
            Year = budget.Year,
            MonthName = $"{monthName}/{budget.Year}",
            Spent = spent,
            Remaining = remaining,
            PercentageUsed = Math.Round(percentageUsed, 2),
            Status = status,
            AlertAt50Percent = budget.AlertAt50Percent,
            AlertAt80Percent = budget.AlertAt80Percent,
            AlertAt100Percent = budget.AlertAt100Percent,
            Alert50Sent = budget.Alert50Sent,
            Alert80Sent = budget.Alert80Sent,
            Alert100Sent = budget.Alert100Sent,
            CreatedAt = budget.CreatedAt,
            HistoricalAverage = metrics.HistoricalAverage,
            ProjectedSpend = metrics.ProjectedSpend,
            SuggestedBudget = metrics.SuggestedBudget,
            BudgetVariance = metrics.BudgetVariance,
            ProjectedVariance = metrics.ProjectedVariance,
            DailyBudget = metrics.DailyBudget,
            BurnRate = metrics.BurnRate,
            BurnRateVariance = metrics.BurnRateVariance,
            DaysElapsed = metrics.DaysElapsed,
            DaysRemaining = metrics.DaysRemaining,
            TransactionsCount = metrics.TransactionsCount,
            LastTransactionDate = metrics.LastTransactionDate,
            Recommendation = recommendation
        };
    }

    private async Task<BudgetMetrics> CalculateMetricsAsync(Budget budget, string userId)
    {
        var periodStart = new DateTime(budget.Year, budget.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1).AddTicks(-1);

        var today = DateTime.UtcNow;
        var currentDate = today < periodStart ? periodStart : today;
        if (currentDate > periodEnd)
        {
            currentDate = periodEnd;
        }

        var totalDays = DateTime.DaysInMonth(budget.Year, budget.Month);
        var rawDaysElapsed = (currentDate - periodStart).TotalDays + 1;
        var daysElapsed = Math.Clamp((int)Math.Floor(rawDaysElapsed <= 0 ? 1 : rawDaysElapsed), 1, totalDays);
        var daysRemaining = Math.Max(0, totalDays - daysElapsed);

        var currentStats = await _context.Transactions
            .Where(t =>
                t.UserId == userId &&
                t.CategoryId == budget.CategoryId &&
                t.Type == TransactionType.Expense &&
                t.Date >= periodStart &&
                t.Date <= periodEnd)
            .GroupBy(t => 1)
            .Select(g => new
            {
                Spent = g.Sum(x => x.Amount),
                Count = g.Count(),
                LastDate = g.Max(x => x.Date)
            })
            .FirstOrDefaultAsync();

        var spent = currentStats?.Spent ?? 0m;
        var transactionsCount = currentStats?.Count ?? 0;
        var lastTransaction = currentStats?.LastDate;

        var historicalStart = periodStart.AddMonths(-3);
        var historicalTotals = await _context.Transactions
            .Where(t =>
                t.UserId == userId &&
                t.CategoryId == budget.CategoryId &&
                t.Type == TransactionType.Expense &&
                t.Date >= historicalStart &&
                t.Date < periodStart)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => g.Sum(x => x.Amount))
            .ToListAsync();

        var historicalAverage = historicalTotals.Count > 0
            ? Math.Round(historicalTotals.Average(), 2)
            : 0m;

        var totalDaysDecimal = totalDays > 0 ? (decimal)totalDays : 1m;
        var daysElapsedDecimal = daysElapsed > 0 ? (decimal)daysElapsed : 1m;

        var dailyBudget = Math.Round(totalDays > 0 ? budget.Amount / totalDaysDecimal : 0m, 2);
        var burnRate = Math.Round(spent / daysElapsedDecimal, 2);
        var burnRateVariance = Math.Round(burnRate - dailyBudget, 2);

        var projectedSpend = Math.Round(burnRate * totalDaysDecimal, 2);

        var suggestedBudget = CalculateSuggestedBudget(
            budget.Amount,
            projectedSpend,
            historicalAverage);

        var budgetVariance = Math.Round(budget.Amount - historicalAverage, 2);
        var projectedVariance = Math.Round(projectedSpend - budget.Amount, 2);

        return new BudgetMetrics(
            spent,
            transactionsCount,
            lastTransaction,
            historicalAverage,
            projectedSpend,
            suggestedBudget,
            dailyBudget,
            burnRate,
            burnRateVariance,
            daysElapsed,
            daysRemaining,
            budgetVariance,
            projectedVariance);
    }

    private static string DetermineStatus(decimal percentageUsed)
    {
        if (percentageUsed >= 100)
            return "Excedido";
        if (percentageUsed >= 80)
            return "Crítico";
        if (percentageUsed >= 50)
            return "Alerta";

        return "Normal";
    }

    private static decimal CalculateSuggestedBudget(decimal amount, decimal projectedSpend, decimal historicalAverage)
    {
        var suggested = amount;

        if (projectedSpend > amount && projectedSpend > 0)
        {
            suggested = projectedSpend * 1.05m;
        }
        else if (historicalAverage > amount)
        {
            suggested = historicalAverage * 1.1m;
        }
        else if (historicalAverage > 0 && amount > historicalAverage * 1.3m)
        {
            suggested = Math.Max(historicalAverage * 1.15m, amount * 0.9m);
        }

        return Math.Round(Math.Max(0, suggested), 2);
    }

    private static string BuildRecommendation(
        string status,
        decimal budgetAmount,
        decimal projectedVariance,
        decimal budgetVariance,
        decimal historicalAverage,
        decimal suggestedBudget,
        int daysRemaining,
        int transactionsCount,
        decimal burnRateVariance)
    {
        if (status == "Excedido")
        {
            return "Você ultrapassou o orçamento desta categoria. Realocar gastos ou aportar recursos extras evita impacto no saldo do mês.";
        }

        var projectedThreshold = Math.Max(50m, budgetAmount * 0.1m);
        if (projectedVariance > projectedThreshold)
        {
            return "O ritmo atual indica estouro do orçamento. Reduza gastos discricionários ou aumente o limite para absorver despesas essenciais.";
        }

        if (status == "Crítico")
        {
            return daysRemaining > 0
                ? "Você está muito próximo do limite com dias restantes no mês. Controle despesas futuras e priorize itens indispensáveis."
                : "Encerramento do período acima do planejado. Considere ajustar metas ou reforçar a reserva para o próximo mês.";
        }

        if (status == "Alerta")
        {
            if (burnRateVariance > 0)
            {
                return "O gasto diário está acima do ideal. Replaneje compras e monitore as próximas transações.";
            }

            return "Você está entrando na zona de atenção. Acompanhe de perto as despesas para não alcançar níveis críticos.";
        }

        if (historicalAverage > 0 && budgetVariance < -(historicalAverage * 0.1m))
        {
            return "O orçamento está abaixo do seu histórico. Avalie aumentar o limite ou redistribuir recursos de outras categorias.";
        }

        if (historicalAverage > 0 && budgetVariance > historicalAverage * 0.3m && transactionsCount > 0)
        {
            return "Há margem sobrando nesta categoria. Realoque parte do valor para outras metas ou para sua reserva.";
        }

        var reductionThreshold = Math.Max(20m, budgetAmount * 0.05m);
        if (suggestedBudget < budgetAmount && budgetAmount - suggestedBudget >= reductionThreshold)
        {
            return "Você está confortável nesta categoria. Considere reduzir um pouco o orçamento para fortalecer outras prioridades.";
        }

        return "Você está dentro do planejado. Continue acompanhando para manter o desempenho atual.";
    }

    private async Task PublishOverviewAsync(string userId, int year, int month)
    {
        var overview = await GetOverviewAsync(userId, year, month);
        await _realtimeNotifier.NotifyBudgetOverviewUpdatedAsync(userId, overview);
    }

    private sealed class BudgetMetrics
    {
        public BudgetMetrics(
            decimal spent,
            int transactionsCount,
            DateTime? lastTransactionDate,
            decimal historicalAverage,
            decimal projectedSpend,
            decimal suggestedBudget,
            decimal dailyBudget,
            decimal burnRate,
            decimal burnRateVariance,
            int daysElapsed,
            int daysRemaining,
            decimal budgetVariance,
            decimal projectedVariance)
        {
            Spent = spent;
            TransactionsCount = transactionsCount;
            LastTransactionDate = lastTransactionDate;
            HistoricalAverage = historicalAverage;
            ProjectedSpend = projectedSpend;
            SuggestedBudget = suggestedBudget;
            DailyBudget = dailyBudget;
            BurnRate = burnRate;
            BurnRateVariance = burnRateVariance;
            DaysElapsed = daysElapsed;
            DaysRemaining = daysRemaining;
            BudgetVariance = budgetVariance;
            ProjectedVariance = projectedVariance;
        }

        public decimal Spent { get; }
        public int TransactionsCount { get; }
        public DateTime? LastTransactionDate { get; }
        public decimal HistoricalAverage { get; }
        public decimal ProjectedSpend { get; }
        public decimal SuggestedBudget { get; }
        public decimal DailyBudget { get; }
        public decimal BurnRate { get; }
        public decimal BurnRateVariance { get; }
        public int DaysElapsed { get; }
        public int DaysRemaining { get; }
        public decimal BudgetVariance { get; }
        public decimal ProjectedVariance { get; }
    }

    private static BudgetCategorySnapshotDto BuildCategorySnapshot(BudgetResponseDto dto)
    {
        var projectionSlack = dto.ProjectedVariance < 0 ? Math.Abs(dto.ProjectedVariance) : 0m;
        var suggestionSlack = dto.SuggestedBudget < dto.Amount ? dto.Amount - dto.SuggestedBudget : 0m;
        var baseSlack = Math.Max(projectionSlack, suggestionSlack);
        var available = dto.Remaining > 0 ? dto.Remaining : 0m;
        var cappedSlack = Math.Min(dto.Amount * 0.4m, Math.Min(available, baseSlack));
        var potentialReallocation = dto.Status is "Crítico" or "Excedido"
            ? 0m
            : Math.Round(Math.Max(0m, cappedSlack), 2);

        return new BudgetCategorySnapshotDto
        {
            BudgetId = dto.Id,
            CategoryId = dto.CategoryId,
            CategoryName = dto.CategoryName,
            CategoryIcon = dto.CategoryIcon,
            CategoryColor = dto.CategoryColor,
            Amount = dto.Amount,
            Spent = dto.Spent,
            Remaining = dto.Remaining,
            PercentageUsed = dto.PercentageUsed,
            Status = dto.Status,
            ProjectedSpend = dto.ProjectedSpend,
            ProjectedVariance = dto.ProjectedVariance,
            SuggestedBudget = dto.SuggestedBudget,
            BudgetVariance = dto.BudgetVariance,
            PotentialReallocation = potentialReallocation,
            DailyBudget = dto.DailyBudget,
            BurnRate = dto.BurnRate,
            BurnRateVariance = dto.BurnRateVariance,
            TransactionsCount = dto.TransactionsCount,
            LastTransactionDate = dto.LastTransactionDate,
            Recommendation = dto.Recommendation
        };
    }

    private static string BuildOverviewRecommendation(
        IReadOnlyCollection<BudgetCategorySnapshotDto> atRisk,
        IReadOnlyCollection<BudgetCategorySnapshotDto> comfortable,
        decimal suggestedReallocation,
        int month,
        int year)
    {
        var monthName = PtCulture.DateTimeFormat.GetMonthName(month);

        if (atRisk.Count > 0)
        {
            var highlight = atRisk.First();
            var projectedText = highlight.ProjectedVariance > 0
                ? $" com excedente projetado de {highlight.ProjectedVariance.ToString("C", PtCulture)}"
                : string.Empty;

            return $"Há {atRisk.Count} categorias em risco em {monthName}/{year}. Priorize ajustes em {highlight.CategoryName}{projectedText}.";
        }

        if (comfortable.Count > 0 && suggestedReallocation > 0)
        {
            return $"Você pode realocar até {suggestedReallocation.ToString("C", PtCulture)} de categorias confortáveis para reforçar outras prioridades.";
        }

        return $"Os orçamentos de {monthName}/{year} estão sob controle. Continue monitorando para manter o desempenho.";
    }

    private IQueryable<Budget> ApplySorting(
        IQueryable<Budget> query,
        string? sortBy,
        string? sortOrder)
    {
        var isDescending = sortOrder?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "amount" => isDescending 
                ? query.OrderByDescending(b => b.Amount) 
                : query.OrderBy(b => b.Amount),
            
            "category" => isDescending 
                ? query.OrderByDescending(b => b.Category.Name) 
                : query.OrderBy(b => b.Category.Name),
            
            "month" => isDescending 
                ? query.OrderByDescending(b => b.Year).ThenByDescending(b => b.Month)
                : query.OrderBy(b => b.Year).ThenBy(b => b.Month),
            
            _ => query.OrderByDescending(b => b.Year)
                      .ThenByDescending(b => b.Month)
                      .ThenBy(b => b.Category.Name)
        };
    }
}
