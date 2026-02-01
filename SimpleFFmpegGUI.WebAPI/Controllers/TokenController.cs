using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class TokenController : FFmpegControllerBase
    {
        public TokenController(
            ILogger<MediaInfoController> Logger, IConfiguration config, PipeClient pipeClient) : base(Logger, config, pipeClient) { }

        [HttpGet]
        [Route("Check")]
        public bool CheckToken(string token)
        {
            return config.GetValue<string>("Token") == token;
        }

        [HttpGet]
        [Route("Need")]
        public bool NeedToken()
        {
            return config.GetValue<string>("Token") != null;
        }
    }
}