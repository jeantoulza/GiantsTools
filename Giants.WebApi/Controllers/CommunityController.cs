﻿namespace Giants.WebApi.Controllers
{
    using Giants.DataContract.V1;
    using Giants.Services;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/[controller]")]
    public class CommunityController : ControllerBase
    {
        private readonly ICommunityService discordService;

        public CommunityController(
            ICommunityService communityService)
        {
            this.discordService = communityService;
        }

        [HttpGet]
        public CommunityStatus GetDiscordStatus()
        {
            return new CommunityStatus
            {
                CommunityAppName = "Discord",
                CommunityAppUri = this.discordService.GetDiscordUri()
            };
        }
    }
}
