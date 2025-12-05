using CoreWCF;
using CoreWCF.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WCFServices.Services;
using WCFServices.DataAccess;
using WCFServices.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CoreWCF.Channels;
using CoreWCF.Description;

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

            // 启动TIBCO适配器
            var tibcoAdapter = app.Services.GetService<ITibcoAdapter>();
            if (tibcoAdapter != null)
            {
                tibcoAdapter.Start();
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
            services.AddSingleton<ITibcoMessageService, TibcoMessageService>();
            services.AddSingleton<ITibcoMessageSender, TibcoMessageService>();
            services.AddSingleton<ITibcoMessageListener, TibcoMessageListener>();
            services.AddSingleton<ITibcoAdapter, TibcoAdapter>();

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