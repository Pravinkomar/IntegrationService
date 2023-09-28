using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting.WindowsServices;

namespace IntegrationService
{
    class Program
    {
        internal static IConfigurationRoot Config;

        static void Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            // Config the DatabaseTriggers
            // this._notificationStoredProcedure = ConfigurationManager.AppSettings["test.notificationStoredProcedure"];


            Config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                //.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
                .Build();

            IntegrationListener iL = new IntegrationListener(1);
            iL.StartListener();

            if (isService)
            {
                var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                var pathToContentRoot = Path.GetDirectoryName(pathToExe);

                var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<WebServiceStartup>()
                .UseConfiguration(Config)
                .UseContentRoot(pathToContentRoot)
                //.UseContentRoot(Directory.GetCurrentDirectory())
                .UseSetting("detailedErrors", "true")
                .CaptureStartupErrors(true)
                .Build();

                host.RunAsService();
            }
            else
            {
                var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<WebServiceStartup>()
                .UseConfiguration(Config)             
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseSetting("detailedErrors", "true")
                .CaptureStartupErrors(true)
                .Build();

                host.Run();
            }

            if (!isService)
            {
                Console.ReadLine();
                iL.StopListener();
            }
        }
    }
}
