using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using EntityFrameworkRuler;

[assembly: AssemblyTitle(Vsix.Name)]
[assembly: AssemblyDescription(Vsix.Description)]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(Vsix.Author)]
[assembly: AssemblyProduct(Vsix.Name)]
[assembly: AssemblyCopyright(Vsix.Author)]
[assembly: AssemblyTrademark("")]
//[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: AssemblyVersion(Vsix.Version)]
[assembly: AssemblyFileVersion(Vsix.Version)]
//[assembly: System.Resources.NeutralResourcesLanguageAttribute("en-US", UltimateResourceFallbackLocation.Satellite)]

namespace EntityFrameworkRuler.Properties;

public class IsExternalInit { }