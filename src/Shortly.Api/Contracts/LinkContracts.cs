using System.ComponentModel.DataAnnotations;

namespace Shortly.Api.Contracts;

public sealed class CreateLinkRequest : IValidatableObject
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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tags is null)
            yield break;

        if (Tags.Count > 10)
        {
            yield return new ValidationResult(
                "Tags cannot exceed 10 entries.",
                [nameof(Tags)]);
        }

        foreach (var (key, value) in Tags)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                yield return new ValidationResult(
                    "Tag key cannot be empty.",
                    [nameof(Tags)]);
            }
            else if (key.Length > 50)
            {
                yield return new ValidationResult(
                    $"Tag key '{key}' exceeds 50 characters.",
                    [nameof(Tags)]);
            }

            if (value is not null && value.Length > 200)
            {
                yield return new ValidationResult(
                    $"Tag value for key '{key}' exceeds 200 characters.",
                    [nameof(Tags)]);
            }
        }
    }
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
