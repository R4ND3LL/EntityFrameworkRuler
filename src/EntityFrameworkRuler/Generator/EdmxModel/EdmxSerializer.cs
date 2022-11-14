using System.Xml;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Generator.EdmxModel;

// ReSharper disable once MemberCanBeInternal
public static class EdmxSerializer {
    // ReSharper disable once InconsistentNaming
    private static XmlSerializer serializer;

    private static XmlSerializer SerializerXml =>
        serializer ??= new XmlSerializerFactory().CreateSerializer(typeof(EdmxRoot));

    // ReSharper disable once MemberCanBeInternal
    public static EdmxRoot Deserialize(string edmxContent) {
        using var stringReader = new StringReader(edmxContent);
        using var xmlReader = XmlReader.Create(stringReader);
        return (EdmxRoot)SerializerXml.Deserialize(xmlReader);
    }
}