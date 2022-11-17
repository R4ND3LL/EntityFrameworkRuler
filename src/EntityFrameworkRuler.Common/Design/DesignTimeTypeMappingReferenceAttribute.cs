namespace EntityFrameworkRuler.Design;

/// <summary>
///     <para>
///         Identifies an additional assembly to inspect for custom property types during design time scaffolding.
///     </para>
///     <para>
///         This attribute is typically used by design-time extensions. It is generally not used in application code.
///     </para>
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-providers">Implementation of database providers and extensions</see>
///     for more information and examples.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class DesignTimeTypeMappingReferenceAttribute {

}