using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CleverBudget.Api.Extensions;

public static class EtagGenerator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static string Create(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}
