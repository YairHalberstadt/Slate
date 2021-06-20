﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Slate.Networking.Internal.Protocol;
using Slate.Networking.RabbitMQ;
using Slate.Overseer.Configuration;

namespace Slate.Overseer
{
    internal class ApplicationLauncher : IHostedService
    {
        private readonly List<Process> _managedProcesses = new();

        private readonly ILogger _logger;
        private readonly IHostEnvironment _hostingEnvironment;
        private readonly IRabbitClient _rabbitClient;
        private readonly ComponentSection _componentSection;
        private bool _running = true;

        public ApplicationLauncher(ILogger logger, IHostEnvironment hostingEnvironment, ComponentSection componentSection, IRabbitClient rabbitClient)
        {
            _logger = logger.ForContext<ApplicationLauncher>();
            _hostingEnvironment = hostingEnvironment;
            _componentSection = componentSection;
            _rabbitClient = rabbitClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Information($"{Assembly.GetEntryAssembly()?.GetName().Name} Started");
            _logger.Information($"Component Root Path: {_componentSection.ComponentRootPath}");

            foreach (var definition in _componentSection.Definitions.Where(d => d.LaunchOnStart))
            {
                LaunchComponent(definition);
            }

            return Task.CompletedTask;
        }

        private void LaunchComponent(ComponentDefinition definition)
        {
            if (!_running) return;

            string useHeartbeat = Debugger.IsAttached ? " --UseHeartbeat True" : string.Empty;

            var fileName = Path.GetFullPath(Path.Combine(_componentSection.ComponentRootPath, definition.Application));
            var startInfo = new ProcessStartInfo(fileName)
            {
                WorkingDirectory = Path.GetDirectoryName(fileName),
                UseShellExecute = true,
                Arguments = $"--Environment={_hostingEnvironment.EnvironmentName}{useHeartbeat}"
            };
            
            Task.Run(async () =>
            {
                try
                {
                    _logger
                        .ForContext("Parameters", startInfo.ArgumentList, true)
                        .ForContext("WorkingDirectory", startInfo.WorkingDirectory)
                        .Information("Starting application {ApplicationName}", startInfo.FileName);

                    var process = Process.Start(startInfo);
                    if (process is null)
                    {
                        throw new Exception($"Unable to start process {fileName}");
                    }
                    _managedProcesses.Add(process);
                    await process.WaitForExitAsync();
                    OnProcessExited(definition);
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Error in component {definition.Application}");
                }
            });
        }

        private void OnProcessExited(ComponentDefinition definition)
        {
            if (definition.LaunchOnStart)
            {
                LaunchComponent(definition);
            }
        }
        
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _running = false;
            _rabbitClient.Send(new FullSystemShutdownMessage());
            var exitTasks = _managedProcesses.Select(async mp =>
            {
                var delayTask = Task.Delay(TimeSpan.FromSeconds(30));
                var exitTask = mp.WaitForExitAsync();
                var finishedTask = await Task.WhenAny(delayTask, exitTask);
                if (finishedTask == delayTask)
                {
                    _logger.Warning("Process {ProcessName} did not exit after being sent the shutdown message", mp.ProcessName);
                    mp.Kill();
                }
            });

            await Task.WhenAll(exitTasks);
        }
    }
}
