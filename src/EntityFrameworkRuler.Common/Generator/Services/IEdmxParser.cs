using EntityFrameworkRuler.Generator.EdmxModel;

namespace EntityFrameworkRuler.Generator.Services;

/// <summary> Service that parses an EDMX file into an object model usable for rule generation. </summary>
public interface IEdmxParser {
    /// <summary> Parse the EDMX file at the given location </summary>
    EdmxParsed Parse(string filePath);
}