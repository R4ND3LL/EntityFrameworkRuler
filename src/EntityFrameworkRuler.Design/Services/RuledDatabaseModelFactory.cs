using System.Data;
using System.Data.Common;
using System.Globalization;
using Castle.DynamicProxy;
using EntityFrameworkRuler.Design.Services.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace EntityFrameworkRuler.Design.Services;

/// <summary> Proxy for the actual IDatabaseModelFactory </summary>
internal sealed class RuledDatabaseModelFactory : IDatabaseModelFactory {
    private readonly IServiceProvider serviceProvider;
    private readonly IDatabaseModelFactory actualFactory;
    private readonly IOperationReporter reporter;

    public RuledDatabaseModelFactory(IServiceProvider serviceProvider, IDatabaseModelFactory actualFactory, IOperationReporter reporter) {
        this.serviceProvider = serviceProvider;
        this.actualFactory = actualFactory;
        this.reporter = reporter;
    }

    public DatabaseModel Create(string connectionString, DatabaseModelFactoryOptions options) {
        var model = actualFactory.Create(connectionString, options);

        // we can never intercept the DbConnection used to create the main model because it is scoped internally only
        if (actualFactory.GetType().Name.StartsWith("SqlServer")) {
            // attempt sproc mapping
            using var connection = new SqlConnection(connectionString);
            try {
                if (connection.State != ConnectionState.Open) connection.Open();
                var modelNew = new DatabaseModelEx(model);
                AppendToModel(connection, modelNew);
                return modelNew;
            } finally {
                connection.Close();
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

    private void AppendToModel(DbConnection connection, DatabaseModelEx model) {
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

}