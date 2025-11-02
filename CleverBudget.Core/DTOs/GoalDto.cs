namespace CleverBudget.Core.DTOs;

public class CreateGoalDto
{
    public int CategoryId { get; set; }
    public decimal TargetAmount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

public class UpdateGoalDto
{
    public decimal? TargetAmount { get; set; }
}

public class GoalResponseDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GoalStatusDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal Percentage { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string Status { get; set; } = string.Empty;
}