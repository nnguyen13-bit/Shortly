using Shortly.Domain.Common;

namespace Shortly.Domain.LinkManagement;

public sealed class LinkDomainException : DomainException
{
    public LinkDomainException(string message) : base(message) { }
    public LinkDomainException(string message, Exception innerException) : base(message, innerException) { }
}
