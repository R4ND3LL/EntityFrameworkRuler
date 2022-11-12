using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using EdmxRuler.Common;
using EdmxRuler.RuleModels.NavigationNaming;
using EdmxRuler.RuleModels.PrimitiveNaming;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

// ReSharper disable ClassCanBeSealed.Global

namespace EntityFrameworkRuler.Services;

/// <summary> Naming service override to be used by Ef scaffold process. </summary>
public class EfCandidateNamingService : CandidateNamingService {
    private readonly PrimitiveNamingRules primitiveNamingRules;
    private readonly bool preserveCasingUsingRegex;
    private readonly NavigationNamingRules navigationRules;

    public EfCandidateNamingService(IRuleProvider ruleProvider) {
        if (!Debugger.IsAttached) Debugger.Launch();
        primitiveNamingRules = ruleProvider.GetPrimitiveNamingRules();
        navigationRules = ruleProvider.GetNavigationNamingRules();
        //this.preserveCasingUsingRegex = preserveCasingUsingRegex;
    }

    /// <summary> Name that table </summary>
    public override string GenerateCandidateIdentifier(DatabaseTable originalTable) {
        if (originalTable == null) throw new ArgumentException("Argument is empty", nameof(originalTable));

        var candidateStringBuilder = new StringBuilder();

        var schema = GetSchema(originalTable.Schema);

        if (schema == null) return base.GenerateCandidateIdentifier(originalTable);

        if (schema.UseSchemaName) candidateStringBuilder.Append(GenerateIdentifier(originalTable.Schema));

        var newTableName = string.Empty;

        if (schema.Tables != null && schema.Tables.Any(t => t.Name == originalTable.Name))
            newTableName = schema.Tables.SingleOrDefault(t => t.Name == originalTable.Name)?.NewName;
        else if (!string.IsNullOrEmpty(schema.TableRegexPattern) && schema.TablePatternReplaceWith != null) {
            if (preserveCasingUsingRegex)
                newTableName = RegexNameReplace(schema.TableRegexPattern, originalTable.Name,
                    schema.TablePatternReplaceWith);
            else
                newTableName = GenerateIdentifier(RegexNameReplace(schema.TableRegexPattern, originalTable.Name,
                    schema.TablePatternReplaceWith));
        } else
            newTableName = base.GenerateCandidateIdentifier(originalTable);

        if (string.IsNullOrWhiteSpace(newTableName)) {
            candidateStringBuilder.Append(GenerateIdentifier(originalTable.Name));

            return candidateStringBuilder.ToString();
        }

        candidateStringBuilder.Append(newTableName);

        return candidateStringBuilder.ToString();
    }

    /// <summary> Name that column </summary>
    public override string GenerateCandidateIdentifier(DatabaseColumn originalColumn) {
        return base.GenerateCandidateIdentifier(originalColumn);
    }

    /// <summary> Name that navigation dependent </summary>
    public override string GetDependentEndCandidateNavigationPropertyName(IForeignKey foreignKey) {
        return base.GetDependentEndCandidateNavigationPropertyName(foreignKey);
    }

    /// <summary> Name that navigation principal </summary>
    public override string GetPrincipalEndCandidateNavigationPropertyName(IForeignKey foreignKey,
        string dependentEndNavigationPropertyName) {
        return base.GetPrincipalEndCandidateNavigationPropertyName(foreignKey, dependentEndNavigationPropertyName);
    }

    #region internal members

    private static string GenerateIdentifier(string value) {
        var candidateStringBuilder = new StringBuilder();
        var previousLetterCharInWordIsLowerCase = false;
        var isFirstCharacterInWord = true;

        foreach (var c in value) {
            var isNotLetterOrDigit = !char.IsLetterOrDigit(c);
            if (isNotLetterOrDigit
                || (previousLetterCharInWordIsLowerCase && char.IsUpper(c))) {
                isFirstCharacterInWord = true;
                previousLetterCharInWordIsLowerCase = false;
                if (isNotLetterOrDigit) continue;
            }

            candidateStringBuilder.Append(
                isFirstCharacterInWord ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
            isFirstCharacterInWord = false;
            if (char.IsLower(c)) previousLetterCharInWordIsLowerCase = true;
        }

        return candidateStringBuilder.ToString();
    }

    private static string RegexNameReplace(string pattern, string originalName, string replacement, int timeout = 100) {
        string newName = string.Empty;

        try {
            newName = Regex.Replace(originalName, pattern, replacement, RegexOptions.None,
                TimeSpan.FromMilliseconds(timeout));
        } catch (RegexMatchTimeoutException) {
            Console.WriteLine(
                $"Regex pattern {pattern} time out when trying to match {originalName}, name won't be replaced");
        }

        return newName;
    }

    private SchemaReference GetSchema(string originalSchema)
        => primitiveNamingRules?
            .Schemas
            .FirstOrDefault(x => x.SchemaName == originalSchema);

    #endregion
}