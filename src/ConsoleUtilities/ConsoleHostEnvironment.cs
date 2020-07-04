using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Fulgoribus.ConsoleUtilities
{
    /// <summary>
    /// Create an <see cref="IHostEnvironment"/> using the same rules as the HostBuilder class does, without requiring an entire Host environment.
    /// </summary>
    public class ConsoleHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; }

        public string? ApplicationName { get; set; }

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; }

        public ConsoleHostEnvironment(IConfigurationRoot configuration)
        {
            EnvironmentName = configuration[HostDefaults.EnvironmentKey] ?? Environments.Production;
            ApplicationName = configuration[HostDefaults.ApplicationKey];
            ContentRootPath = ResolveContentRootPath(configuration[HostDefaults.ContentRootKey], AppContext.BaseDirectory);

            if (string.IsNullOrEmpty(ApplicationName))
            {
                ApplicationName = Assembly.GetEntryAssembly()?.GetName().Name;
            }

            ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
        }

        private static string ResolveContentRootPath(string contentRootPath, string basePath)
        {
            if (string.IsNullOrEmpty(contentRootPath))
            {
                return basePath;
            }
            if (Path.IsPathRooted(contentRootPath))
            {
                return contentRootPath;
            }
            return Path.Combine(Path.GetFullPath(basePath), contentRootPath);
        }
    }
}
