﻿namespace Giants.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Giants.Services.Core;
    using Microsoft.Extensions.Configuration;

    public class ServerRegistryService : IServerRegistryService
    {
        private readonly IServerRegistryStore registryStore;
        private readonly IConfiguration configuration;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly TimeSpan timeoutPeriod;
        private readonly int maxServerCount;

        public ServerRegistryService(
            IServerRegistryStore registryStore,
            IConfiguration configuration,
            IDateTimeProvider dateTimeProvider)
        {
            this.registryStore = registryStore;
            this.configuration = configuration;
            this.dateTimeProvider = dateTimeProvider;
            this.timeoutPeriod = TimeSpan.FromMinutes(Convert.ToDouble(this.configuration["ServerTimeoutPeriodInMinutes"]));
            this.maxServerCount = Convert.ToInt32(this.configuration["MaxServerCount"]);
        }

        public async Task AddServer(
            ServerInfo server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (string.IsNullOrEmpty(server.HostIpAddress))
            {
                throw new ArgumentException(nameof(server.HostIpAddress));
            }

            await this.registryStore.UpsertServerInfo(server ?? throw new ArgumentNullException(nameof(server)));
        }

        public async Task<IEnumerable<ServerInfo>> GetAllServers()
        {
            return (await this.registryStore
                .GetServerInfos(whereExpression: c => c.LastHeartbeat > this.dateTimeProvider.UtcNow - this.timeoutPeriod))
                .Take(this.maxServerCount);
        }
    }
}
