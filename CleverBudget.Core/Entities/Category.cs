namespace CleverBudget.Core.Entities;

public class Category
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public User User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
}