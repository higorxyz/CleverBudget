using System.Linq;
using System.Text.Json;

namespace CleverBudget.Infrastructure.Helpers;

public static class CategoryTagHelper
{
    public static IReadOnlyCollection<string> Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        try
        {
            var tags = JsonSerializer.Deserialize<string[]>(raw);
            return tags?.Where(t => !string.IsNullOrWhiteSpace(t))
                       .Select(t => t.Trim())
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .ToArray() ?? Array.Empty<string>();
        }
        catch
        {
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(t => t.Trim())
                      .Where(t => t.Length > 0)
                      .Distinct(StringComparer.OrdinalIgnoreCase)
                      .ToArray();
        }
    }

    public static string Serialize(IEnumerable<string>? tags)
    {
        if (tags == null)
        {
            return "[]";
        }

        var normalized = tags.Where(t => !string.IsNullOrWhiteSpace(t))
                              .Select(t => t.Trim())
                              .Distinct(StringComparer.OrdinalIgnoreCase)
                              .ToArray();

        return normalized.Length == 0 ? "[]" : JsonSerializer.Serialize(normalized);
    }
}
