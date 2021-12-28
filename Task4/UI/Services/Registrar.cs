using Microsoft.Extensions.DependencyInjection;


namespace UI.Services
{
    internal static class Registrar
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services)=> services
            .AddSingleton<IAsyncDataService, ObjectRecognizerWebService>();
    }
}
