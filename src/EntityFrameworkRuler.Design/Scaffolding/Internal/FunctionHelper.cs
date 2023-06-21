using EntityFrameworkRuler.Design.Metadata;
using EntityFrameworkRuler.Design.Metadata.Builders;
using EntityFrameworkRuler.Design.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkRuler.Design.Scaffolding.Internal;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
internal static class FunctionHelper {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    internal static string GenerateMultiResultTupleSyntax(this DatabaseFunction dbFunction, Function function) {
        if (dbFunction.Results.Count <= 1) return null;

        var ids = new List<string>();
        var i = 1;
        foreach (var entity in function.ResultEntities) ids.Add($"List<{entity.Name}> Results{i++}");
        return ids.Count > 0 ? $"({ids.Join()})" : null;
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    internal static string GenerateExecutionStatement(this DatabaseFunction function, string retValueName) {
        if (function.FunctionType == FunctionType.StoredProcedure) {
            var paramList = function.Parameters
                .Select(p => p.IsOutput ? $"@{p.Name} OUTPUT" : $"@{p.Name}").ToList();

            paramList.RemoveAt(paramList.Count - 1);

            return
                $"EXEC @{retValueName} = [{function.Schema}].[{function.Name}] {paramList.Join()}";
        } else {
            // function statement
            var paramList = function.Parameters
                .Select(p => {
                    Debug.Assert(!p.IsOutput);
                    return p.IsOutput ? $"@{p.Name} OUTPUT" : $"@{p.Name}";
                }).ToList();

            if (function.IsScalar) return $"SELECT [{function.Schema}].[{function.Name}] ({string.Join(", ", paramList)}) as returnValue";

            // table valued function
            Debug.Assert(function.IsTableValuedFunction);
            var selectList = function.Results[0].ResultColumns.Select(o => $"[{o.Name}]").Join();
            return $"SELECT {selectList} FROM [{function.Schema}].[{function.Name}] ({string.Join(", ", paramList)})";
        }
    }
}