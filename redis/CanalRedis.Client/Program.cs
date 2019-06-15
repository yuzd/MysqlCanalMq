﻿using System;
using System.IO;
using System.Linq;
using AntData.ORM.Data;
using Canal.Server;
using Canal.SqlParse;
using Canal.SqlParse.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Extensions.Logging;

namespace CanalRedis.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            NLog.LogManager.LoadConfiguration("NLog.Config");

            var builderConfig = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"))
                .AddEnvironmentVariables().Build();

            AntData.ORM.Common.Configuration.Linq.AllowMultipleQuery = true;

            AntData.ORM.Common.Configuration.UseDBConfig(builderConfig);

            var connectionString = builderConfig["consume.db"];
            if (!string.IsNullOrEmpty(connectionString))
            {
                var dbConnectionConfig = AntData.ORM.Common.Configuration.DBSettings.DatabaseSettings.First().ConnectionItemList.First();
                dbConnectionConfig.ConnectionString = connectionString;
                Console.WriteLine(connectionString);
            }

            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    
                    services.AddHostedService<ConsumerService>();
                    services.UseMysqlParseService();

                    services.AddMysqlEntitys<DB>("to", ops =>
                    {
                        ops.IsEnableLogTrace = false;
                        ops.OnLogTrace = OnLogTrace;
                    });

                    services.AddLogging(config => config.AddNLog());

                    services.Configure<RedisOption>(builderConfig.GetSection("Redis"));

                    services.AddSingleton<IConfiguration>(builderConfig);

                });

            builder.Build().Run();
        }
        private static void OnLogTrace(CustomerTraceInfo obj)
        {

        }
    }
}