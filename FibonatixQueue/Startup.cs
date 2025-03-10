﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Azure.Storage.Queues;
using Azure.Storage.Blobs;
using Azure.Core.Extensions;
using StackExchange.Redis;
using FibonatixQueue.Services;
using FibonatixQueue.Controllers;
using FibonatixQueue.Settings;
[assembly: FunctionsStartup(typeof(FibonatixQueue.Startup))]

namespace FibonatixQueue
{
    public interface IStartup
    {
        IConfiguration Configuration { get; }

        void ConfigureServices(IServiceCollection services);

        void Configure(IApplicationBuilder app, IWebHostEnvironment env);
    }

    public class Startup : IStartup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            if (bool.Parse(Configuration["Transform"]))
            {
                services.Configure<SecureDBSettings>(db => { db.ConnectionString = Configuration["ConnectionString"]; db.Password = Configuration["Password"]; db.Algorithm = Configuration["Algorithm"]; });
                services.AddSingleton<ISecureServiceSettings>(s => s.GetRequiredService<IOptions<SecureDBSettings>>().Value);
            }
            else
            {
                services.Configure<CommonDBSettings>(db => { db.ConnectionString = Configuration["ConnectionString"]; db.Password = Configuration["Password"]; });
                services.AddSingleton<IServiceSettings>(s => s.GetRequiredService<IOptions<CommonDBSettings>>().Value);
            }

            // Keeps the service alive and it's symmetric algorithm properties
            if (Configuration["Service"] == "Redis")
            {
                services.AddSingleton<RedisQueueService>();
            }
            if (Configuration["Service"] == "MongoDB")
                services.AddSingleton<MongoQueueService>();

            services.AddControllers();
            services.AddCustomHealthCheck(Configuration);
            services.AddAzureClients(builder =>
            {
                builder.AddBlobServiceClient(Configuration["ConnectionStrings:LocalDBTesting:blob"], preferMsi: true);
                builder.AddQueueServiceClient(Configuration["ConnectionStrings:LocalDBTesting:queue"], preferMsi: true);
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "FibonatixQueue", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FibonatixQueue v1"));
            }

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FibonatixQueue v1"));

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            /*app.Run(async context =>
            {
                await context.Response.StartAsync();
                context.Response.Redirect("https://localhost:5001/swagger");
            });*/
        }
    }

    /*public class ServiceStartup : IStartup
    {
        public ServiceStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            if (bool.Parse(Configuration["Transform"]))
            {
                services.Configure<SecureDBSettings>(db => { db.ConnectionString = Configuration["ConnectionString"]; db.Password = Configuration["Password"]; db.Algorithm = Configuration["Algorithm"]; });
                services.AddSingleton<ISecureServiceSettings>(s => s.GetRequiredService<IOptions<SecureDBSettings>>().Value);
            }
            else
            {
                services.Configure<CommonDBSettings>(db => { db.ConnectionString = Configuration["ConnectionString"]; db.Password = Configuration["Password"]; });
                services.AddSingleton<IServiceSettings>(s => s.GetRequiredService<IOptions<CommonDBSettings>>().Value);
            }

            // Keeps the service alive and it's symmetric algorithm properties
            services.AddSingleton<RedisQueueService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }*/

    internal static class StartupExtensions
    {
        public static IAzureClientBuilder<BlobServiceClient, BlobClientOptions> AddBlobServiceClient(this AzureClientFactoryBuilder builder, string serviceUriOrConnectionString, bool preferMsi)
        {
            if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri serviceUri))
            {
                return builder.AddBlobServiceClient(serviceUri);
            }
            else
            {
                return builder.AddBlobServiceClient(serviceUriOrConnectionString);
            }
        }
        public static IAzureClientBuilder<QueueServiceClient, QueueClientOptions> AddQueueServiceClient(this AzureClientFactoryBuilder builder, string serviceUriOrConnectionString, bool preferMsi)
        {
            if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri serviceUri))
            {
                return builder.AddQueueServiceClient(serviceUri);
            }
            else
            {
                return builder.AddQueueServiceClient(serviceUriOrConnectionString);
            }
        }
    }

    /*internal static class StartupInput
    {
        public static void AddInputServiceClient(Array strings) // -> BackgroundService
        {

        }
    }*/
}
