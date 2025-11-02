namespace CleverBudget.Core.Entities;

/// <summary>
/// Representa um orçamento mensal para uma categoria específica
/// </summary>
public class Budget
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public bool AlertAt50Percent { get; set; } = true;
    public bool AlertAt80Percent { get; set; } = true;
    public bool AlertAt100Percent { get; set; } = true;
    public bool Alert50Sent { get; set; }
    public bool Alert80Sent { get; set; }
    public bool Alert100Sent { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
