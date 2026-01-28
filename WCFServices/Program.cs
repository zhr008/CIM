using CoreWCF.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            // 添加配置
            services.Configure<OracleConfig>(options =>
            {
                options.ConnectionString = "Data Source=localhost:1521/XE;User Id=system;Password=oracle;Connection Timeout=30;";
                options.CommandTimeout = 30;
                options.EnableLogging = true;
                options.MaxPoolSize = 100;
                options.MinPoolSize = 5;
                options.ConnectionTimeout = 30;
            });

            // 注册Dapper-based Oracle访问服务
            services.AddScoped<IOracleDataAccess, OracleDataAccess>();

            // 配置TIBCO服务
            services.AddSingleton<TibrvService>(); // 使用Common.Services中的TibrvService
            services.AddScoped<ITibcoMessageService, TibcoMessageService>();
            services.AddScoped<ITibcoMessageSender, TibcoMessageService>();
            services.AddScoped<ITibcoMessageListener, TibcoMessageListener>();
            services.AddScoped<ITibcoAdapter, TibcoAdapter>();
            
            // 添加TIBCO订阅者服务
            services.AddSingleton<TibcoSubscriberService>();

            // 配置业务服务
            services.AddScoped<IMesBusinessService, MesBusinessService>();

            // 配置WCF服务
            services.AddServiceModelServices();
            services.AddScoped<IMesService, MesService>();
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddServiceModelServices();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseServiceModel(builder =>
            {
                builder.AddService<MesService>();
                builder.AddServiceEndpoint<MesService, IMesService>(new CoreWCF.BasicHttpBinding(), "/MESService");
            });
        }
    }
}