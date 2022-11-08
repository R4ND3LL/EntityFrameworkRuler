using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EdmxRuler.Extensions;
using EdmxRuler.Generator.EdmxModel;
using EdmxRuler.RuleModels;
using EdmxRuler.RuleModels.NavigationNaming;
using EdmxRuler.RuleModels.PrimitiveNaming;
using EdmxRuler.RuleModels.PropertyTypeChanging;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal

namespace EdmxRuler.Generator;

/// <summary> Generate rules from an EDMX such that they can be applied to a Reverse Engineered Entity Framework model to achieve the same structure as in the EDMX. </summary>
public sealed class RuleGenerator {
    public RuleGenerator(string edmxFilePath) {
        EdmxFilePath = edmxFilePath;
        pluralizer = new Pluralizer();
    }

    #region properties

    private readonly Pluralizer pluralizer;

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary> The EDMX file path </summary>
    public string EdmxFilePath { get; }

    private List<string> errors;

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary> Any errors that were encountered during generation </summary>
    public IReadOnlyCollection<string> Errors => errors;


    private List<IEdmxRuleModelRoot> rules;

    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary> The rules generated from the EDMX via the TryGenerateRules() call </summary>
    public IReadOnlyCollection<IEdmxRuleModelRoot> Rules => rules;

    public PropertyTypeChangingRules PropertyTypeChangingRules =>
        rules.OfType<PropertyTypeChangingRules>().SingleOrDefault();

    public PrimitiveNamingRules PrimitiveNamingRules => rules.OfType<PrimitiveNamingRules>().SingleOrDefault();
    public NavigationNamingRules NavigationNamingRules => rules.OfType<NavigationNamingRules>().SingleOrDefault();

    /// <summary> The correlated EDMX model that is read from the EDMX file during the TryGenerateRules() call </summary>
    public EdmxParsed EdmxParsed { get; private set; }

    #endregion

    /// <summary> Generate rules from an EDMX such that they can be applied to a Reverse Engineered Entity Framework model to achieve the same structure as in the EDMX.
    /// Errors are monitored and added to local Errors collection. </summary>
    public IReadOnlyCollection<IEdmxRuleModelRoot> TryGenerateRules() {
        var e = errors = new List<string>();
        var r = rules = new List<IEdmxRuleModelRoot>();
        try {
            EdmxParsed ??= EdmxParser.Parse(EdmxFilePath);
        } catch (Exception ex) {
            e.Add($"Error parsing EDMX: {ex.Message}");
            return Array.Empty<IEdmxRuleModelRoot>();
        }


        GenerateAndAdd(GetPrimitiveNamingRules);
        GenerateAndAdd(GetNavigationNamingRules);
        GenerateAndAdd(GetPropertyTypeChangingRules);
        return Rules;

        void GenerateAndAdd<T>(Func<T> gen) where T : class, IEdmxRuleModelRoot {
            try {
                var rulesRoot = gen();
                r.Add(rulesRoot);
            } catch (Exception ex) {
                e.Add($"Error generating output for {typeof(T)}: {ex.Message}");
            }
        }
    }

    /// <summary> Persist the previously generated rules to the given target path. </summary>
    /// <param name="projectBasePath"> The location to save the rule files. </param>
    /// <param name="fileNameOptions"> Custom naming options for the rule files.  Optional. This parameter can be used to skip writing a rule file by setting that rule file to null. </param>
    /// <returns> True if completed with no errors.  When false, see Errors collection for details. </returns>
    /// <exception cref="Exception"></exception>
    public async Task<bool> TrySaveRules(string projectBasePath, RuleFileNameOptions fileNameOptions = null) {
        var dir = new DirectoryInfo(projectBasePath);
        if (!dir.Exists) {
            errors.Add("Output folder does not exist");
            return false;
        }

        fileNameOptions ??= new RuleFileNameOptions();
        await TryWriteRules(() => PrimitiveNamingRules, fileNameOptions.PrimitiveNamingFile);
        await TryWriteRules(() => NavigationNamingRules, fileNameOptions.NavigationNamingFile);
        await TryWriteRules(() => PropertyTypeChangingRules, fileNameOptions.PropertyTypeChangingFile);
        return Errors.Count == 0;

        async Task TryWriteRules<T>(Func<T> ruleGetter, string fileName) where T : class {
            try {
                if (fileName.IsNullOrWhiteSpace()) return; // file skipped by user
                var rulesRoot = ruleGetter() ?? throw new Exception("Rule model null");
                await WriteRules(rulesRoot, fileName);
            } catch (Exception ex) {
                errors.Add($"Error writing rule to file {fileName}: {ex.Message}");
            }
        }

        Task WriteRules<T>(T rulesRoot, string filename)
            where T : class {
            var path = Path.Combine(dir.FullName, filename);
            return rulesRoot.ToJson<T>(path);
        }
    }


    #region Main rule gen methods

    private PrimitiveNamingRules GetPrimitiveNamingRules() {
        var edmx = EdmxParsed;
        if (edmx?.Entities.IsNullOrEmpty() != false) return new PrimitiveNamingRules();

        var root = new PrimitiveNamingRules();

        foreach (var grp in edmx.Entities.GroupBy(o => o.DbSchema)) {
            if (grp.Key.IsNullOrWhiteSpace()) continue;

            var schemaRule = new SchemaReference();
            root.Schemas.Add(schemaRule);
            schemaRule.SchemaName = grp.Key;
            schemaRule.UseSchemaName = false; // will append schema name to entity name

            foreach (var entity in edmx.Entities.OrderBy(o => o.Name)) {
                // if entity name is different than db, it has to go into output
                var tbl = new ClassRename();
                var renamed = false;
                tbl.Name = entity.StorageNameCleansed ?? entity.Name;
                tbl.NewName = entity.ConceptualEntity?.Name ?? tbl.Name;
                if (tbl.Name != tbl.NewName) renamed = true;

                Debug.Assert(tbl.Name.IsValidSymbolName());
                Debug.Assert(tbl.NewName.IsValidSymbolName());

                foreach (var property in entity.Properties) {
                    // if property name is different than db, it has to go into output
                    var storagePropertyName = property.DbColumnNameCleansed;
                    if (storagePropertyName.IsNullOrWhiteSpace() ||
                        property.Name == storagePropertyName) continue;
                    tbl.Columns ??= new List<PropertyRename>();
                    tbl.Columns.Add(new PropertyRename {
                        Name = storagePropertyName, NewName = property.Name
                    });
                    Debug.Assert(tbl.Columns[^1].Name.IsValidSymbolName());
                    Debug.Assert(tbl.Columns[^1].NewName.IsValidSymbolName());
                    renamed = true;
                }

                if (renamed) schemaRule.Tables.Add(tbl);
            }
        }

        return root;
    }

    private NavigationNamingRules GetNavigationNamingRules() {
        var edmx = EdmxParsed;
        var rule = new NavigationNamingRules();
        rule.Classes ??= new List<ClassReference>();

        if (edmx?.Entities.IsNullOrEmpty() != false) return new NavigationNamingRules();

        foreach (var entity in edmx.Entities.OrderBy(o => o.Name)) {
            // omit the namespace. it often does not match the Reverse Engineered value
            rule.Namespace ??= ""; //entity.Namespace;

            // if entity name is different than db, it has to go into output
            var tbl = new ClassReference();
            var renamed = false;
            tbl.Name = entity.Name;

            foreach (var navigation in entity.NavigationProperties) {
                if (navigation.Association == null) continue;

                var ass = navigation.Association;
                var constraint = ass.ReferentialConstraints.FirstOrDefault();
                if (constraint == null) continue;

                var deps = constraint.DependentProperties;
                var dep = deps?.FirstOrDefault();
                if (dep == null) continue;

                var isMany = navigation.Multiplicity == Multiplicity.Many;

                var principalEntity = constraint.PrincipalEntity;
                var dependentEntity = constraint.DependentEntity;
                var isPrincipalEnd = principalEntity.Name == navigation.EntityName;
                var isDependentEnd = dependentEntity.Name == navigation.EntityName;

                var inverseEntity = isPrincipalEnd ? dependentEntity : principalEntity;
                var prefix = isDependentEnd ? string.Empty : inverseEntity.StorageNameCleansed;
                string efCoreName;
                string altName;
                if (isMany) {
                    efCoreName = $"{prefix}{dep.DbColumnNameCleansed}Navigations";
                    altName = pluralizer.Pluralize(navigation.ToRole.Entity.StorageNameCleansed);
                } else {
                    efCoreName = $"{prefix}{dep.DbColumnNameCleansed}Navigation";
                    altName = navigation.ToRole.Entity.StorageNameCleansed;
                }

                var newName = navigation.Name;

                //if (efCoreName == "ProjectFkNavigation") Debugger.Break();

                tbl.Properties ??= new List<NavigationRename>();
                var navigationRename = new NavigationRename { NewName = newName };
                if (efCoreName.HasNonWhiteSpace()) navigationRename.Name.Add(efCoreName);
                if (altName.HasNonWhiteSpace()) navigationRename.Name.Add(altName);
                if (navigationRename.Name.Count > 0) tbl.Properties.Add(navigationRename);
                renamed = true;
            }

            if (renamed) rule.Classes.Add(tbl);
        }

        return rule;
    }

    private PropertyTypeChangingRules GetPropertyTypeChangingRules() {
        var edmx = EdmxParsed;
        var rule = new PropertyTypeChangingRules();
        rule.Classes ??= new List<TypeChangingClass>();

        if (edmx?.Entities.IsNullOrEmpty() != false) return rule;

        foreach (var entity in edmx.Entities.OrderBy(o => o.Name)) {
            // omit the namespace. it often does not match the Reverse Engineered value
            rule.Namespace ??= ""; //entity.Namespace;

            // if entity name is different than db, it has to go into output
            var tbl = new TypeChangingClass();
            var renamed = false;
            tbl.Name = entity.Name;

            foreach (var property in entity.Properties) {
                // if property name is different than db, it has to go into output
                if (property?.EnumType == null) continue;
                tbl.Properties ??= new List<TypeChangingProperty>();
                tbl.Properties.Add(new TypeChangingProperty {
                    Name = property.Name,
                    NewType = property.EnumType.ExternalTypeName ?? property.EnumType.FullName
                });
                renamed = true;
            }

            if (renamed) rule.Classes.Add(tbl);
        }

        return rule;
    }

    #endregion
}