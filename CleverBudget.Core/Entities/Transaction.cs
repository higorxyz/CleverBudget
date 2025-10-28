using CleverBudget.Core.Enums;

namespace CleverBudget.Core.Entities;

public class Transaction
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}