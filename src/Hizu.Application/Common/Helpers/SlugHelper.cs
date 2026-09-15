using System.Text.RegularExpressions;

namespace Hizu.Application.Common.Helpers;

public static class SlugHelper
{
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Trim and lowercase
        var slug = text.Trim().ToLowerInvariant();

        // Replace invalid chars with hyphen
        slug = Regex.Replace(slug, @"[^a-z0-9\u0600-\u06FF\s-]", "");

        // Convert multiple spaces/hyphens to a single hyphen
        slug = Regex.Replace(slug, @"[\s-]+", "-").Trim('-');

        return slug;
    }
}
