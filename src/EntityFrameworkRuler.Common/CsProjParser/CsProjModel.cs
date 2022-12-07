/*
 Licensed under the Apache License, Version 2.0

 http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Diagnostics;
using System.Xml.Serialization;

namespace EntityFrameworkRuler.CsProjParser {
    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    [DebuggerDisplay(
        "PackageReference={Include} v{Version} PrivateAssets={PrivateAssets} IncludeAssets={IncludeAssets}")]
    [XmlRoot(ElementName = "PackageReference")]
    public class CsProjPackageReference {
        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlAttribute(AttributeName = "Include")]
        public string Include { get; set; }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlAttribute(AttributeName = "Version")]
        public string Version { get; set; }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlElement(ElementName = "IncludeAssets")]
        public string IncludeAssets { get; set; }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlElement(ElementName = "PrivateAssets")]
        public string PrivateAssets { get; set; }
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    [DebuggerDisplay("ProjectReference={Include}")]
    [XmlRoot(ElementName = "ProjectReference")]
    public class CsProjProjectReference {
        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlAttribute(AttributeName = "Include")]
        public string Include { get; set; }
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    [DebuggerDisplay("Reference={Include} HintPath={HintPath}")]
    [XmlRoot(ElementName = "Reference")]
    public class CsProjReference {
        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlElement(ElementName = "HintPath")]
        public string HintPath { get; set; }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlAttribute(AttributeName = "Include")]
        public string Include { get; set; }
    }

    /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
    [XmlRoot(ElementName = "Project")]
    public class CsProject {
        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlAttribute(AttributeName = "Sdk")]
        public string Sdk { get; set; }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlElement(ElementName = "AssemblyName")]
        public string AssemblyName { get; set; }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlAttribute(AttributeName = "TargetFrameworks")]
        public string TargetFrameworks { get; set; }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlAttribute(AttributeName = "TargetFramework")]
        public string TargetFramework { get; set; }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlAttribute(AttributeName = "ImplicitUsings")]
        public string ImplicitUsings { get; set; }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlElement(ElementName = "PackageReference")]
        public List<CsProjPackageReference> PackageReference { get; set; } = new();

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlElement(ElementName = "ProjectReference")]
        public List<CsProjProjectReference> ProjectReferences { get; set; } = new();

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlElement(ElementName = "Reference")]
        public List<CsProjReference> References { get; set; } = new();

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        [XmlIgnore]
        public FileInfo File { get; set; }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public string GetAssemblyName() {
            if (AssemblyName.HasNonWhiteSpace() || File == null || File.FullName.IsNullOrWhiteSpace()) return AssemblyName;
            var name = File.Name;
            if (Path.HasExtension(name)) name = Path.GetFileNameWithoutExtension(name);
            return name;
        }

        /// <summary> This is an internal API and is subject to change or removal without notice. </summary>
        public string FindSolutionParentPath() {
            if (File == null || File.FullName.IsNullOrWhiteSpace()) return null;
            return File.Directory?.FullName?.FindSolutionParentPath();
        }
    }
}