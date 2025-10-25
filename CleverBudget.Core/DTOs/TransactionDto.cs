using CleverBudget.Core.Enums;

namespace CleverBudget.Core.DTOs;

public class CreateTransactionDto
{
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public DateTime Date { get; set; }
}

public class UpdateTransactionDto
{
    public decimal? Amount { get; set; }
    public TransactionType? Type { get; set; }
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public DateTime? Date { get; set; }
}

public class TransactionResponseDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; }
}