using System.ComponentModel.DataAnnotations;

namespace Shortly.Api.Contracts;

public sealed class CreateLinkRequest
{
    [Required]
    [StringLength(4, MinimumLength = 2)]
    [RegularExpression(@"^[a-z0-9]+$", ErrorMessage = "Domain prefix must be 2-4 lowercase alphanumeric characters.")]
    public required string DomainPrefix { get; init; }

    [Required]
    [Url]
    [StringLength(2048)]
    public required string DestinationUrl { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string CreatedBy { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public Dictionary<string, string>? Tags { get; init; }
}

public sealed class LinkResponse
{
    public required Guid Id { get; init; }
    public required string ShortCode { get; init; }
    public required string DomainPrefix { get; init; }
    public required string DestinationUrl { get; init; }
    public required string Status { get; init; }
    public required string CreatedBy { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public Dictionary<string, string>? Tags { get; init; }
    public required string ShortUrl { get; init; }
}
