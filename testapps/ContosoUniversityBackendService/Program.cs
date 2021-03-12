using ContosoUniversityBackendService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace ContosoUniversityBackendService
{
    class Program
    {

        static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            CreateDbIfNotExists(host);

            host.Run();
        }

        private static void CreateDbIfNotExists(IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                try
                {
                    var context = services.GetRequiredService<SchoolContext>();
                    // context.Database.EnsureCreated();
                    DbInitializer.Initialize(context);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred creating the DB.");
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");

#if DEBUG
            builder.AddJsonFile("appsettings.Development.json", true);
#endif

            var configuration = builder.Build();

            var hostBuilder = Host.CreateDefaultBuilder()
                                    .ConfigureServices(services =>
                                    {
                                        services.AddSingleton<IConfiguration>(builder.Build());
                                        services.AddDbContext<SchoolContext>(options =>
                                        {
                                            options.UseSqlServer(configuration.GetConnectionString("SchoolContext"));
                                        });
                                        services.AddHostedService<ContosoService>();
                                    });


            return hostBuilder;
        }
    }
}
