namespace Shortly.Domain.CustomDomains;

public readonly record struct CustomDomainId(Guid Value)
{
    public static CustomDomainId New() => new(Guid.NewGuid());
}
