using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UI.Data;


namespace UI
{
    class Program {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static async Task Main(string[] args)
        {
            var host = Bootstrapper.Host;
            await host.StartAsync().ConfigureAwait(false);

            using (var scope = Bootstrapper.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<InitializerDb>().InitializeAsync().Wait();
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

            await host.StopAsync().ConfigureAwait(false);
            host.Dispose();
        }


        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();


        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureServices(Bootstrapper.ConfigureServices);
    }
}
