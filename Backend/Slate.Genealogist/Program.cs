﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using Microsoft.Extensions.DependencyInjection;
using Slate.Genealogist;
using Slate.Genealogist.Stores;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Verbose)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Verbose)
    .MinimumLevel.Override("System", LogEventLevel.Verbose)
    .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Verbose)
    .WriteTo.Seq("http://localhost:5341")
    .Enrich.FromLogContext()
    // uncomment to write to Azure diagnostics stream
    //.WriteTo.File(
    //    @"D:\home\LogFiles\Application\identityserver.txt",
    //    fileSizeLimitBytes: 1_000_000,
    //    rollOnFileSizeLimit: true,
    //    shared: true,
    //    flushToDiskInterval: TimeSpan.FromSeconds(1))
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
    .CreateLogger();

try
{
    Log.Information("Starting host...");
    var host = CreateHostBuilder(args).Build();
    var databaseMigration = host.Services.GetService<IMigrateDatabase>();
    await databaseMigration.Migrate();
    host.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly.");
    Console.ReadLine();
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
    