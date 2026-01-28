using CoreWCF.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WCFServices.Models;
using WCFServices.Services;

namespace WCFServices
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 配置服务
            ConfigureServices(builder.Services);

            var app = builder.Build();

            // 配置WCF endpoints
            app.UseServiceModel();

            // 启动TIBCO订阅者服务
            var tibcoSubscriber = app.Services.GetService<TibcoSubscriberService>();
            if (tibcoSubscriber != null)
            {
                // 在后台启动TIBCO订阅者服务
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await tibcoSubscriber.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"TIBCO订阅者服务启动失败: {ex.Message}");
                    }
                });
            }

            // 启动WCF服务
            app.Run();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // 添加日志
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // 配置Oracle数据库
            var oracleConfig = new OracleConfig
            {
                ConnectionString = "Data Source=localhost:1521/XE;User Id=system;Password=oracle;Connection Timeout=30;",
                CommandTimeout = 30,
                EnableLogging = true,
                MaxPoolSize = 100,
                MinPoolSize = 5,
                ConnectionTimeout = 30
            };
            services.AddSingleton(oracleConfig);
            services.AddSingleton<IOracleDataAccess, OracleDataAccess>();

            // 配置TIBCO服务
            services.AddSingleton<TibrvRendezvousService>();
            services.AddSingleton<ITibcoMessageService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<TibcoMessageService>>();
                var tibcoService = provider.GetRequiredService<TibrvRendezvousService>();
                return new TibcoMessageService(logger, tibcoService);
            });
            services.AddSingleton<ITibcoMessageSender>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<TibcoMessageService>>();
                var tibcoService = provider.GetRequiredService<TibrvRendezvousService>();
                return new TibcoMessageService(logger, tibcoService);
            });
            services.AddSingleton<ITibcoMessageListener>(provider =>
            {
                var businessService = provider.GetRequiredService<IMesBusinessService>();
                var logger = provider.GetRequiredService<ILogger<TibcoMessageListener>>();
                return new TibcoMessageListener(businessService, logger);
            });
            services.AddSingleton<ITibcoAdapter>(provider =>
            {
                var messageService = provider.GetRequiredService<ITibcoMessageService>();
                var businessService = provider.GetRequiredService<IMesBusinessService>();
                var logger = provider.GetRequiredService<ILogger<TibcoAdapter>>();
                return new TibcoAdapter(messageService, businessService, logger);
            });
            
            // 添加TIBCO订阅者服务
            services.AddSingleton<TibcoSubscriberService>();

            // 配置业务服务
            services.AddScoped<IMesBusinessService, MesBusinessService>();

            // 配置WCF服务
            services.AddServiceModelServices();
            services.AddSingleton<IMesService, MesService>();
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddServiceModelServices();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseServiceModel(builder =>
            {
                builder.AddService<MesService>();
                builder.AddServiceEndpoint<MesService, IMesService>(new CoreWCF.BasicHttpBinding(), "/MESService");
            });
        }
    }
}