using System.Text.RegularExpressions;

namespace Shortly.Api.Validation;

public static partial class InputSanitiser
{
    /// <summary>
    /// Strips HTML/script tags and trims whitespace from input strings.
    /// Applied at the API boundary to prevent stored XSS.
    /// </summary>
    public static string? Sanitise(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var sanitised = HtmlTagRegex().Replace(input, string.Empty);
        return sanitised.Trim();
    }

    [GeneratedRegex(@"<[^>]*>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();
}
