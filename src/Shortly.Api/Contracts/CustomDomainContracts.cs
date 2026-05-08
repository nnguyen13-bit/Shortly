using System.ComponentModel.DataAnnotations;

namespace Shortly.Api.Contracts;

public sealed class RegisterCustomDomainRequest
{
    [Required]
    [StringLength(4, MinimumLength = 2)]
    [RegularExpression(@"^[a-z0-9]+$", ErrorMessage = "Prefix must be 2-4 lowercase alphanumeric characters.")]
    public required string Prefix { get; init; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Name { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }
}

public sealed class CustomDomainResponse
{
    public required Guid Id { get; init; }
    public required string Prefix { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? Warning { get; init; }
}
