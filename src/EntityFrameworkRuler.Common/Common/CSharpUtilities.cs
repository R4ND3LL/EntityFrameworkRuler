﻿using System.Globalization;
using System.Text.RegularExpressions;

#pragma warning disable CS8632

namespace EntityFrameworkRuler.Common;

/// <summary>
/// EF Core standard for entity element naming.
/// Borrowed from Microsoft.EntityFrameworkCore.Scaffolding.Internal
/// </summary>
public class CSharpUtilities : ICSharpUtilities {
    private static readonly HashSet<string> CSharpKeywords = new() {
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while"
    };

    private static readonly Regex InvalidCharsRegex
        = new(
            @"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]",
            default,
            TimeSpan.FromMilliseconds(1000.0));

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual bool IsCSharpKeyword(string identifier)
        => IsCSharpKeywordStatic(identifier);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static bool IsCSharpKeywordStatic(string identifier)
        => CSharpKeywords.Contains(identifier);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual string GenerateCSharpIdentifier(
        string identifier,
        ICollection<string> existingIdentifiers,
        Func<string, string> singularizePluralizer)
        => GenerateCSharpIdentifier(identifier, existingIdentifiers, singularizePluralizer, Uniquifier);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual string GenerateCSharpIdentifier(
        string identifier,
        ICollection<string> existingIdentifiers,
        Func<string, string> singularizePluralizer,
        Func<string, ICollection<string>, string> uniquifier) {
        var proposedIdentifier =
            identifier.Length > 1 && identifier[0] == '@'
                ? "@" + InvalidCharsRegex.Replace(identifier[1..], "_")
                : InvalidCharsRegex.Replace(identifier, "_");
        if (string.IsNullOrEmpty(proposedIdentifier)) proposedIdentifier = "_";

        var firstChar = proposedIdentifier[0];
        if (!char.IsLetter(firstChar)
            && firstChar != '_'
            && firstChar != '@')
            proposedIdentifier = "_" + proposedIdentifier;
        else if (IsCSharpKeyword(proposedIdentifier)) proposedIdentifier = "_" + proposedIdentifier;

        if (singularizePluralizer != null) proposedIdentifier = singularizePluralizer(proposedIdentifier);

        return uniquifier(proposedIdentifier, existingIdentifiers);
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual string Uniquifier(string proposedIdentifier, ICollection<string> existingIdentifiers) {
        if (existingIdentifiers == null) return proposedIdentifier;

        var finalIdentifier = proposedIdentifier;
        var suffix = 1;
        while (existingIdentifiers.Contains(finalIdentifier)) {
            finalIdentifier = proposedIdentifier + suffix;
            suffix++;
        }

        return finalIdentifier;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static bool IsValidIdentifierStatic(string name, bool checkKeyword = true) {
        if (string.IsNullOrEmpty(name)) return false;

        if (!IsIdentifierStartCharacter(name[0])) return false;

        var nameLength = name.Length;
        for (var i = 1; i < nameLength; i++)
            if (!IsIdentifierPartCharacter(name[i]))
                return false;
        if (checkKeyword && IsCSharpKeywordStatic(name)) return false;
        return true;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public virtual bool IsValidIdentifier(string name) {
        if (string.IsNullOrEmpty(name)) return false;

        if (!IsIdentifierStartCharacter(name[0])) return false;

        var nameLength = name.Length;
        for (var i = 1; i < nameLength; i++)
            if (!IsIdentifierPartCharacter(name[i]))
                return false;
        //if (IsCSharpKeyword(name)) return false;
        return true;
    }

    private static bool IsIdentifierStartCharacter(char ch) {
        if (ch < 'a')
            return ch >= 'A'
                   && (ch <= 'Z'
                       || ch == '_');

        if (ch <= 'z') return true;

        return ch > '\u007F' && IsLetterChar(CharUnicodeInfo.GetUnicodeCategory(ch));
    }

    private static bool IsIdentifierPartCharacter(char ch) {
        if (ch < 'a')
            return ch < 'A'
                ? ch >= '0'
                  && ch <= '9'
                : ch <= 'Z'
                  || ch == '_';

        if (ch <= 'z') return true;

        if (ch <= '\u007F') return false;

        var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
        if (IsLetterChar(cat)) return true;

        switch (cat) {
            case UnicodeCategory.DecimalDigitNumber:
            case UnicodeCategory.ConnectorPunctuation:
            case UnicodeCategory.NonSpacingMark:
            case UnicodeCategory.SpacingCombiningMark:
            case UnicodeCategory.Format:
                return true;
        }

        return false;
    }

    private static bool IsLetterChar(UnicodeCategory cat) {
        switch (cat) {
            case UnicodeCategory.UppercaseLetter:
            case UnicodeCategory.LowercaseLetter:
            case UnicodeCategory.TitlecaseLetter:
            case UnicodeCategory.ModifierLetter:
            case UnicodeCategory.OtherLetter:
            case UnicodeCategory.LetterNumber:
                return true;
        }

        return false;
    }
}

/// <summary>
/// EF Core standard for entity element naming.
/// Borrowed from Microsoft.EntityFrameworkCore.Scaffolding.Internal
/// </summary>
internal interface ICSharpUtilities {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    string GenerateCSharpIdentifier(string identifier, ICollection<string>? existingIdentifiers,
        Func<string, string>? singularizePluralizer);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    string GenerateCSharpIdentifier(string identifier, ICollection<string>? existingIdentifiers,
        Func<string, string>? singularizePluralizer, Func<string, ICollection<string>?, string> uniquifier);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    bool IsCSharpKeyword(string identifier);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    bool IsValidIdentifier(string? name);
}