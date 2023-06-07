using EntityFrameworkRuler.Design.Metadata;
using EntityFrameworkRuler.Design.Metadata.Builders;
using EntityFrameworkRuler.Design.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkRuler.Design.Scaffolding.Internal;

public static class FunctionHelper {
    // private void GenerateProcedure(Function function, DatabaseFunction dbFunction, DatabaseModelEx model, bool signatureOnly, bool useAsyncCalls) {
    //     var paramStrings = function.GetParameters().Where(p => !p.IsOutput)
    //         .Select(p => $"{p.StoreType .Reference(p.ClrType(asMethodParameter: true))} {Code.Identifier(p.Name)}")
    //         .ToList();
    //
    //     var allOutParams = dbFunction.Parameters.Where(p => p.Output).ToList();
    //
    //     var outParams = allOutParams.SkipLast(1).ToList();
    //
    //     var retValueName = allOutParams.Last().Name;
    //
    //     var outParamStrings = outParams
    //         .Select(p => $"OutputParameter<{Code.Reference(p.ClrType())}> {Code.Identifier(p.Name)}")
    //         .ToList();
    //
    //     string fullExec = GenerateProcedureStatement(dbFunction, retValueName, useAsyncCalls);
    //
    //     var multiResultId = GenerateMultiResultSyntax(dbFunction, model);
    //
    //     var identifier = GenerateIdentifierName(dbFunction, model);
    //
    //     var returnClass = identifier + "Result";
    //
    //     if (!string.IsNullOrEmpty(dbFunction.MappedType)) {
    //         returnClass = dbFunction.MappedType;
    //     }
    //
    //     var line = GenerateMethodSignature(dbFunction, outParams, paramStrings, retValueName, outParamStrings, identifier, multiResultId, signatureOnly, useAsyncCalls, returnClass);
    //
    //     if (signatureOnly) {
    //         Sb.Append(line);
    //         return;
    //     }
    //
    //     using (Sb.Indent()) {
    //         Sb.AppendLine();
    //
    //         Sb.AppendLine(line);
    //         Sb.AppendLine("{");
    //
    //         using (Sb.Indent()) {
    //             foreach (var parameter in allOutParams) {
    //                 GenerateParameterVar(parameter, dbFunction);
    //             }
    //
    //             Sb.AppendLine();
    //
    //             Sb.AppendLine("var sqlParameters = new []");
    //             Sb.AppendLine("{");
    //             using (Sb.Indent()) {
    //                 foreach (var parameter in dbFunction.Parameters) {
    //                     if (parameter.Output) {
    //                         Sb.Append($"{ParameterPrefix}{parameter.Name}");
    //                     } else {
    //                         GenerateParameter(parameter, dbFunction);
    //                     }
    //
    //                     Sb.AppendLine(",");
    //                 }
    //             }
    //
    //             Sb.AppendLine("};");
    //
    //             if (dbFunction.HasValidResultSet && (dbFunction.Results.Count == 0 || dbFunction.Results[0].Count == 0)) {
    //                 Sb.AppendLine(useAsyncCalls
    //                     ? $"var _ = await _context.Database.ExecuteSqlRawAsync({fullExec});"
    //                     : $"var _ = _context.Database.ExecuteSqlRaw({fullExec});");
    //             } else {
    //                 if (dbFunction.SupportsMultipleResultSet) {
    //                     Sb.AppendLine();
    //                     Sb.AppendLine("var dynamic = CreateDynamic(sqlParameters);");
    //                     Sb.AppendLine($"{multiResultId}  _;");
    //                     Sb.AppendLine();
    //                     Sb.AppendLine(
    //                         $"using (var reader = {(useAsyncCalls ? "await GetMultiReaderAsync" : "GetMultiReader")}(_context, dynamic, \"[{dbFunction.Schema}].[{dbFunction.Name}]\"))");
    //                     Sb.AppendLine("{");
    //
    //                     using (Sb.Indent()) {
    //                         var statements = GenerateMultiResultStatement(dbFunction, model, useAsyncCalls);
    //                         Sb.AppendLine($"_ = {statements};");
    //                     }
    //
    //                     Sb.AppendLine("}");
    //                 } else {
    //                     Sb.AppendLine(useAsyncCalls
    //                         ? $"var _ = await _context.SqlQueryAsync<{returnClass}>({fullExec});"
    //                         : $"var _ = _context.SqlQuery<{returnClass}>({fullExec});");
    //                 }
    //             }
    //
    //             Sb.AppendLine();
    //
    //             foreach (var parameter in outParams) {
    //                 Sb.AppendLine($"{Code.Identifier(parameter.Name)}.SetValue({ParameterPrefix}{parameter.Name}.Value);");
    //             }
    //
    //             if (dbFunction.SupportsMultipleResultSet) {
    //                 Sb.AppendLine($"{retValueName}?.SetValue(dynamic.Get<int>(\"{retValueName}\"));");
    //             } else {
    //                 Sb.AppendLine($"{retValueName}?.SetValue({ParameterPrefix}{retValueName}.Value);");
    //             }
    //
    //             Sb.AppendLine();
    //
    //             Sb.AppendLine("return _;");
    //         }
    //
    //         Sb.AppendLine("}");
    //     }
    // }
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
                var returnClass = function.Name + "Result";
                if (!string.IsNullOrEmpty(function.MappedType)) returnClass = function.MappedType;
                returnType = $"List<{returnClass}>";
            }
        }

        return returnType;
    }

    internal static string GenerateMultiResultSyntax(this DatabaseFunction dbFunction, Function function) {
        if (dbFunction.Results.Count == 1) return null;

        var ids = new List<string>();
        var i = 1;
        foreach (var _ in dbFunction.Results) {
            var suffix = $"{i++}";

            var resultTypeName = function.Name + "Result" + suffix;
            ids.Add($"List<{resultTypeName}> Result{suffix}");
        }

        return $"({string.Join(", ", ids)})";
    }

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