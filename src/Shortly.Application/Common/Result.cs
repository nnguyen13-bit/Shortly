namespace Shortly.Application.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict
}

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ErrorType? ErrorType { get; }

    private Result(bool isSuccess, T? value, string? error, ErrorType? errorType)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorType = errorType;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);
    public static Result<T> Failure(string error, ErrorType errorType = Common.ErrorType.Validation) => new(false, default, error, errorType);
    public static Result<T> NotFound(string error) => new(false, default, error, Common.ErrorType.NotFound);
    public static Result<T> Conflict(string error) => new(false, default, error, Common.ErrorType.Conflict);
}
