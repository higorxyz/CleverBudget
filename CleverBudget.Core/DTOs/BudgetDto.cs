using CleverBudget.Core.Enums;

namespace CleverBudget.Core.DTOs;

/// <summary>
/// DTO para criação de orçamento
/// </summary>
public class CreateBudgetDto
{
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public bool AlertAt50Percent { get; set; } = true;
    public bool AlertAt80Percent { get; set; } = true;
    public bool AlertAt100Percent { get; set; } = true;
}

/// <summary>
/// DTO para atualização de orçamento
/// </summary>
public class UpdateBudgetDto
{
    public decimal? Amount { get; set; }
    public bool? AlertAt50Percent { get; set; }
    public bool? AlertAt80Percent { get; set; }
    public bool? AlertAt100Percent { get; set; }
}

/// <summary>
/// DTO de resposta para orçamento
/// </summary>
public class BudgetResponseDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Spent { get; set; }
    public decimal Remaining { get; set; }
    public decimal PercentageUsed { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool AlertAt50Percent { get; set; }
    public bool AlertAt80Percent { get; set; }
    public bool AlertAt100Percent { get; set; }
    public bool Alert50Sent { get; set; }
    public bool Alert80Sent { get; set; }
    public bool Alert100Sent { get; set; }
    public DateTime CreatedAt { get; set; }
}
