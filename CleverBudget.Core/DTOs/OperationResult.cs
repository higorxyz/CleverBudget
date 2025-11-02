namespace CleverBudget.Core.DTOs;

public class OperationResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }

    public static OperationResult<T> SuccessResult(T data)
    {
        return new OperationResult<T>
        {
            Success = true,
            Data = data
        };
    }

    public static OperationResult<T> FailureResult(string errorMessage, string? errorCode = null)
    {
        return new OperationResult<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}

public class OperationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }

    public static OperationResult SuccessResult()
    {
        return new OperationResult
        {
            Success = true
        };
    }

    public static OperationResult FailureResult(string errorMessage, string? errorCode = null)
    {
        return new OperationResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}
