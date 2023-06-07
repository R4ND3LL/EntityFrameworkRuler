using EntityFrameworkRuler.Design.Metadata;
using EntityFrameworkRuler.Design.Metadata.Builders;
using EntityFrameworkRuler.Design.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkRuler.Design.Scaffolding.Internal;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public static class FunctionHelper { 
    
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public static string GetReturnType(this DatabaseFunction dbFunction,
        Function function,
        string multiResultSyntax) {
        string returnType;
        if (dbFunction.HasValidResultSet && (dbFunction.Results.Count == 0 || dbFunction.Results[0].Count == 0)) {
            returnType = "int";
        } else {
            if (dbFunction.SupportsMultipleResultSet) {
                returnType = multiResultSyntax;
            } else {
                var returnClass = function.Name + "Results";
                if (!string.IsNullOrEmpty(function.MappedType)) returnClass = function.MappedType;
                returnType = $"List<{returnClass}>";
            }
        }

        return returnType;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    internal static string GenerateMultiResultSyntax(this DatabaseFunction dbFunction, Function function) {
        if (dbFunction.Results.Count == 1) return null;

        var ids = new List<string>();
        var i = 1;
        foreach (var entity in function.ResultEntities) {
            var suffix = $"{i++}";
            ids.Add($"List<{entity.Name}> Results{suffix}");
        }

        return  $"({ids.Join()})";
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    internal static string GenerateProcedureStatement(this DatabaseFunction procedure, string retValueName, bool useAsyncCalls) {
        var paramList = procedure.Parameters
            .Select(p => p.IsOutput ? $"@{p.Name} OUTPUT" : $"@{p.Name}").ToList();

        paramList.RemoveAt(paramList.Count - 1);

        var fullExec =
            $"\"EXEC @{retValueName} = [{procedure.Schema}].[{procedure.Name}] {string.Join(", ", paramList)}\", sqlParameters{(useAsyncCalls ? ", cancellationToken" : string.Empty)}"
                .Replace(" \"", "\"", StringComparison.OrdinalIgnoreCase);
        return fullExec;
    }
}