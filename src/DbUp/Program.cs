using System;
using System.Reflection;
using DbUp;
using Fulgoribus.ConsoleUtilities;
using Microsoft.Extensions.Configuration;

namespace Fulgoribus.Luxae.DbUp
{
    static class Program
    {
        static int Main(string[] args)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var configuration = ConsoleConfigurationBuilder.BuildConfiguration(args, assembly);

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            var upgrader = DeployChanges.To.SqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(assembly)
                .WithTransactionPerScript()
                .LogToConsole()
                .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(result.Error);
                Console.ResetColor();
#if DEBUG
                Console.ReadLine();
#endif
                return -1;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success!");
            Console.ResetColor();
            return 0;
        }
    }
}
