using CleverBudget.Core.Enums;

namespace CleverBudget.Core.Entities;

/// <summary>
/// Representa uma transação recorrente (automática)
/// </summary>
public class RecurringTransaction
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DayOfMonth { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastGeneratedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}