using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;


namespace UI.ViewModels
{
    internal static class Registrar
    {
        public static IServiceCollection RegisterViewModels(this IServiceCollection services) => services.
            AddSingleton<MainWindowViewModel>();
    }
}
