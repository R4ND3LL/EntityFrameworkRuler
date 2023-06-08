using EntityFrameworkRuler.Design.Metadata;
using EntityFrameworkRuler.Design.Metadata.Builders;
using EntityFrameworkRuler.Design.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkRuler.Design.Scaffolding.Internal;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public static class FunctionHelper {
     

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    internal static string GenerateMultiResultTupleSyntax(this DatabaseFunction dbFunction, Function function) {
        if (dbFunction.Results.Count == 1) return null;

        var ids = new List<string>();
        var i = 1;
        foreach (var entity in function.ResultEntities) ids.Add($"List<{entity.Name}> Results{i++}");
        return $"({ids.Join()})";
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    internal static string GenerateExecutionStatement(this DatabaseFunction procedure, string retValueName) {
        if (procedure.FunctionType == FunctionType.StoredProcedure) {
            var paramList = procedure.Parameters
                .Select(p => p.IsOutput ? $"@{p.Name} OUTPUT" : $"@{p.Name}").ToList();

            paramList.RemoveAt(paramList.Count - 1);

            return 
                $"EXEC @{retValueName} = [{procedure.Schema}].[{procedure.Name}] {paramList.Join()}"
                    .Replace(" \"", "\"", StringComparison.OrdinalIgnoreCase);
        } else {
            // function statement
            var paramList = procedure.Parameters
                .Select(p => {
                    Debug.Assert(!p.IsOutput);
                    return p.IsOutput ? $"@{p.Name} OUTPUT" : $"@{p.Name}";
                }).ToList();

            return $"SELECT [{procedure.Schema}].[{procedure.Name}] ({string.Join(", ", paramList)})"
                    .Replace(" \"", "\"", StringComparison.OrdinalIgnoreCase);
        }
    }
}