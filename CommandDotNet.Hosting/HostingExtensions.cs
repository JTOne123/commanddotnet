﻿using System;
using System.IO;
using System.Threading.Tasks;
using CommandDotNet.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CommandDotNet.Hosting
{
    public static class HostingExtensions
    {
        public static AppRunner UseHost(this AppRunner runner,
            string[]? args = null, 
            Action<IHostBuilder>? configureHost = null,
            bool supportReplSessions = false)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(hostConfig =>
                {
                    hostConfig.SetBasePath(Directory.GetCurrentDirectory());
                });
            configureHost?.Invoke(hostBuilder);
            return runner.UseHost(hostBuilder, supportReplSessions);
        }

        public static AppRunner UseHost(this AppRunner runner, IHostBuilder hostBuilder, bool supportReplSessions = false)
        {
            if (hostBuilder == null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }

            hostBuilder
                .ConfigureServices((hbc,services) =>
                {
                    if (supportReplSessions)
                    {
                        services.AddScoped<ScopedServices>();
                        services.AddScoped(provider => provider.GetRequiredService<ScopedServices>().CommandContext);
                    }
                    else
                    {
                        services.AddSingleton<ScopedServices>();
                        services.AddSingleton(provider => provider.GetRequiredService<ScopedServices>().CommandContext);
                    }
                });

            var host = hostBuilder.Build();

            return runner.Configure(cfg =>
            {
                cfg.UseMiddleware(RunHost, MiddlewareSteps.CancellationHandler + 1000);
                cfg.UseMiddleware(InjectCommandContextForDI, MiddlewareSteps.CancellationHandler + 1001);
                cfg.Services.Add(new Config(host, supportReplSessions));
            });
        }

        private class Config
        {
            public bool IsStarted;
            public IHost Host { get; }
            public bool SupportReplSessions { get; }

            public Config(IHost host, bool supportReplSessions)
            {
                Host = host ?? throw new ArgumentNullException(nameof(host));
                SupportReplSessions = supportReplSessions;
            }
        }

        private static async Task<int> RunHost(CommandContext context, ExecutionDelegate next)
        {
            var config = context.AppConfig.Services.GetOrThrow<Config>();

            if (config.IsStarted)
            {
                // this call is within a REPL session.
                // do not recreate the host
                return await next(context);
            }

            config.IsStarted = true;
            await config.Host.StartAsync();
            var result = await next(context);
            await config.Host.StopAsync();

            return result;
        }

        private class ScopedServices
        {
            public CommandContext? CommandContext;
        }

        private static Task<int> InjectCommandContextForDI(CommandContext context, ExecutionDelegate next)
        {
            var config = context.AppConfig.Services.GetOrThrow<Config>();
            var serviceProvider = config.Host.Services;

            if (config.SupportReplSessions)
            {
                using var scope = serviceProvider.CreateScope();
                scope.ServiceProvider.GetRequiredService<ScopedServices>().CommandContext = context;
                return next(context);
            }

            serviceProvider.GetRequiredService<ScopedServices>().CommandContext = context;
            return next(context);
        }
    }
}