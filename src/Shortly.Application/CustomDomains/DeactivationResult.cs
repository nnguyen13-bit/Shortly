using Shortly.Domain.CustomDomains;

namespace Shortly.Application.CustomDomains;

public sealed record DeactivationResult(CustomDomain Domain, string? Warning);
