/*
 Licensed under the Apache License, Version 2.0

 http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Diagnostics;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.Applicator.CsProjParser {
    [DebuggerDisplay(
        "PackageReference={Include} v{Version} PrivateAssets={PrivateAssets} IncludeAssets={IncludeAssets}")]
    [XmlRoot(ElementName = "PackageReference")]
    internal class CsProjPackageReference {
        [XmlAttribute(AttributeName = "Include")]
        public string Include { get; set; }

        [XmlAttribute(AttributeName = "Version")]
        public string Version { get; set; }

        [XmlElement(ElementName = "IncludeAssets")]
        public string IncludeAssets { get; set; }

        [XmlElement(ElementName = "PrivateAssets")]
        public string PrivateAssets { get; set; }
    }

    [DebuggerDisplay("ProjectReference={Include}")]
    [XmlRoot(ElementName = "ProjectReference")]
    internal class CsProjProjectReference {
        [XmlAttribute(AttributeName = "Include")]
        public string Include { get; set; }
    }

    [DebuggerDisplay("Reference={Include} HintPath={HintPath}")]
    [XmlRoot(ElementName = "Reference")]
    internal class CsProjReference {
        [XmlElement(ElementName = "HintPath")]
        public string HintPath { get; set; }

        [XmlAttribute(AttributeName = "Include")]
        public string Include { get; set; }
    }

    [XmlRoot(ElementName = "Project")]
    internal class CsProject {
        [XmlAttribute(AttributeName = "Sdk")]
        public string Sdk { get; set; }

        [XmlElement(ElementName = "AssemblyName")]
        public string AssemblyName { get; set; }

        [XmlAttribute(AttributeName = "TargetFrameworks")]
        public string TargetFrameworks { get; set; }

        [XmlAttribute(AttributeName = "TargetFramework")]
        public string TargetFramework { get; set; }

        [XmlAttribute(AttributeName = "ImplicitUsings")]
        public string ImplicitUsings { get; set; }

        [XmlElement(ElementName = "PackageReference")]
        public List<CsProjPackageReference> PackageReference { get; set; } = new();

        [XmlElement(ElementName = "ProjectReference")]
        public List<CsProjProjectReference> ProjectReferences { get; set; } = new();

        [XmlElement(ElementName = "Reference")]
        public List<CsProjReference> References { get; set; } = new();

        [XmlIgnore]
        public FileInfo File { get; set; }

        public string GetAssemblyName() {
            if (AssemblyName.HasNonWhiteSpace() || File == null || File.FullName.IsNullOrWhiteSpace()) return AssemblyName;
            var name = File.Name;
            if (Path.HasExtension(name)) name = Path.GetFileNameWithoutExtension(name);
            return name;
        }

        public string FindSolutionParentPath() {
            if (File == null || File.FullName.IsNullOrWhiteSpace()) return null;
            return File.Directory?.FullName?.FindSolutionParentPath();
        }
    }
}