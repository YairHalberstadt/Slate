﻿using System;
using System.IO.Compression;
using MessagePipe;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;
using Slate.GameWarden.Game;
using Slate.GameWarden.ServiceLocation;
using Slate.Networking.External.Protocol;
using StrongInject;

namespace Slate.GameWarden
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.UseStrongInjectForGrpcServiceResolution();
            services.AddMessagePipe();
            services.AddCodeFirstGrpc(config =>
            {
                config.ResponseCompressionLevel = CompressionLevel.Optimal;
                config.EnableDetailedErrors = true;
            });
            services.TryAddSingleton(BinderConfiguration.Create(binder: new ServiceBinderWithServiceResolutionFromServiceCollection(services)));
            services.AddCodeFirstGrpcReflection();

            services.AddAuthorization();
            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Authority = "https://localhost:8001";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false
                    };
                });

            services.ReplaceWithSingletonServiceUsingContainer<ServiceLocation.GameContainer, IAuthorizationService>();
            services.ReplaceWithSingletonServiceUsingContainer<ServiceLocation.GameContainer, IAccountService>();
            services.ReplaceWithSingletonServiceUsingContainer<ServiceLocation.GameContainer, IGameService>();
            services.AddSingleton<IContainer<Func<Guid, CharacterCoordinator>>>(sp => sp.GetRequiredService<ServiceLocation.GameContainer>());
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment _)
        {
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<IAuthorizationService>();
                endpoints.MapGrpcService<IAccountService>();
                endpoints.MapGrpcService<IGameService>();
            });
        }
    }
}