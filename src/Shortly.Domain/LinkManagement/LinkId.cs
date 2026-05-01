namespace Shortly.Domain.LinkManagement;

public readonly record struct LinkId(Guid Value)
{
    public static LinkId New() => new(Guid.NewGuid());
}
