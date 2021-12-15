using System;
using Core.DataAccessLayer.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace UI.Data
{
    internal static class Registrar
    {
        public static IServiceCollection RegisterDatabase(this IServiceCollection services, IConfiguration configuration) => services
            .AddDbContext<RecognizedImagesDb>(options =>
            {
                var type = configuration["Type"];

                switch (type)
                {
                    case "MSSQL":
                        options.UseSqlServer(configuration.GetConnectionString(type));
                        break;

                    case null:
                        throw new InvalidOperationException("Database type not specified.");

                    default:
                        throw new InvalidOperationException("The database type is not supported.");
                }
            })
            .AddTransient<InitializerDb>();
    }
}
