using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EntityFrameworkRuler.Extension;

internal static class StringExtensions {
    /// <summary> Return true if strings are equal. </summary>
    [DebuggerStepThrough]
    public static bool EqualsIgnoreCase(this string str, string str2) => string.Equals(str, str2, StringComparison.OrdinalIgnoreCase);

    /// <summary> Return true if string starts with the given string. </summary>
    [DebuggerStepThrough]
    public static bool StartsWithIgnoreCase([NotNullWhen(true)] this string str, string str2) =>
        str?.StartsWith(str2, StringComparison.OrdinalIgnoreCase) == true;

    /// <summary> Return true if string starts with the given string. </summary>
    [DebuggerStepThrough]
    public static bool EndsWithIgnoreCase([NotNullWhen(true)] this string str, string str2) =>
        str?.EndsWith(str2, StringComparison.OrdinalIgnoreCase) == true;

    /// <summary> Return null if the given value is empty.  Return the original value otherwise. </summary>
    [DebuggerStepThrough]
    public static string NullIfEmpty(this string str) { return string.IsNullOrEmpty(str) ? null : str; }

    /// <summary> Return null if the given value is whitespace or empty.  Return the original value otherwise. </summary>
    [DebuggerStepThrough]
    public static string NullIfWhitespace(this string str) { return string.IsNullOrWhiteSpace(str) ? null : str; }

    /// <summary> Indicates whether the specified string is null or an System.String.Empty string. </summary>
    [DebuggerStepThrough]
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string str) { return string.IsNullOrEmpty(str); }

    /// <summary> Indicates whether the specified string is null or an System.String.Empty string. </summary>
    [DebuggerStepThrough]
    public static bool HasCharacters([NotNullWhen(true)] this string str) { return !string.IsNullOrEmpty(str); }

    /// <summary> Indicates whether a specified string is null, empty, or consists only of white-space characters. </summary>
    [DebuggerStepThrough]
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string str) { return string.IsNullOrWhiteSpace(str); }

    /// <summary> Indicates whether a specified string is null, empty, or consists only of white-space characters. </summary>
    [DebuggerStepThrough]
    public static bool HasNonWhiteSpace([NotNullWhen(true)] this string str) { return !string.IsNullOrWhiteSpace(str); }

    /// <summary>
    /// Concatenates the members of a constructed System.Collections.Generic.IEnumerable&lt;T&gt;
    /// collection of type System.String, using the specified separator between each member.
    /// </summary>
    [DebuggerStepThrough]
    public static string Join(this IEnumerable<string> strs, string separator = ", ") {
        return string.Join(separator, strs);
    }

    /// <summary> Take the first string that is not null or empty </summary>
    [DebuggerStepThrough]
    public static string Coalesce(this string str, params string[] strings) {
        return string.IsNullOrEmpty(str) ? strings.FirstOrDefault(s => !string.IsNullOrEmpty(s)) : str;
    }

    /// <summary> Take the first string that is not null or empty </summary>
    [DebuggerStepThrough]
    public static string Coalesce(this string str, params Func<string>[] strings) {
        return string.IsNullOrEmpty(str)
            ? strings.Select(o => o?.Invoke()).FirstOrDefault(s => !string.IsNullOrEmpty(s))
            : str;
    }

    /// <summary> Take the first string that is not null or white space </summary>
    [DebuggerStepThrough]
    public static string CoalesceWhiteSpace(this string str, params string[] strings) {
        return string.IsNullOrWhiteSpace(str)
            ? (strings?.Length > 0 ? strings.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) : null)
            : str;
    }

    /// <summary> Take the first string that is not null or white space </summary>
    [DebuggerStepThrough]
    public static string CoalesceWhiteSpace(this string str, params Func<string>[] strings) {
        return string.IsNullOrWhiteSpace(str)
            ? (strings?.Length > 0
                ? strings.Select(o => o?.Invoke()).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))
                : null)
            : str;
    }

    /// <summary> will return true if the string is a valid symbol name </summary>
    internal static bool IsValidSymbolName(this string str) {
        if (string.IsNullOrEmpty(str)) return false;
        for (var i = 0; i < str.Length; i++) {
            var c = str[i];
            if (!IsValidInIdentifier(c, i == 0)) return false;
        }

        return true;
    }
    /// <summary> will return true if the string is a valid symbol name </summary>
    internal static bool IsValidDbIdentifier(this string str) {
        if (string.IsNullOrEmpty(str)) return false;
        for (var i = 0; i < str.Length; i++) {
            var c = str[i];
            if (c == ' ') continue;
            if (!IsValidInIdentifier(c, i == 0)) return false;
        }

        return true;
    }

    /// <summary>
    /// Will return _ in place of invalid chars such as spaces.
    /// EF6 standard policy in dealing with spaces is to just convert to underscore.
    /// EF Core will eliminate the character and ensure next valid char is capitalized.
    /// For EF Core usage, use GenerateCandidateIdentifier() instead.
    /// </summary>
    internal static string GenerateLegacyCandidateIdentifier(this string str) {
        if (string.IsNullOrEmpty(str)) return "";
        return new(CleanseSymbolNameChars(str).ToArray());

        static IEnumerable<char> CleanseSymbolNameChars(string str) {
            if (string.IsNullOrEmpty(str)) yield break;
            for (var i = 0; i < str.Length; i++) {
                var c = str[i];
                if (IsValidInIdentifier(c, i == 0))
                    yield return c;
                else
                    yield return '_';
            }
        }
    }

    /// <summary>
    /// Convert DB element name to entity identifier. This is the EF Core standard.
    /// Borrowed from Microsoft.EntityFrameworkCore.Scaffolding.Internal.CandidateNamingService.GenerateCandidateIdentifier()
    /// </summary>
    public static string GenerateCandidateIdentifier(this string originalIdentifier) {
        var candidateStringBuilder = new StringBuilder();
        var previousLetterCharInWordIsLowerCase = false;
        var isFirstCharacterInWord = true;
        foreach (var c in originalIdentifier) {
            var isNotLetterOrDigit = !char.IsLetterOrDigit(c);
            if (isNotLetterOrDigit
                || (previousLetterCharInWordIsLowerCase && char.IsUpper(c))) {
                isFirstCharacterInWord = true;
                previousLetterCharInWordIsLowerCase = false;
                if (isNotLetterOrDigit) continue;
            }

            candidateStringBuilder.Append(isFirstCharacterInWord ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
            isFirstCharacterInWord = false;
            if (char.IsLower(c)) previousLetterCharInWordIsLowerCase = true;
        }

        return candidateStringBuilder.ToString();
    }

    /// <summary> will capitalize the first letter of the given string if it is lower. </summary>
    internal static string CapitalizeFirst(this string str) {
        if (string.IsNullOrEmpty(str)) return "";
        if (char.IsLetter(str[0]) && char.IsLower(str[0])) return char.ToUpper(str[0]) + str[1..];
        return str;
    }

    private static bool IsValidInIdentifier(this char c, bool firstChar = true) {
        switch (char.GetUnicodeCategory(c)) {
            case UnicodeCategory.UppercaseLetter:
            case UnicodeCategory.LowercaseLetter:
            case UnicodeCategory.TitlecaseLetter:
            case UnicodeCategory.ModifierLetter:
            case UnicodeCategory.OtherLetter:
                // Always allowed in C# identifiers
                return true;

            case UnicodeCategory.LetterNumber:
            case UnicodeCategory.NonSpacingMark:
            case UnicodeCategory.SpacingCombiningMark:
            case UnicodeCategory.DecimalDigitNumber:
            case UnicodeCategory.ConnectorPunctuation:
            case UnicodeCategory.Format:
                // Only allowed after first char
                return c.In('_') || !firstChar;
            default:
                return false;
        }
    }

    public static (string namespaceName, string name) SplitNamespaceAndName(this string fullName) {
        if (fullName.IsNullOrEmpty()) return default;

        var parts = fullName.Split('.');
        Debug.Assert(parts.Length > 0);
        if (parts.Length <= 1) return ("", parts[0]);

        var typeName = parts[^1];
        return (parts.Take(parts.Length - 1).Join("."), typeName);
    }

    internal static char GetSwitchArgChar(this string arg) {
        arg = GetSwitchArg(arg);
        return arg?[0] ?? default;
    }

    internal static string GetSwitchArg(this string arg) {
        if (arg.IsNullOrWhiteSpace() || arg.Length < 2) return default;
        var firstChar = arg[0];
        return firstChar is '-' or '/' ? arg[1..].ToLower() : default;
    }
}