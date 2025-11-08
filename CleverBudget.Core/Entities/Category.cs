using CleverBudget.Core.Enums;

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
    public CategoryKind Kind { get; set; } = CategoryKind.Essential;
    public string? Segment { get; set; }
    public string Tags { get; set; } = "[]";
    
    public User User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
}