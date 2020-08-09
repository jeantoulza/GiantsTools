﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Giants.DataContract;
using Giants.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Giants.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ServersController : ControllerBase
    {
        private readonly IMapper mapper;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<ServersController> logger;
        private readonly IServerRegistryService serverRegistryService;

        public ServersController(
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ServersController> logger,
            IServerRegistryService serverRegistryService)
        {
            this.mapper = mapper;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
            this.serverRegistryService = serverRegistryService;
        }

        [HttpGet]
        public async Task<IEnumerable<DataContract.ServerInfoWithHostAddress>> GetServers()
        {
            IEnumerable<Services.ServerInfo> serverInfo = await this.serverRegistryService.GetAllServers();

            IMapper mapper = Services.Mapper.GetMapper();
            var mappedServers = serverInfo
                .Select(x => 
                {
                    var serverInfo = mapper.Map<ServerInfoWithHostAddress>(x);
                    serverInfo.HostIpAddress = x.HostIpAddress;
                    return serverInfo;
                })
                .ToList();

            string requestIpAddress = this.GetRequestIpAddress();
            logger.LogInformation("Returning {Count} servers to {IPAddress}", mappedServers.Count, requestIpAddress);

            return mappedServers;
        }

        [HttpPost]
        public async Task AddServer([FromBody]DataContract.ServerInfo serverInfo)
        {
            ArgumentUtility.CheckForNull(serverInfo, nameof(serverInfo));

            string requestIpAddress = this.GetRequestIpAddress();

            this.logger.LogInformation("Request to add server from {IPAddress}", requestIpAddress);

            var serverInfoEntity = mapper.Map<Services.ServerInfo>(serverInfo);
            serverInfoEntity.HostIpAddress = requestIpAddress.ToString();
            serverInfoEntity.LastHeartbeat = DateTime.UtcNow;

            await this.serverRegistryService.AddServer(serverInfoEntity);

            this.logger.LogInformation("Added server successfully for {IPAddress}", requestIpAddress);
        }

        private string GetRequestIpAddress()
        {
            return this.httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }
    }
}
