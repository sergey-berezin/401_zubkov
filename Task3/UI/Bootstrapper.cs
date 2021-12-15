using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UI.Data;
using UI.Services;
using UI.ViewModels;


namespace UI
{
    internal class Bootstrapper
    {
        private static IHost? host;
        public static IHost Host => host ??= Program.CreateHostBuilder(Environment.GetCommandLineArgs()).Build();

        public static IServiceProvider Services => Host.Services;

        internal static void ConfigureServices(HostBuilderContext host, IServiceCollection services) => services
            .RegisterDatabase(host.Configuration.GetSection("Database"))
            .RegisterServices()
            .RegisterViewModels();
    }
}
