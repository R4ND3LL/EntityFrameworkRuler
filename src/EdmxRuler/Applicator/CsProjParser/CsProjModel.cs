/*
 Licensed under the Apache License, Version 2.0

 http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using EdmxRuler.Extensions;

namespace EdmxRuler.Applicator.CsProjParser {
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
        public string FilePath { get; set; }

        public string GetAssemblyName() {
            if (AssemblyName.HasNonWhiteSpace() || FilePath.IsNullOrWhiteSpace()) return AssemblyName;
            var fileInfo = new FileInfo(FilePath);
            var name = fileInfo.Name;
            if (Path.HasExtension(name)) name = Path.GetFileNameWithoutExtension(name);
            return name;
        }

        public static string[] FindEdmxFilesNearProject(string projectBasePath) {
            return FindEdmxFilesUnderPath(FindSolutionParentPath(projectBasePath));
        }

        public static string[] FindEdmxFilesUnderPath(string solutionBasePath) {
            return Directory.GetFiles(solutionBasePath, "*.edmx", SearchOption.AllDirectories);
        }

        public string FindSolutionParentPath() {
            if (FilePath.IsNullOrWhiteSpace()) return null;
            var fileInfo = new FileInfo(FilePath);
            return fileInfo.Directory?.FullName == null ? null : FindSolutionParentPath(fileInfo.Directory.FullName);
        }

        public static string FindSolutionParentPath(string projectBasePath) {
            var di = new DirectoryInfo(projectBasePath);
            while (di?.GetFiles("*.sln", SearchOption.TopDirectoryOnly).Length == 0) di = di.Parent;
            return di?.Exists != true ? projectBasePath : di.FullName;
        }
    }
}