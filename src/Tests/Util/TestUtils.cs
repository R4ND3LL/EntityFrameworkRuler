using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options; 

namespace EdmxRuler.Tests.Util;

public sealed class TestTools {
    public IConfigurationRoot Config { get; set; }
    public IServiceProvider ServiceProvider { get; set; }
}

public static class TestUtils {
    public static TestTools InitConfigSettings(bool createServiceProvider = false) {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && dir.Name != "EdmxRuler") dir = dir.Parent;
        var path = dir?.Exists == true ? Path.Combine(dir.FullName, "EdmxRuler") : null;

        if (!Directory.Exists(path)) throw new Exception("App path not found");

        var builder = new ConfigurationBuilder()
                .SetBasePath(path)
            ;
        var configuration = builder.Build();


        ServiceProvider sp = null;
        if (createServiceProvider) {
            IServiceCollection services = new ServiceCollection();
            ConfigureServices(services, configuration);

            sp = services.BuildServiceProvider();
        }

        var tools = new TestTools() { Config = configuration, ServiceProvider = sp };

        return tools;
    }

    private static void ConfigureServices(IServiceCollection services, IConfigurationRoot configuration) {
        services.AddSingleton(configuration);
    }
 
 
}
 