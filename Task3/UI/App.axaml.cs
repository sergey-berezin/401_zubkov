using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using UI.ViewModels;
using UI.Views;


namespace UI
{
    public class App: Application {
        public override void Initialize() {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted() {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var DataContext = Bootstrapper.Host.Services.GetRequiredService<MainWindowViewModel>();
                desktop.MainWindow = new MainWindow {
                    DataContext = DataContext,
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
