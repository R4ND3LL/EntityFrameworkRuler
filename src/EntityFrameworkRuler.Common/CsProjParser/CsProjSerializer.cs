using System.Xml.Linq;
using System.Xml.XPath;

namespace EntityFrameworkRuler.CsProjParser;

internal static class CsProjSerializer {
    public static CsProject Deserialize(string projContent) {
        var doc = XDocument.Parse(projContent);
        var csProj = new CsProject();

        foreach (var propertyGroup in doc.XPathSelectElements("//PropertyGroup")) {
            var implicitUsings = propertyGroup.Attribute("ImplicitUsings")?.Value ??
                                 propertyGroup.Element("ImplicitUsings")?.Value;
            if (implicitUsings.HasNonWhiteSpace()) csProj.ImplicitUsings = implicitUsings;

            var assemblyName = propertyGroup.Attribute("AssemblyName")?.Value ??
                               propertyGroup.Element("AssemblyName")?.Value;
            if (assemblyName.HasNonWhiteSpace()) csProj.AssemblyName = assemblyName;

            var targetFramework = propertyGroup.Attribute("TargetFramework")?.Value ??
                                  propertyGroup.Element("TargetFramework")?.Value;
            if (targetFramework.HasNonWhiteSpace()) csProj.TargetFramework = targetFramework;

            var targetFrameworks = propertyGroup.Attribute("TargetFrameworks")?.Value ??
                                   propertyGroup.Element("TargetFrameworks")?.Value;
            if (targetFrameworks.HasNonWhiteSpace()) csProj.TargetFrameworks = targetFrameworks;
        }

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