using CoreWCF;
using CoreWCF.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WCFServices.Contracts;
using WCFServices.Database;
using WCFServices.Implementations;
using WCFServices.Services;

namespace WCFServices
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddServiceModelServices();
            
            // Register database repository
            builder.Services.AddScoped<IMssqlRepository, MssqlRepository>();
            
            // Register message service
            builder.Services.AddScoped<IMessageService, MessageService>();
            
            // Register WCF service implementation
            builder.Services.AddScoped<IWcfService, WcfServiceImpl>();

            var app = builder.Build();

            // Initialize database
            using (var scope = app.Services.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IMssqlRepository>();
                repository.InitializeDatabaseAsync().Wait();
            }

            // Configure the HTTP request pipeline
            app.UseServiceModel(builder =>
            {
                builder.AddService<WcfServiceImpl>(serviceOptions =>
                {
                    serviceOptions.Singleton();
                });

                builder.AddServiceEndpoint<WcfServiceImpl, IWcfService>(
                    new BasicHttpBinding(),
                    "/WcfService.svc"
                );
            });

            // Start TIBRV listening after application starts
            var messageService = app.Services.GetRequiredService<IMessageService>();
            app.Lifetime.ApplicationStarted.Register(async () =>
            {
                await Task.Delay(2000); // Wait 2 seconds for WCF service to start
                await messageService.StartListeningAsync();
            });

            // Handle shutdown
            app.Lifetime.ApplicationStopping.Register(async () =>
            {
                await messageService.StopListeningAsync();
            });

            app.Run();
        }
    }
}