﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Game.Networking.External.Protocol;
using Grpc.Net.Client;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;

namespace Networking
{
    public class GameConnection
    {
        private readonly string _serverHost;
        private readonly int _serverPort;
        private GrpcChannel? _channel;

        public GameConnection(string serverHost, int serverPort)
        {
            _serverHost = serverHost;
            _serverPort = serverPort;
        }

        public async Task<(bool WasSuccessful, string? ErrorMessage)> Connect(string authToken)
        {
            _channel = GrpcChannel.ForAddress($"http://{_serverHost}:{_serverPort}", new GrpcChannelOptions()
            {
                HttpClient = new HttpClient()
                {
                    DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", authToken)}
                }
            });
            
            var auth = _channel.CreateGrpcService<IAuthorizationService>();
            var response = await auth.AuthorizeAsync(new AuthorizeRequest());

            return (response.WasSuccessful, response.ErrorMessage);
        }

        public async Task<IReadOnlyList<Character>> GetCharacters()
        {
            if (_channel is null) throw new Exception("Channel not ready yet!");
            var account = _channel.CreateGrpcService<IAccountService>();
            var response = await account.GetCharactersAsync(new GetCharactersRequest());
            return response.Characters;
        }
    }
}
