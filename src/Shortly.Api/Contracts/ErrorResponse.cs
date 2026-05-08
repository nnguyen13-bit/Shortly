namespace Shortly.Api.Contracts;

public sealed class ErrorResponse
{
    public required string Error { get; init; }
    public required int StatusCode { get; init; }
}
