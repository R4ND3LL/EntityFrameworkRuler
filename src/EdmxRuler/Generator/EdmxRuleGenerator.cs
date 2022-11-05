using Bricelam.EntityFrameworkCore.Design;
using EdmxRuler.Extensions;
using EdmxRuler.Generator.EdmxModel;
using EdmxRuler.RuleModels;
using EdmxRuler.RuleModels.EnumMapping;
using EdmxRuler.RuleModels.PropertyRenaming;
using EdmxRuler.RuleModels.TableColumnRenaming;
using Schema = EdmxRuler.RuleModels.TableColumnRenaming.Schema;

namespace EdmxRuler.Generator;

public sealed class EdmxRuleGenerator {
    public EdmxRuleGenerator(string edmxFilePath) {
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

    public EnumMappingRulesRoot EnumRules => rules.OfType<EnumMappingRulesRoot>().SingleOrDefault();
    public TableAndColumnRulesRoot TableAndColumnRules => rules.OfType<TableAndColumnRulesRoot>().SingleOrDefault();

    public ClassPropertyNamingRulesRoot ClassPropertyNamingRules =>
        rules.OfType<ClassPropertyNamingRulesRoot>().SingleOrDefault();

    /// <summary> The correlated EDMX model that is read from the EDMX file during the TryGenerateRules() call </summary>
    public EdmxParsed EdmxParsed { get; private set; }

    #endregion

    /// <summary> Parse the EDMX and generate rule models.  Monitor the Errors collection for issues. </summary>
    public IReadOnlyCollection<IEdmxRuleModelRoot> TryGenerateRules() {
        var e = errors = new List<string>();
        var r = rules = new List<IEdmxRuleModelRoot>();
        try {
            EdmxParsed ??= EdmxParser.Parse(EdmxFilePath);
        } catch (Exception ex) {
            e.Add($"Error parsing EDMX: {ex.Message}");
            return Array.Empty<IEdmxRuleModelRoot>();
        }


        GenerateAndAdd(GetTableAndColumnRenameRules);
        GenerateAndAdd(GetNavigationRenameRules);
        GenerateAndAdd(GetEnumMappingRules);
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

    public async Task<bool> TrySaveRules(string projectBasePath, RuleFileNameOptions fileNameOptions = null) {
        var dir = new DirectoryInfo(projectBasePath);
        if (!dir.Exists) {
            errors.Add("Output folder does not exist");
            return false;
        }

        fileNameOptions ??= new RuleFileNameOptions();
        await TryWriteRules(() => TableAndColumnRules, fileNameOptions.RenamingFilename);
        await TryWriteRules(() => ClassPropertyNamingRules, fileNameOptions.PropertyFilename);
        await TryWriteRules(() => EnumRules, fileNameOptions.EnumMappingFilename);
        return Errors.Count == 0;

        async Task TryWriteRules<T>(Func<T> ruleGetter, string fileName) where T : class {
            try {
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

    private TableAndColumnRulesRoot GetTableAndColumnRenameRules() {
        var edmx = EdmxParsed;
        if (edmx?.Entities.IsNullOrEmpty() != false) return new TableAndColumnRulesRoot();

        var root = new TableAndColumnRulesRoot();

        foreach (var grp in edmx.Entities.GroupBy(o => o.DbSchema)) {
            if (grp.Key.IsNullOrWhiteSpace()) continue;

            var schemaRule = new Schema();
            root.Schemas.Add(schemaRule);
            schemaRule.SchemaName = grp.Key;
            schemaRule.UseSchemaName = false; // will append schema name to entity name

            foreach (var entity in edmx.Entities.OrderBy(o => o.Name)) {
                // if entity name is different than db, it has to go into output
                var tbl = new TableRenamer();
                var renamed = false;
                tbl.Name = entity.StorageEntity?.Name ?? entity.Name;
                tbl.NewName = entity.ConceptualEntity?.Name ?? tbl.Name;
                if (tbl.Name != tbl.NewName) {
                    tbl.NewName = entity.ConceptualEntity.Name;
                    renamed = true;
                }

                foreach (var property in entity.Properties)
                    // if property name is different than db, it has to go into output
                    if (property.StorageProperty != null &&
                        property.PropertyName != property.StorageProperty.Name) {
                        tbl.Columns ??= new List<ColumnNamer>();
                        tbl.Columns.Add(new ColumnNamer {
                            Name = property.StorageProperty.Name, NewName = property.PropertyName
                        });
                        renamed = true;
                    }

                if (renamed) schemaRule.Tables.Add(tbl);
            }
        }

        return root;
    }

    private ClassPropertyNamingRulesRoot GetNavigationRenameRules() {
        var edmx = EdmxParsed;
        var rule = new ClassPropertyNamingRulesRoot();
        rule.Classes ??= new List<ClassRenamer>();

        if (edmx?.Entities.IsNullOrEmpty() != false) return new ClassPropertyNamingRulesRoot();

        foreach (var entity in edmx.Entities.OrderBy(o => o.Name)) {
            if (rule.Namespace == null) rule.Namespace = entity.Namespace;

            // if entity name is different than db, it has to go into output
            var tbl = new ClassRenamer();
            var renamed = false;
            tbl.Name = entity.StorageEntity?.Name ?? entity.Name;

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
                var prefix = isDependentEnd ? string.Empty : inverseEntity.Name;
                string efCoreName;
                string altName;
                if (isMany) {
                    efCoreName = $"{prefix}{dep.PropertyName}Navigations";
                    altName = pluralizer.Pluralize(navigation.ToRole.Entity.Name);
                } else {
                    efCoreName = $"{prefix}{dep.PropertyName}Navigation";
                    altName = navigation.ToRole.Entity.Name;
                }

                var newName = navigation.PropertyName;
                tbl.Properties ??= new List<PropertyRenamer>();
                tbl.Properties.Add(
                    new PropertyRenamer { Name = efCoreName, AlternateName = altName, NewName = newName });
                renamed = true;
            }

            if (renamed) rule.Classes.Add(tbl);
        }

        return rule;
    }

    private EnumMappingRulesRoot GetEnumMappingRules() {
        var edmx = EdmxParsed;
        var rule = new EnumMappingRulesRoot();
        rule.Classes ??= new List<EnumMappingClass>();

        if (edmx?.Entities.IsNullOrEmpty() != false) return rule;

        foreach (var entity in edmx.Entities.OrderBy(o => o.Name)) {
            if (rule.Namespace == null) rule.Namespace = entity.Namespace;

            // if entity name is different than db, it has to go into output
            var tbl = new EnumMappingClass();
            var renamed = false;
            tbl.Name = entity.StorageEntity?.Name ?? entity.Name;

            foreach (var property in entity.Properties)
                // if property name is different than db, it has to go into output
                if (property?.EnumType != null) {
                    tbl.Properties ??= new List<EnumMappingProperty>();
                    tbl.Properties.Add(new EnumMappingProperty {
                        Name = property.StorageProperty.Name,
                        EnumType = property.EnumType.ExternalTypeName ?? property.EnumType.FullName
                    });
                    renamed = true;
                }

            if (renamed) rule.Classes.Add(tbl);
        }

        return rule;
    }

    #endregion
}