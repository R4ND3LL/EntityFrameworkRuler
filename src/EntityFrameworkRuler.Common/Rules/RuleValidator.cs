﻿using System.Text.RegularExpressions;
using EntityFrameworkRuler.Common;

// ReSharper disable MemberCanBeInternal

namespace EntityFrameworkRuler.Rules;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public class RuleValidator : IRuleValidator {
    private const string invalidSymbolName = "Invalid symbol name";
    private const string tooLong = "Too long";

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public RuleValidator() { }

    private IValidator dbContextRule;
    private IValidator schemaRule;
    private IValidator entityRule;
    private IValidator propertyRule;
    private IValidator navigationRule;
    private IValidator foreignKeyRule;

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public IEnumerable<EvaluationFailure> Validate(IRuleItem rule, bool withChildren = true) {
        IValidator validator;
        switch (rule) {
            case DbContextRule:
                validator = dbContextRule ??= InitializeDbContextValidator();
                break;
            case SchemaRule:
                validator = schemaRule ??= InitializeSchemaRuleValidator();
                break;
            case EntityRule:
                validator = entityRule ??= InitializeEntityRuleValidator();
                break;
            case PropertyRule:
                validator = propertyRule ??= InitializePropertyRuleValidator();
                break;
            case NavigationRule:
                validator = navigationRule ??= InitializeNavigationRuleValidator();
                break;
            case ForeignKeyRule:
                validator = foreignKeyRule ??= InitializeForeignKeyRuleValidator();
                break;
            default: yield break;
        }

        foreach (var failure in validator.Evaluate(rule)) yield return failure;

        if (!withChildren) yield break;
        foreach (var child in rule.GetChildren()) {
            foreach (var childFailure in Validate(child, true)) {
                yield return childFailure;
            }
        }
    }


    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual Validator<DbContextRule> InitializeDbContextValidator() {
        return new Validator<DbContextRule>()
                .For(o => o.Name)
                .Assert(s => s.IsValidSymbolName(), invalidSymbolName)
                .Assert(s => s.Length < 200, tooLong)
                .For(o => o.Schemas)
                .Assert(o => o.Select(r => r.SchemaName).Where(r => r.HasCharacters()).IsDistinct(), "Schema names should be unique")
                .For(o => o.ForeignKeys)
                .Assert(o => o.Select(r => r.Name).Where(r => r.HasCharacters()).IsDistinct(), "Foreign key names should be unique")
                .For(o => o.Annotations)
                .Assert(o => o.All(r => r.Key.HasCharacters()), "Annotation keys are required")
                .Assert(o => o.Select(r => r.Key).IsDistinct(), "Annotation keys should be unique")
            ;
    }


    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual Validator<SchemaRule> InitializeSchemaRuleValidator() {
        return new Validator<SchemaRule>()
                .For(o => o.SchemaName)
                .Assert(s => s.IsNullOrEmpty() || s.IsValidAsciiString(), invalidSymbolName)
                .Assert(s => s.Length < 200, tooLong)
                .For(o => o.Namespace).Assert(o => o.IsNullOrWhiteSpace() || o.Split('.').All(p => p.IsValidSymbolName()),
                    "Invalid namespace")
                .For(o => o.ColumnRegexPattern).Assert(o => VerifyRegEx(o))
                .For(o => o.TableRegexPattern).Assert(o => VerifyRegEx(o))
                .For(o => o.Entities, rule => $"{rule.SchemaName} entities")
                //.Assert(o => o.Select(r => r.Name).Where(r => r.HasCharacters()).IsDistinct(), "Entity Names should be unique")
                .Assert(o => {
                    var list = o.Select(r => r.EntityName)
                        .Where(r => r.HasCharacters())
                        .GroupBy(r => r)
                        .Where(r => r.Count() > 1)
                        .Select(r => r.Key)
                        .ToList();
                    if (list.IsNullOrEmpty()) return EvaluatorResponse.SuccessResponse();
                    var duplicates = list.Join();
                    return EvaluatorResponse.FailResponse("Entity EntityNames should be unique. Duplicates are " + duplicates);
                })
                .Assert(o => {
                    var list = o.Select(r => r.GetFinalName())
                        .Where(r => r.HasCharacters())
                        .GroupBy(r => r)
                        .Where(r => r.Count() > 1)
                        .Select(r => r.Key)
                        .ToList();
                    if (list.IsNullOrEmpty()) return EvaluatorResponse.SuccessResponse();
                    var duplicates = list.Join();
                    return EvaluatorResponse.FailResponse("Final entity names should be unique. Duplicates are " + duplicates);
                })
                .For(o => o.Annotations, rule => $"{rule.SchemaName} annotations")
                .Assert(o => o.All(r => r.Key.HasCharacters()), "Annotation keys are required")
                .Assert(o => o.Select(r => r.Key).IsDistinct(), "Annotation keys should be unique")
            ;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual Validator<EntityRule> InitializeEntityRuleValidator() {
        return new Validator<EntityRule>()
                .For(o => o.Name)
                .Assert(s => s.HasCharacters(), invalidSymbolName)
                .Assert(s => s.Length < 200, tooLong)
                .For(o => o.NewName).Assert(o => o.IsNullOrWhiteSpace() || (o.IsValidSymbolName() && o.Length < 300), invalidSymbolName)
                .For(o => o.EntityName).Assert(o => o.IsNullOrWhiteSpace() || (o.IsValidSymbolName() && o.Length < 300), invalidSymbolName)
                .For(o => o.Properties, rule => $"{rule.GetFinalName()} Properties")
                .Assert(o => o.Select(r => r.Name).Where(r => r.HasCharacters()).IsDistinct(), "Column Names should be unique")
                .Assert(o => {
                    var list = o.Select(r => r.PropertyName)
                        .Where(r => r.HasCharacters())
                        .GroupBy(r => r)
                        .Where(r => r.Count() > 1)
                        .Select(r => r.Key)
                        .ToList();
                    if (list.IsNullOrEmpty()) return EvaluatorResponse.SuccessResponse();
                    var duplicates = list.Join();
                    return EvaluatorResponse.FailResponse("Column PropertyNames should be unique. Duplicates are " + duplicates);
                })
                //.For(o => o.Navigations)
                // .Assert(o => o.Where(r => r.FkName.HasCharacters())
                //         .Select(r => (r.FkName, r.IsPrincipal)).IsDistinct(),
                //     "FkNames should be unique") // removed to allow for self referencing navigation pairs
                .For(o => ((IEntityRule)o).GetProperties(), rule => $"{rule.GetFinalName()} properties")
                .Assert(o => {
                    var list = o.Select(r => r.GetFinalName())
                        .Where(r => r.HasCharacters())
                        .GroupBy(r => r)
                        .Where(r => r.Count() > 1)
                        .Select(r => r.Key)
                        .ToList();
                    if (list.IsNullOrEmpty()) return EvaluatorResponse.SuccessResponse();
                    var duplicates = list.Join();
                    return EvaluatorResponse.FailResponse("Final property names should be unique. Duplicates are " + duplicates);
                })
                .For(o => o.Annotations)
                .Assert(o => o.All(r => r.Key.HasCharacters()), "Annotation keys are required")
                .Assert(o => o.Select(r => r.Key).IsDistinct(), "Annotation keys should be unique")
            ;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual Validator<PropertyRule> InitializePropertyRuleValidator() {
        return new Validator<PropertyRule>()
                .For(o => o.Name)
                .Assert(s => s.HasCharacters(), invalidSymbolName)
                .Assert(s => s.Length < 200, tooLong)
                .For(o => o.NewName).Assert(o => o.IsNullOrWhiteSpace() || (o.IsValidSymbolName() && o.Length < 300), invalidSymbolName)
                .For(o => o.PropertyName)
                .Assert(o => o.IsNullOrWhiteSpace() || (o.IsValidSymbolName() && o.Length < 300), invalidSymbolName)
                .For(o => o.NewType)
                .Assert(o => o.IsNullOrWhiteSpace() || o.Split('.').All(p => p.IsValidSymbolName()), "Invalid type name")
                .For(o => o.Annotations)
                .Assert(o => o.All(r => r.Key.HasCharacters()), "Annotation keys are required")
                .Assert(o => o.Select(r => r.Key).IsDistinct(), "Annotation keys should be unique")
            ;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual Validator<NavigationRule> InitializeNavigationRuleValidator() {
        return new Validator<NavigationRule>()
                .For(o => o.Name)
                .Assert(s => s.IsNullOrWhiteSpace() || (s.IsValidSymbolName() && s.Length < 300), invalidSymbolName)
                .For(o => o.NewName).Assert(o => o.IsNullOrWhiteSpace() || (o.IsValidSymbolName() && o.Length < 300), invalidSymbolName)
                .For(o => o.ToEntity).Assert(o => o.IsNullOrWhiteSpace() || (o.IsValidSymbolName() && o.Length < 300), invalidSymbolName)
                .For(o => o.FkName).Assert(o => o.IsNullOrWhiteSpace() || (o.IsValidSymbolName() && o.Length < 300), invalidSymbolName)
                .For(o => o.Multiplicity).Assert(o => o.IsNullOrEmpty() || o.ParseMultiplicity() != Multiplicity.Unknown)
                .For(o => o.Annotations)
                .Assert(o => o.All(r => r.Key.HasCharacters()), "Annotation keys are required")
                .Assert(o => o.Select(r => r.Key).IsDistinct(), "Annotation keys should be unique")
            ;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected virtual Validator<ForeignKeyRule> InitializeForeignKeyRuleValidator() {
        return new Validator<ForeignKeyRule>()
                .For(o => o.Name)
                .Assert(s => s.IsNullOrWhiteSpace() || (s.IsValidSymbolName() && s.Length < 300), invalidSymbolName)
                .For(o => o.Name).Assert(o => o.IsNullOrWhiteSpace() || (o.IsValidSymbolName() && o.Length < 300), invalidSymbolName)
                .For(o => o.PrincipalEntity)
                .Assert(o => o.IsNullOrWhiteSpace() || (o.IsValidSymbolName() && o.Length < 300), invalidSymbolName)
                .For(o => o.DependentEntity)
                .Assert(o => o.IsNullOrWhiteSpace() || (o.IsValidSymbolName() && o.Length < 300), invalidSymbolName)
                .For(o => o.PrincipalProperties)
                .Assert(o => o.Where(r => r.HasCharacters()).IsDistinct(), "Principal Properties should be unique")
                .For(o => o.DependentProperties)
                .Assert(o => o.Where(r => r.HasCharacters()).IsDistinct(), "Dependent Properties should be unique")
                .For(o => o.Annotations)
                .Assert(o => o.Count == 0, "Foreign key annotations are not supported")
            ;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    protected static bool VerifyRegEx(string testPattern, bool allowBlank = true) {
        var isValid = true;
        if (testPattern.IsNullOrWhiteSpace()) return allowBlank;
        try {
            _ = Regex.Match("", testPattern);
        } catch (ArgumentException) {
            // BAD PATTERN: Syntax error
            isValid = false;
        }

        return isValid;
    }
}

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public interface IRuleValidator {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    IEnumerable<EvaluationFailure> Validate(IRuleItem rule, bool withChildren = true);
}