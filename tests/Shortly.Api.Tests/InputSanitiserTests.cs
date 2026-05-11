using Shortly.Api.Validation;

namespace Shortly.Api.Tests;

public sealed class InputSanitiserTests
{
    [Fact]
    public void Sanitise_NullInput_ReturnsNull()
    {
        Assert.Null(InputSanitiser.Sanitise(null));
    }

    [Fact]
    public void Sanitise_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal("", InputSanitiser.Sanitise(""));
    }

    [Fact]
    public void Sanitise_PlainText_ReturnsSameText()
    {
        Assert.Equal("hello world", InputSanitiser.Sanitise("hello world"));
    }

    [Fact]
    public void Sanitise_HtmlTags_StripsAllTags()
    {
        Assert.Equal("alert('xss')", InputSanitiser.Sanitise("<script>alert('xss')</script>"));
    }

    [Fact]
    public void Sanitise_MixedHtml_StripsTagsKeepsText()
    {
        Assert.Equal("Hello World", InputSanitiser.Sanitise("<b>Hello</b> <i>World</i>"));
    }

    [Fact]
    public void Sanitise_NestedTags_StripsAll()
    {
        Assert.Equal("content", InputSanitiser.Sanitise("<div><span>content</span></div>"));
    }

    [Fact]
    public void Sanitise_WhitespaceOnly_ReturnsWhitespace()
    {
        // string.IsNullOrWhiteSpace returns true, so it's returned as-is
        Assert.Equal("   ", InputSanitiser.Sanitise("   "));
    }

    [Fact]
    public void Sanitise_LeadingTrailingWhitespace_Trims()
    {
        Assert.Equal("test", InputSanitiser.Sanitise("  test  "));
    }

    [Fact]
    public void Sanitise_ImgTagWithOnError_Strips()
    {
        Assert.Equal("", InputSanitiser.Sanitise("<img src=x onerror=alert(1)>"));
    }

    [Fact]
    public void Sanitise_EventHandlerAttributes_Strips()
    {
        Assert.Equal("Click", InputSanitiser.Sanitise("<a href=\"#\" onclick=\"steal()\">Click</a>"));
    }
}
