﻿using System;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Slate.Backend.Shared;
using Slate.Events.InMemory;
using Slate.Networking.Internal.Protocol.Cell.Services;
using StrongInject;

namespace Slate.Snowglobe
{
    [Register(typeof(CellServerNotifierService))]
    [Register(typeof(CellService), Scope.SingleInstance, typeof(ICellService))]
    [Register(typeof(Events.InMemory.EventAggregator), Scope.SingleInstance, typeof(IEventAggregator))]
    internal partial class SnowglobeContainer : CoreServicesModule,
        IContainer<CellServerNotifierService>,
        IContainer<ICellService>,
        IContainer<HeartbeatService>,
        IContainer<GracefulShutdownService>
    {
        private readonly IServiceProvider _services;

        public SnowglobeContainer(IServiceProvider services) : base(services)
        {
            _services = services;
        }

        [Instance]
        private IServerAddressesFeature Addresses =>
            _services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()
            ?? throw new Exception($"Unable to resolve {nameof(IServerAddressesFeature)}");
    }
}
