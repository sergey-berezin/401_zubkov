using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.WebApi.Context;
using Microsoft.EntityFrameworkCore;


namespace Core.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services) => services
            .AddDbContext<RecognizedImagesDb>(options =>
            {
                var configuration = Configuration.GetSection("Database");
                var type = configuration["Type"];
                switch (type)
                {
                    case "MSSQL":
                        options.UseLazyLoadingProxies().UseSqlServer(configuration.GetConnectionString(type));
                        break;

                    case null:
                        throw new InvalidOperationException("Database type not specified.");

                    default:
                        throw new InvalidOperationException("The database type is not supported.");
                }
            })
            .AddControllers();


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
