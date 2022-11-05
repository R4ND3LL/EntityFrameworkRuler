using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Globalization;
using EdmxRuler.Extensions;
using EdmxRuler.RuleModels;
using EdmxRuler.RuleModels.EnumMapping;
using EdmxRuler.RuleModels.PropertyRenaming;
using EdmxRuler.RuleModels.TableColumnRenaming;
using Microsoft.CodeAnalysis;

[assembly: CLSCompliant(false)]

namespace EdmxRuler.Applicator;

public static class RoslynRuleApplicator {
    /// <summary> Load all rule files from the project base path and apply to the enclosed project. </summary>
    /// <param name="projectBasePath"> project folder. </param>
    /// <param name="fileNameOptions"> optional rule file naming options </param>
    /// <returns> list of errors. </returns>
    public static async Task<List<string>> ApplyRulesToPath(string projectBasePath,
        RuleFileNameOptions fileNameOptions = null) {
        var errors = new List<string>();
        try {
            if (projectBasePath == null || !Directory.Exists(projectBasePath))
                throw new ArgumentException(nameof(projectBasePath));

            var fullProjectPath = Directory.GetFiles(projectBasePath, "*.csproj").FirstOrDefault();
            if (fullProjectPath == null) throw new ArgumentException("csproj not found", nameof(projectBasePath));

            fileNameOptions ??= new RuleFileNameOptions();

            var jsonFiles = new[] {
                    fileNameOptions.RenamingFilename, fileNameOptions.PropertyFilename,
                    fileNameOptions.EnumMappingFilename
                }
                .Where(o => o.HasNonWhiteSpace())
                .Select(o => o.Trim())
                .ToArray();


            if (jsonFiles.Length == 0) return errors; // nothing to do

            foreach (var jsonFile in jsonFiles)
                try {
                    var fullPath = Path.Combine(projectBasePath, jsonFile);
                    var fileInfo = new FileInfo(fullPath);
                    if (!fileInfo.Exists) continue;

                    if (jsonFile == fileNameOptions.RenamingFilename) {
                        if (await TryReadRules<TableAndColumnRulesRoot>(fileInfo, errors) is { } schemas)
                            errors.AddRange(await ApplyTableAndColumnRenamingRules(schemas, fullProjectPath));
                    } else if (jsonFile == fileNameOptions.PropertyFilename) {
                        if (await TryReadRules<ClassPropertyNamingRulesRoot>(fileInfo, errors) is
                            { } propertyRenamingRoot)
                            errors.AddRange(await ApplyClassPropertyNamingRules(propertyRenamingRoot, fullProjectPath));
                    } else if (jsonFile == fileNameOptions.EnumMappingFilename) {
                        if (await TryReadRules<EnumMappingRulesRoot>(fileInfo, errors) is { } enumMappingRoot)
                            errors.AddRange(await ApplyEnumMappingRules(enumMappingRoot, fullProjectPath));
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                    errors.Add($"Error processing {jsonFile}: {ex.Message}");
                    throw;
                }

            return errors;
        } catch (Exception ex) {
            errors.Add($"Error: {ex.Message}");
            throw;
        }
    }

    private static async Task<T> TryReadRules<T>(FileInfo jsonFile, List<string> status) where T : class, new() {
        var rules = await jsonFile.FullName.TryReadJsonFile<T>();
        if (rules == null) {
            status.Add($"Unable to open {jsonFile.Name}");
            return null;
        }

        return rules;
    }


    private static async Task<List<string>> ApplyClassPropertyNamingRules(
        ClassPropertyNamingRulesRoot classPropertyNamingRulesRoot,
        string fullProjectPath,
        string contextFolder = "",
        string modelsFolder = "") {
        var status = new List<string>();
        if (classPropertyNamingRulesRoot.Classes == null ||
            classPropertyNamingRulesRoot.Classes.Count == 0) return status; // nothing to do

        var project = await TryLoadProjectOrFallback(fullProjectPath, contextFolder, modelsFolder, status);
        if (project == null || !project.Documents.Any()) return status;

        var renameCount = 0;
        foreach (var classRename in classPropertyNamingRulesRoot.Classes)
        foreach (var refRename in classRename.Properties) {
            var fromNames = new[] { refRename.Name, refRename.AlternateName }
                .Where(o => !string.IsNullOrEmpty(o)).Distinct().ToArray();
            if (fromNames.Length == 0) continue;

            Document docWithRename = null;
            foreach (var fromName in fromNames) {
                docWithRename = await project.Documents.RenamePropertyAsync(
                    classRename.Name,
                    fromName,
                    refRename.NewName);
                if (docWithRename != null) break;
            }

            if (docWithRename != null) {
                // documents have been mutated. update reference to workspace:
                project = docWithRename.Project;
                renameCount++;
                status.Add(
                    $"Renamed class {classRename.Name} property {fromNames[0]} -> {refRename.NewName}");
            } else
                status.Add(
                    $"Could not find table {classRename.Name} property {string.Join(", ", fromNames)}");
        }

        if (renameCount == 0) {
            status.Add("No properties renamed");
            return status;
        }

        var saved = await project.Documents.SaveDocumentsAsync();
        Debug.Assert(saved > 0, "No documents saved");
        status.Add($"{renameCount} properties renamed across {saved} files");
        return status;
    }

    private static async Task<List<string>> ApplyEnumMappingRules(
        EnumMappingRulesRoot enumMappingRulesRoot,
        string fullProjectPath,
        string contextFolder = "",
        string modelsFolder = "") {
        var status = new List<string>();
        if (enumMappingRulesRoot.Classes == null || enumMappingRulesRoot.Classes.Count == 0)
            return status; // nothing to do

        var project = await TryLoadProjectOrFallback(fullProjectPath, contextFolder, modelsFolder, status);
        if (project == null || !project.Documents.Any()) return status;

        var renameCount = 0;
        foreach (var classRename in enumMappingRulesRoot.Classes)
        foreach (var refRename in classRename.Properties) {
            var fromName = refRename.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fromName)) continue;

            var docWithRename = await project.Documents.ChangePropertyTypeAsync(
                classRename.Name,
                fromName,
                refRename.EnumType);

            if (docWithRename != null) {
                // documents have been mutated. update reference to workspace:
                project = docWithRename.Project;
                renameCount++;
                status.Add(
                    $"Update class {classRename.Name} property {fromName} type to {refRename.EnumType}");
            } else
                status.Add($"Could not find table {classRename.Name} property {fromName}");
        }

        if (renameCount == 0) {
            status.Add("No properties types updated");
            return status;
        }

        var saved = await project.Documents.SaveDocumentsAsync();
        Debug.Assert(saved > 0, "No documents saved");
        status.Add($"{renameCount} properties mapped to enums across {saved} files");
        return status;
    }

    private static async Task<List<string>> ApplyTableAndColumnRenamingRules(
        TableAndColumnRulesRoot tableAndColumnRulesRoot, string fullProjectPath, string contextFolder = "",
        string modelsFolder = "") {
        // map to class renaming 
        var rules = tableAndColumnRulesRoot.ToPropertyRules();
        return await ApplyClassPropertyNamingRules(rules, fullProjectPath, contextFolder, modelsFolder);
    }


    private static async Task<Project> TryLoadProjectOrFallback(string fullProjectPath, string contextFolder,
        string modelsFolder, List<string> status) {
        try {
            var dir = Path.GetDirectoryName(fullProjectPath);
            var project = await RoslynExtensions.LoadExistingProjectAsync(fullProjectPath);
            if (project?.Documents.Any() == true) return project;

            project = TryLoadFallbackAdhocProject(dir, contextFolder, modelsFolder, status);
            return project;
        } catch (Exception ex) {
            status.Add($"Error loading project: {ex.Message}");
            return null;
        }
    }

    private static Project TryLoadFallbackAdhocProject(string projectBasePath, string contextFolder,
        string modelsFolder, List<string> status) {
        /* If existing project file fails to load, we can fallback to creating an adhoc workspace with an in memory project
                  * that contains only the cs files we are concerned with.
                  *
                  * Currently, this will successfully rename the model properties but it does NOT process the references from the Configuration
                  * classes.  This is likely because Roslyn is not resolving information about EntityTypeBuilder<> such that it
                  * can identify the model property references.
                  *
                  * This issue *should* be resolved simply by adding EFC references to the project (as is illustrated below) but
                  * it's still not working.
                  *
                  * To do: resolve paths to EFC assemblies and use those in the refAssemblies list below.
                  */
        var cSharpFolders = new HashSet<string>();
        if (contextFolder?.Length > 0 && Directory.Exists(Path.Combine(projectBasePath, contextFolder)))
            cSharpFolders.Add(Path.Combine(projectBasePath, contextFolder));

        if (modelsFolder?.Length > 0 && Directory.Exists(Path.Combine(projectBasePath, modelsFolder)))
            cSharpFolders.Add(Path.Combine(projectBasePath, modelsFolder));

        if (cSharpFolders.Count == 0)
            // use project base path
            cSharpFolders.Add(projectBasePath);

        var cSharpFiles = cSharpFolders
            .SelectMany(o => Directory.GetFiles(o, "*.cs", SearchOption.AllDirectories))
            .Distinct().ToList();

        if (cSharpFiles.Count == 0) {
            status.Add("No .cs files found");
            return null;
        }

        var refAssemblies = new[] {
            // typeof(IEntityTypeConfiguration<>).Assembly, typeof(SqlServerValueGenerationStrategy).Assembly,
            // typeof(Microsoft.EntityFrameworkCore.SqlServer.Query.Internal.SearchConditionConvertingExpressionVisitor).Assembly,
            // typeof(Microsoft.EntityFrameworkCore.RelationalDbFunctionsExtensions).Assembly,
            typeof(NotMappedAttribute).Assembly
        }.Distinct().ToList();
        var refs = refAssemblies.Select(o => MetadataReference.CreateFromFile(o.Location)).ToList();

        try {
            using var workspace = cSharpFiles.GetWorkspaceForFilePaths(refs);
            var project = workspace.CurrentSolution.Projects.First();
            if (project?.Documents.Any() == true) return project;
        } catch (Exception ex) {
            status.Add($"Unable to get in-memory project from workspace: {ex.Message}");
            return null;
        }

        return null;
    }
}