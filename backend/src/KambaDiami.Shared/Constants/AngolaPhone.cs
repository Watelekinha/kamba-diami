using System.Text.RegularExpressions;

namespace KambaDiami.Shared.Constants;

/// <summary>
/// Números móveis angolanos: 9 dígitos a começar por 9, com ou sem o indicativo +244.
/// São guardados sempre no formato +2449XXXXXXXX.
/// </summary>
public static partial class AngolaPhone
{
    [GeneratedRegex(@"^(?:\+?244|00244)?(9\d{8})$")]
    private static partial Regex MobilePattern();

    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var compact = new string(raw.Where(c => char.IsDigit(c) || c == '+').ToArray());
        var match = MobilePattern().Match(compact);
        return match.Success ? "+244" + match.Groups[1].Value : null;
    }

    public static bool IsValid(string? raw) => Normalize(raw) is not null;
}
