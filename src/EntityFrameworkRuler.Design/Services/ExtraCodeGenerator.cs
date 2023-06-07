﻿using System.Text;
using EntityFrameworkRuler.Design.Metadata;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace EntityFrameworkRuler.Design.Services;

public class ExtraCodeGenerator: IExtraCodeGenerator {
    private IOperationReporter reporter;
    private readonly IEnumerable<IRuledModelCodeGenerator> ruledModelCodeGenerators;
    private IDesignTimeRuleLoader designTimeRuleLoader;

    public ExtraCodeGenerator(IOperationReporter reporter, IEnumerable<IRuledModelCodeGenerator> ruledModelCodeGenerators, IDesignTimeRuleLoader designTimeRuleLoader) {
        this.reporter = reporter;
        this.ruledModelCodeGenerators = ruledModelCodeGenerators;
        this.designTimeRuleLoader = designTimeRuleLoader;
    }

    public IList<ScaffoldedFile> GenerateCode(ModelEx modelEx, ModelCodeGenerationOptions codeGenerationOptions) {
        var resultingFiles = new List<ScaffoldedFile>(); 
        if (modelEx == null) {
            reporter.WriteInformation("ModelEx not found");
            return resultingFiles;
        }

        foreach (var ruledModelCodeGenerator in ruledModelCodeGenerators)
            try {
                resultingFiles.AddRange(ruledModelCodeGenerator.GenerateModel(modelEx, codeGenerationOptions));
            } catch (Exception ex) {
                reporter.WriteError($"RULED: Code gen failed: " + ex.Message);
            }

        return resultingFiles;
    }

    /// <summary>
    /// Save generated files
    /// </summary>
    /// <param name="resultingFiles"> files generated by ruler </param>
    /// <param name="outputDir"> from arg outputDir </param>
    /// <param name="overwriteFiles"> from arg overwriteFiles </param>
    public IList<string> SaveFiles(IList<ScaffoldedFile> resultingFiles, string outputDir, bool overwriteFiles) {
        var projectDir = designTimeRuleLoader.GetProjectDir();
        outputDir = outputDir != null ? Path.GetFullPath(Path.Combine(projectDir, outputDir)) : projectDir;
        var files = new List<string>();
        foreach (var file in resultingFiles) {
            var path = Path.Combine(outputDir, file.Path);
            File.WriteAllText(path, file.Code, Encoding.UTF8);
            files.Add(path);
        }

        return files;
    }

}
public interface IExtraCodeGenerator {
    IList<ScaffoldedFile> GenerateCode(ModelEx modelEx, ModelCodeGenerationOptions codeGenerationOptions);

    /// <summary>
    /// Save generated files
    /// </summary>
    /// <param name="resultingFiles"> files generated by ruler </param>
    /// <param name="outputDir"> from arg outputDir </param>
    /// <param name="overwriteFiles"> from arg overwriteFiles </param>
    IList<string> SaveFiles(IList<ScaffoldedFile> resultingFiles, string outputDir, bool overwriteFiles);
}