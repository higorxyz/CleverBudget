namespace CleverBudget.Core.Entities;

public class Goal
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal TargetAmount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Relacionamentos
    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}