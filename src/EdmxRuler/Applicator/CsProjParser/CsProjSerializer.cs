using System.Xml.Linq;
using System.Xml.XPath;

namespace EdmxRuler.Applicator.CsProjParser;

internal static class CsProjSerializer {
  
    public static CsProject Deserialize(string csprojContent) {
        var doc = XDocument.Parse(csprojContent);
        var csProj = new CsProject();

        csProj.PackageReference.AddRange(doc.XPathSelectElements("//PackageReference")
            .Select(pr => new CsProjPackageReference {
                Include = pr.Attribute("Include")?.Value,
                Version = pr.Attribute("Version")?.Value ?? pr.Element("Version")?.Value,
                PrivateAssets = pr.Attribute("PrivateAssets")?.Value ?? pr.Element("PrivateAssets")?.Value,
                IncludeAssets = pr.Attribute("IncludeAssets")?.Value ?? pr.Element("IncludeAssets")?.Value,
            }));

        csProj.ProjectReferences.AddRange(doc.XPathSelectElements("//ProjectReference")
            .Select(pr => new CsProjProjectReference {
                Include = pr.Attribute("Include")?.Value,
            }));
        csProj.References.AddRange(doc.XPathSelectElements("//Reference")
            .Select(pr => new CsProjReference {
                Include = pr.Attribute("Include")?.Value,
                HintPath = pr.Element("HintPath")?.Value,
            }));

        return csProj;
    }
}