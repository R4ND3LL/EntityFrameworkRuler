using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Castle.DynamicProxy;
using EntityFrameworkRuler.Design.Metadata;
using EntityFrameworkRuler.Design.Scaffolding.Metadata;
using EntityFrameworkRuler.Design.Services.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace EntityFrameworkRuler.Design.Services;

/// <summary> Proxy for the actual IDatabaseModelFactory </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
internal class RuledDatabaseModelFactory : IDatabaseModelFactory {
    private readonly IServiceProvider serviceProvider;
    private readonly IDatabaseModelFactory actualFactory;
    private readonly IOperationReporter reporter;
    private readonly IDesignTimeRuleLoader designTimeRuleLoader;

    public RuledDatabaseModelFactory(IServiceProvider serviceProvider, IDatabaseModelFactory actualFactory, IOperationReporter reporter,
        IDesignTimeRuleLoader designTimeRuleLoader) {
        this.serviceProvider = serviceProvider;
        this.actualFactory = actualFactory;
        this.reporter = reporter;
        this.designTimeRuleLoader = designTimeRuleLoader;
    }

    public DatabaseModel Create(string connectionString, DatabaseModelFactoryOptions options) {
        var model = actualFactory.Create(connectionString, options);

        // we can never intercept the DbConnection used to create the main model because it is scoped internally only
        if (actualFactory.GetType().Name.StartsWith("SqlServer")) {
            // attempt sproc mapping

            SqlConnection connection;

            try {
                connection = new SqlConnection(connectionString);
            } catch (ArgumentException) {
                // if connection string is a path to dacpac, when running together with ErikEJ.EntityFrameworkCore.SqlServer.Dacpac
                return model;
            }

            try {
                if (connection.State != ConnectionState.Open) connection.Open();
                var modelNew = new DatabaseModelEx(model);
                AppendToModel(connection, modelNew);
                return modelNew;
            } finally {
                connection.Close();
                connection.Dispose();
            }
        }

        return model;
    }


    public DatabaseModel Create(DbConnection connection, DatabaseModelFactoryOptions options) {
        var connectionStartedOpen = connection.State == ConnectionState.Open;
        if (!connectionStartedOpen) connection.Open();

        try {
            var model = actualFactory.Create(connection, options);

            Debug.Assert(connection.State == ConnectionState.Open);
            connectionStartedOpen = connection.State == ConnectionState.Open;
            if (!connectionStartedOpen) connection.Open();

            var modelNew = new DatabaseModelEx(model);

            AppendToModel(connection, modelNew);
            return modelNew;
        } finally {
            if (!connectionStartedOpen) connection.Close();
        }
    }

    protected virtual void OnModelAppended(DatabaseModelEx model) {
        VisitFunctions(model, model.Functions);
    }

    protected virtual void VisitFunctions(DatabaseModelEx model, IList<DatabaseFunction> functions) {
        foreach (var function in functions) {
            VisitFunction(model, function);
        }
    }

    protected virtual void VisitFunction(DatabaseModelEx model, DatabaseFunction function) {
        var hasComplexType = function.FunctionType == FunctionType.StoredProcedure;
        if (hasComplexType) {
            var i = 1;

            foreach (var resultTable in function.Results) {
                resultTable.Schema = function.Schema;
                resultTable.Comment = $"Result for function {function.Name}";
                resultTable.Ordinal = i++;
                resultTable.Function = function;
                resultTable.ResultColumns.ForAll(o => o.Table = resultTable);

                if (function.NoResultSet) continue;

                // make a name for the table 
                var separator = function.Name.Contains(" ") ? " " : (function.Name.Contains("_") ? "_" : "");
                var tableName = function.Name + separator + "Result";
                tableName = tableName.GetUniqueString(s => model.Tables.Any(o => string.Equals(o.Name, s, StringComparison.OrdinalIgnoreCase)));
                resultTable.Name = tableName;

                if (resultTable.ShouldScaffoldEntityFromTable) {
                    // add table for entity creation.  all columns must be named (allowance made for one unnamed column)

                    if (function.UnnamedColumnCount > 0) {
                        for (var j = 0; j < resultTable.Columns.Count; j++) {
                            var column = resultTable.Columns[j];
                            if (column.Name.IsNullOrWhiteSpace()) column.Name = $"Value{j}";
                        }
                    }

                    model.Tables.Add(resultTable);
                }
            }
        } else {
            Debug.Assert(function.IsScalar || function.IsTableValuedFunction || !function.Results.Any() || function.Results[0].Columns.Count == 0);
        }
    }


    private void AppendToModel(DbConnection connection, DatabaseModelEx model) {
        var dbContextRules = designTimeRuleLoader.GetDbContextRules();
        var getFunctions = dbContextRules?.Rule?.Schemas?.Any(s => s.IncludeUnknownFunctions || s.Functions?.Any(f => f.ShouldMap()) == true) == true;

        if (getFunctions) {
            IDatabaseModelFactoryEx databaseModelFactoryEx = null;
            var connectionName = connection?.GetType()?.Name;
            switch (connectionName) {
                case nameof(SqlConnection):
                    databaseModelFactoryEx = serviceProvider.GetConcrete<SqlServerDatabaseModelFactoryEx>();
                    break;
            }

            if (databaseModelFactoryEx == null) {
                reporter?.WriteWarning("DBMS not supported for stored procedure scaffolding");
                return;
            }

            databaseModelFactoryEx.AppendToModel(connection, model);
        }

        OnModelAppended(model);
    }
}