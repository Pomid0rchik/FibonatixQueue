﻿using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace FibonatixQueue
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var host = CreateHostBuilder(args)
                .ConfigureAppConfiguration((hostContext, builder) =>
                {
                    Layout.Main(hostContext.Configuration);

                    if (hostContext.HostingEnvironment.IsDevelopment())
                    {
                        builder.AddUserSecrets<Program>();
                    }
                });
            host.Build().RunAsync().Wait();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
