using CleverBudget.Core.Enums;

namespace CleverBudget.Core.DTOs;

public class CreateRecurringTransactionDto
{
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DayOfMonth { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
}

public class UpdateRecurringTransactionDto
{
    public decimal? Amount { get; set; }
    public string? Description { get; set; }
    public DateTime? EndDate { get; set; }
}

public class RecurringTransactionResponseDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string TypeDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public RecurrenceFrequency Frequency { get; set; }
    public string FrequencyDescription { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DayOfMonth { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public string? DayOfWeekDescription { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastGeneratedDate { get; set; }
    public DateTime? NextGenerationDate { get; set; }
    public DateTime CreatedAt { get; set; }
}