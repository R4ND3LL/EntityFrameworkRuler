namespace EntityFrameworkRuler.Common.Annotations;

/// <summary> This is an internal API and is subject to change or removal without notice. </summary>
public static class EfScaffoldingAnnotationNames {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public const string Prefix = "Scaffolding:";

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public const string DbSetName = Prefix + nameof(DbSetName);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public const string DatabaseName = Prefix + nameof(DatabaseName);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public const string ConcurrencyToken = nameof(ConcurrencyToken);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public const string ConnectionString = Prefix + nameof(ConnectionString);

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    public const string ClrType = nameof(ClrType);
}