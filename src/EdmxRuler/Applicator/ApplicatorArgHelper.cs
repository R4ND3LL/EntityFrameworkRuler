namespace EdmxRuler.Applicator;

public static class ApplicatorArgHelper {
    internal static bool TryParseArgs(string[] args, out string projectBasePath) {
        projectBasePath = null;
        if (args == null || args.Length == 0 || (args.Length == 1 && args[0] == ".")) {
            // auto inspect current folder for both csproj and edmx
            projectBasePath = Directory.GetCurrentDirectory();
            var projectFiles = Directory.GetFiles(projectBasePath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (projectFiles.Length != 1) {
                projectBasePath = null;
                return false;
            }

            return true;
        }

        var csProjFile =
            args.FirstOrDefault(o => o?.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) == true);

        if (csProjFile != null && File.Exists(csProjFile)) {
            projectBasePath = new FileInfo(csProjFile).Directory?.FullName;
            return projectBasePath != null;
        }

        projectBasePath = args.FirstOrDefault(o =>
            o?.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) == false
            && Directory.Exists(o));
        if (projectBasePath == null) return false;

        if (!Directory.Exists(projectBasePath)) return false;

        var projectFiles2 = Directory.GetFiles(projectBasePath, "*.csproj", SearchOption.TopDirectoryOnly);
        if (projectFiles2.Length == 0) {
            projectBasePath = null;
            return false;
        }

        return true;
    }
}