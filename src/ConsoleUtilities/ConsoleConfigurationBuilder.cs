﻿using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Fulgoribus.ConsoleUtilities
{
    public static class ConsoleConfigurationBuilder
    {
        /// <summary>
        /// Create an <see cref="IConfigurationRoot"/> using the standard locations/order outside a hosted environment.
        /// </summary>
        /// <param name="args">Console application command-line arguments.</param>
        /// <param name="appAssembly">(Development environment only.) Assembly of the console app configuration is being created for, for loading user
        /// secrets. The assembly must have an <see cref="Microsoft.Extensions.Configuration.UserSecrets.UserSecretsIdAttribute"/> applied (typically
        /// accomplished by referencing the Microsoft.Extensions.Configuration.UserSecrets NuGet package; the reference by this project is not enough)
        /// so it knows what UserSecretId to use. </param>
        /// <returns><see cref="IConfigurationRoot"/> from JSON configuration, user secrets, environment variables, and the command line.</returns>
        /// <remarks>Supporting a null <paramref name="args"/> because the core framework does. In practice, this should not happen.</remarks>
        public static IConfigurationRoot BuildConfiguration(string[]? args, Assembly? appAssembly = null)
        {
            var configurationBuilder = new ConfigurationBuilder();

            // Determine what environment we're running in.
            configurationBuilder.AddEnvironmentVariables(prefix: "DOTNET_");
            if (args != null)
            {
                configurationBuilder.AddCommandLine(args);
            }
            var environment = new ConsoleHostEnvironment(configurationBuilder.Build());

            // Build application settings following the same order as HostBuilder/WebHostBuilder.
            configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
            if (environment.IsDevelopment() && appAssembly != null)
            {
                configurationBuilder.AddUserSecrets(appAssembly, optional: true);
            }
            configurationBuilder.AddEnvironmentVariables();
            if (args != null)
            {
                configurationBuilder.AddCommandLine(args);
            }

            return configurationBuilder.Build();
        }
    }
}
