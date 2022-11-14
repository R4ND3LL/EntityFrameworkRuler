using EntityFrameworkRuler.Generator.EdmxModel;

namespace EntityFrameworkRuler.Generator.Services;

/// <summary>
/// Service that decides how to name navigation properties.
/// Similar to EF ICandidateNamingService but this one utilizes the EDMX model only.
/// </summary>
public interface IEdmxRulerNamingService {
    /// <summary> Get the possible navigation names that we expect EF to generate during the reverse engineering process. </summary>
    IEnumerable<string> FindCandidateNavigationNames(NavigationProperty navigation);

    /// <summary> Get the entity name that we expect EF to generate during the reverse engineering process. </summary>
    string GetExpectedEntityTypeName(EntityType entity);

    /// <summary> Get the property name that we expect EF to generate during the reverse engineering process. </summary>
    string GetExpectedPropertyName(EntityProperty property, string expectedEntityTypeName = null);
}