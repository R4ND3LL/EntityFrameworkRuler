using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using EntityFrameworkRuler.Design.Metadata;
using EntityFrameworkRuler.Design.Scaffolding.Metadata;
using EntityFrameworkRuler.Design.Services.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Design.Internal;

#pragma warning disable CS1591

namespace EntityFrameworkRuler.Design.Services;

public interface IDatabaseModelFactoryEx {
    void AppendToModel(DbConnection connection, DatabaseModelEx model);
}

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class SqlServerDatabaseModelFactoryEx : IDatabaseModelFactoryEx {
    private readonly IServiceProvider serviceProvider;
    private readonly IOperationReporter reporter;

    public SqlServerDatabaseModelFactoryEx(IServiceProvider serviceProvider, IOperationReporter reporter) {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
    }

    public void AppendToModel(DbConnection connection, DatabaseModelEx model) {
#if NET7
        SqlServerTypeExtensions.UseDateOnlyTimeOnly = true;
#else
        SqlServerTypeExtensions.UseDateOnlyTimeOnly = false;
#endif

        GetDbFunctions(connection, model, null);
    }

    private void GetDbFunctions(DbConnection connection, DatabaseModelEx model, IEnumerable<string> sprocNamesToSelect) {
        var functionsToSelect = new HashSet<string>(sprocNamesToSelect?.ToList() ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        var selectedFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        using var command = connection.CreateCommand();
        command.CommandText =
            """
SELECT ROUTINE_SCHEMA,
       ROUTINE_NAME,
       CAST(CASE WHEN (DATA_TYPE != 'TABLE') THEN 1 ELSE 0 END AS bit) AS IS_SCALAR,
       ROUTINE_TYPE
FROM INFORMATION_SCHEMA.ROUTINES
WHERE NULLIF(ROUTINE_NAME, '') IS NOT NULL
  AND OBJECTPROPERTY(OBJECT_ID(QUOTENAME(ROUTINE_SCHEMA) + '.' + QUOTENAME(ROUTINE_NAME)), 'IsMSShipped') = 0
  AND (select major_id
       from sys.extended_properties
       where major_id = object_id(QUOTENAME(ROUTINE_SCHEMA) + '.' + QUOTENAME(ROUTINE_NAME))
         and minor_id = 0
         and class = 1
         and name = N'microsoft_database_tools_support') IS NULL
ORDER BY ROUTINE_NAME
""";

        RoutineFactory procedureFactory = null;
        RoutineFactory functionFactory = null;

        using (var reader = command.ExecuteReader())
            while (reader.Read()) {
                var schema = reader.GetString(0);
                var name = reader.GetString(1);


                if (!AllowsProcedure(functionsToSelect, selectedFunctions, name)) continue;

                reporter!.WriteInformation($"RULED: Found procedure: {name}");

                var isScalar = reader.GetBoolean(2);
                var type = reader.GetString(3);
                var isProcedure = string.Equals(type, "PROCEDURE", StringComparison.OrdinalIgnoreCase);
                var factory = isProcedure
                    ? procedureFactory ??= (RoutineFactory)serviceProvider.GetConcrete<SqlServerProcedureFactory>()
                    : functionFactory ??= serviceProvider.GetConcrete<SqlServerFunctionFactory>();
                var function = factory.Create(isScalar);

                function.Schema = schema;
                function.Name = name;

                model.Functions.Add(function);
            }

        var allParameters = model.Functions.Count > 0 ? GetParameters(connection) : null;

        foreach (var function in model.Functions.ToArray()) {
            var isProcedure = function.FunctionType == FunctionType.StoredProcedure;
            var factory = isProcedure ? procedureFactory : functionFactory;
            var key = $"{function.Schema}.{function.Name}";
            if (allParameters.TryGetValue(key, out var moduleParameters)) function.Parameters = moduleParameters;

            if (function.FunctionType == FunctionType.Function) {
                if (function.Parameters.Count > 0 && function.Parameters[0].IsOutput && function.Parameters[0].Name.IsNullOrWhiteSpace()) {
                    // this "parameter" actually represents the return value. it's not a parameter at all.
                    var scalarReturnParam = function.Parameters[0];
                    function.Parameters.RemoveAt(0);

                    var table = new DatabaseFunctionResultTable() { Function = function, Schema = function.Schema, Comment = $"{function.Name} result set" };

                    table.Columns.Add(new DatabaseFunctionResultColumn() {
                        Name = scalarReturnParam.Name,
                        Nullable = scalarReturnParam.IsNullable,
                        Ordinal = 0,
                        StoreType = scalarReturnParam.StoreType,
                        Precision = scalarReturnParam.Precision,
                        Scale = scalarReturnParam.Scale,
                        Collation = scalarReturnParam.Collation,
                        Comment = $"{function.Name} return value",
                        Table = table
                    });
                    function.Results.Add(table);
                }
            } else if (isProcedure) function.Parameters.Add(GetReturnParameter());

            if (!function.IsScalar)
                try {
                    function.Results.AddRange(factory.GetResultElementLists(connection, function, true));
                    function.HasAcquiredResultSchema = true;
                } catch (Exception ex) {
                    function.HasAcquiredResultSchema = false;
                    function.Results.Clear();
                    function.Results.Add(new() { Function = function, Schema = function.Schema, Comment = $"{function.Name} result set stub" });
                    reporter.WriteError($"RULED: Unable to get result schema for {function.FunctionType} '{function.Schema}.{function.Name}'. {ex.Message}.");
                }

            var failure = false;
            if (function.FunctionType == FunctionType.Function
                && function.IsScalar
                && function.Parameters.Count > 0
                && function.Parameters.Any(p => p.StoreType == "table type")) {
                reporter.WriteError($"RULED: Unable to scaffold {function.FunctionType} '{function.Schema}.{function.Name}' as it has TVP parameters.");
                failure = true;
            }

            if (!failure)
                foreach (var resultElement in function.Results) {
                    var duplicates = resultElement.Columns.GroupBy(x => x.Name)
                        .Where(g => g.Count() > 1)
                        .Select(y => y.Key)
                        .ToList();

                    if (duplicates.Any()) {
                        reporter.WriteError(
                            $"RULED: Unable to scaffold {function.FunctionType} '{function.Schema}.{function.Name}' as it has duplicate result column names: '{duplicates[0]}'.");
                        failure = true;
                    }
                }

            if (!failure && (function.UnnamedColumnCount > 1 || (function.UnnamedColumnCount > 0 && function.Results.Sum(o => o.Columns.Count) > 1))) {
                reporter.WriteError($"RULED: {function.FunctionType} '{function.Schema}.{function.Name}' has {function.UnnamedColumnCount} un-named columns.");
                failure = true;
            }

            if (failure) model.Functions.Remove(function);
        }

        foreach (var table in functionsToSelect.Except(selectedFunctions, StringComparer.OrdinalIgnoreCase)) reporter.WriteInformation($"RULED: Procedure not found: {table}");
    }

    private static bool AllowsProcedure(HashSet<string> tables, HashSet<string> selectedTables, string name) {
        if (tables.Count == 0) return true;

        if (tables.Contains(name)) {
            selectedTables.Add(name);
            return true;
        }

        return false;
    }

    private static Dictionary<string, List<DatabaseFunctionParameter>> GetParameters(DbConnection connection) {
        using var dtResult = new DataTable();
        var result = new List<DatabaseFunctionParameter>();

        // Validate this - based on https://stackoverflow.com/questions/20115881/how-to-get-stored-procedure-parameters-details/41330791
        using var command = connection.CreateCommand();
        command.CommandText =
            """
                SELECT
                    'Parameter' = p.name,
                    'Type'   = COALESCE(type_name(p.system_type_id), type_name(p.user_type_id)),
                    'Length'   = CAST(p.max_length AS INT),
                    'Precision'   = CAST(case when type_name(p.system_type_id) = 'uniqueidentifier'
                                then p.precision
                                else OdbcPrec(p.system_type_id, p.max_length, p.precision) end AS INT),
                    'Scale'   = CAST(OdbcScale(p.system_type_id, p.scale) AS INT),
                    'Order'  = CAST(parameter_id AS INT),
                    'output' = p.is_output,
                    'TypeName' = QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(TYPE_NAME(p.user_type_id)),
                	'TypeSchema' = t.schema_id,
                	'TypeId' = p.user_type_id,
                    'FunctionName' = OBJECT_NAME(p.object_id),
                    'FunctionSchema' = OBJECT_SCHEMA_NAME(p.object_id),
                    'Collation'   = convert(sysname, case when p.system_type_id in (35, 99, 167, 175, 231, 239) then ServerProperty('collation') end)
                    from sys.parameters p
                	LEFT JOIN sys.table_types t ON t.user_type_id = p.user_type_id
                    ORDER BY p.object_id, p.parameter_id;
                """;

        using var reader = command.ExecuteReader();
        Dictionary<string, List<DatabaseFunctionParameter>> allParameters = null;
        while (reader.Read()) {
            var parameterName = GetStringSafe(0);
            //if (parameterName.IsNullOrWhiteSpace()) continue;
            if (parameterName!.StartsWith("@", StringComparison.Ordinal)) parameterName = parameterName[1..];

            var parameter = new DatabaseFunctionParameter() {
                Name = parameterName,
                StoreType = GetStringSafe(1),
                Length = GetInt32Safe(2),
                Precision = GetInt32Safe(3),
                Scale = GetInt32Safe(4),
                Ordinal = GetInt32Safe(5) ?? 0,
                IsOutput = reader.GetBoolean(6),
                TypeName = GetStringSafe(7),
                TypeSchema = GetInt32Safe(8),
                TypeId = GetInt32Safe(9),
                FunctionName = GetStringSafe(10),
                FunctionSchema = GetStringSafe(11),
                Collation = GetStringSafe(12),
                IsNullable = true,
            };

            result.Add(parameter);
        }


        return result.GroupBy(x => $"{x.FunctionSchema}.{x.FunctionName}").ToDictionary(g => g.Key, g => g.ToList(), StringComparer.InvariantCulture);

        int? GetInt32Safe(int i) {
            return reader.IsDBNull(i) ? null : reader.GetInt32(i);
        }

        string GetStringSafe(int i) {
            return reader.IsDBNull(i) ? null : reader.GetString(i);
        }
    }

    private static DatabaseFunctionParameter GetReturnParameter() {
        // Add parameter to hold the standard return value
        return new() {
            Name = "ReturnValue",
            StoreType = "int",
            IsOutput = true,
            IsNullable = false,
            IsReturnValue = true,
        };
    }
}

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
internal class SqlServerFunctionFactory : RoutineFactory {
    private readonly IOperationReporter reporter;

    public SqlServerFunctionFactory(IOperationReporter reporter) {
        this.reporter = reporter;
    }

    public override DatabaseFunction Create(bool isScalar) {
        return new DatabaseFunction { IsScalar = isScalar, FunctionType = FunctionType.Function };
    }

    public override List<DatabaseFunctionResultTable> GetResultElementLists(DbConnection connection, DatabaseFunction dbFunction, bool multipleResults) {
        if (dbFunction is null) throw new ArgumentNullException(nameof(dbFunction));

        var table = new DatabaseFunctionResultTable() { Function = dbFunction, Schema = dbFunction.Schema };

        using var command = connection.CreateCommand();
        command.CommandText = $@"
SELECT
    c.name,
    COALESCE(type_name(c.system_type_id), type_name(c.user_type_id)) AS type_name,
    c.column_id AS column_ordinal,
    c.is_nullable
FROM sys.columns c
WHERE object_id = OBJECT_ID('{dbFunction.Schema}.{dbFunction.Name}');";

        using var reader = command.ExecuteReader();

        while (reader.Read()) {
            var parameter = new DatabaseFunctionResultColumn() {
                Name = reader.GetString(0) is { } s && !string.IsNullOrEmpty(s) ? s : $"Col{table.Columns.Count + 1}",
                StoreType = reader.GetString(1),
                Ordinal = reader.GetInt32(2),
                Nullable = reader.GetBoolean(3),
                Table = table
            };

            table.Columns.Add(parameter);
            parameter.Table = table;
        }

        return new() { table };
    }
}

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
internal class SqlServerProcedureFactory : RoutineFactory {
    private readonly IOperationReporter reporter;

    public SqlServerProcedureFactory(IOperationReporter reporter) {
        this.reporter = reporter;
    }

    public override DatabaseFunction Create(bool isScalar) {
        return new() { FunctionType = FunctionType.StoredProcedure };
    }

    public override List<DatabaseFunctionResultTable> GetResultElementLists(DbConnection connection, DatabaseFunction dbFunction, bool multipleResults) {
        if (connection is null) throw new ArgumentNullException(nameof(connection));

        if (dbFunction is null) throw new ArgumentNullException(nameof(dbFunction));

        var list = GetAllResultSets(connection, dbFunction, !multipleResults);
        if (list.IsNullOrEmpty() && connection is SqlConnection sc) {
            list = GetFirstResultSet(sc, dbFunction);
        }
        return list;
    }

    private static List<DatabaseFunctionResultTable> GetAllResultSets(DbConnection connection, DatabaseFunction dbFunction, bool singleResult) {
        var result = new List<DatabaseFunctionResultTable>();
        using var sqlCommand = connection.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        sqlCommand.CommandText = $"[{dbFunction.Schema}].[{dbFunction.Name}]";
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
        sqlCommand.CommandType = CommandType.StoredProcedure;

        var parameters = dbFunction.Parameters.Take(dbFunction.Parameters.Count - 1);

        foreach (var parameter in parameters) {
            var param = new SqlParameter("@" + parameter.Name, DBNull.Value);

            if (parameter.ClrType() == typeof(DataTable)) {
                param.Value = GetDataTableFromSchema(parameter, connection);
                param.SqlDbType = SqlDbType.Structured;
            }

            if (parameter.ClrType() == typeof(byte[])) param.SqlDbType = SqlDbType.VarBinary;

            sqlCommand.Parameters.Add(param);
        }

        using var schemaReader = sqlCommand.ExecuteReader(CommandBehavior.SchemaOnly);
        do {
            // https://docs.microsoft.com/en-us/dotnet/api/system.data.datatablereader.getschematable
            var schemaTable = schemaReader.GetSchemaTable();
            var table = new DatabaseFunctionResultTable() { Function = dbFunction, Schema = dbFunction.Schema };

            if (schemaTable == null) break;

            int unnamedColumnCount = 0;

            foreach (DataRow row in schemaTable.Rows)
                if (row != null) {
                    var name = (row[0]?.ToString()).EmptyIfNullOrWhitespace();
                    if (name.IsNullOrWhiteSpace()) {
                        unnamedColumnCount++;
                        //continue;
                    }

                    var storeType = row["DataTypeName"].ToString();

                    if (row["ProviderSpecificDataType"]?.ToString()?.StartsWith("Microsoft.SqlServer.Types.Sql", StringComparison.OrdinalIgnoreCase) ?? false) {
#pragma warning disable CA1308
                        // Normalize strings to uppercase
                        storeType = row["ProviderSpecificDataType"].ToString()?.Replace("Microsoft.SqlServer.Types.Sql", string.Empty, StringComparison.OrdinalIgnoreCase)
                            .ToLowerInvariant();
#pragma warning restore CA1308
                        // Normalize strings to uppercase
                    }

                    table.Columns.Add(new DatabaseFunctionResultColumn() {
                        Name = name,
                        Nullable = (bool?)row["AllowDBNull"] ?? true,
                        Ordinal = (int)row["ColumnOrdinal"],
                        StoreType = storeType,
                        Precision = (short?)row["NumericPrecision"],
                        Scale = (short?)row["NumericScale"],
                        Table = table
                    });
                }

            // Fail if the result set contains multiple un-named columns
            if (schemaTable.Rows.Count > 1 && schemaTable.Rows.Count == unnamedColumnCount) throw new InvalidOperationException($"Only un-named result columns in procedure");

            if (unnamedColumnCount > 0) dbFunction.UnnamedColumnCount += unnamedColumnCount;

            result.Add(table);
        } while (schemaReader.NextResult() && !singleResult);

        return result;
    }

    private static List<DatabaseFunctionResultTable> GetFirstResultSet(SqlConnection connection, DatabaseFunction dbFunction) {
        var result = new List<DatabaseFunctionResultTable>();
        using var dtResult = new DataTable();

        var sql = $"exec dbo.sp_describe_first_result_set N'[{dbFunction.Schema}].[{dbFunction.Name}]';";

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        using var adapter = new SqlDataAdapter {
            SelectCommand = new SqlCommand(sql, connection),
        };
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        adapter.Fill(dtResult);
        int unnamedColumnCount = 0;
        var table = new DatabaseFunctionResultTable() { Function = dbFunction, Schema = dbFunction.Schema };
        result.Add(table);

        foreach (DataRow row in dtResult.Rows) {
            if (row != null) {
                var name = row["name"].ToString();
                if (string.IsNullOrEmpty(name)) {
                    unnamedColumnCount++;
                    continue;
                }

                table.Columns.Add(new DatabaseFunctionResultColumn() {
                    Name = name,
                    Nullable = (bool)row["is_nullable"],
                    Ordinal = int.Parse(row["column_ordinal"].ToString()!, CultureInfo.InvariantCulture),
                    StoreType = string.IsNullOrEmpty(row["system_type_name"].ToString()) ? row["user_type_name"].ToString() : row["system_type_name"].ToString(),
                    //Precision = (short?)row["NumericPrecision"],
                    //Scale = (short?)row["NumericScale"],
                    Table = table
                });
            }
        }

        // If the result set only contains un-named columns
        if (dtResult.Rows.Count == unnamedColumnCount) {
            throw new InvalidOperationException($"Only un-named result columns in procedure");
        }

        if (unnamedColumnCount > 0) {
            dbFunction.UnnamedColumnCount += unnamedColumnCount;
        }

        return result;
    }


    private static DataTable GetDataTableFromSchema(DatabaseFunctionParameter parameter, DbConnection connection) {
        var userType = new SqlParameter {
            Value = parameter.TypeId,
            ParameterName = "@userTypeId",
        };

        var userSchema = new SqlParameter {
            Value = parameter.TypeSchema,
            ParameterName = "@schemaId",
        };

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT SC.name, ST.name AS datatype FROM sys.columns SC INNER JOIN sys.types ST ON ST.system_type_id = SC.system_type_id AND ST.is_user_defined = 0
            WHERE ST.name <> 'sysname' AND SC.object_id =
            (SELECT type_table_object_id FROM sys.table_types WHERE schema_id = @schemaId AND user_type_id =  @userTypeId);
            """;

        var dataTable = new DataTable();
        command.Parameters.Add(userType);
        command.Parameters.Add(userSchema);
        using var reader = command.ExecuteReader();
        while (reader.Read()) {
            var columnName = reader.GetString(0);
            var clrType = SqlServerTypeExtensions.GetClrType(reader.GetString(1), false);
            dataTable.Columns.Add(columnName, clrType);
        }

        return dataTable;
    }
}

internal abstract class RoutineFactory {
    public abstract DatabaseFunction Create(bool isScalar);
    public abstract List<DatabaseFunctionResultTable> GetResultElementLists(DbConnection connection, DatabaseFunction dbFunction, bool multipleResults);
}