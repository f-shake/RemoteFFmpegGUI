using FzLib.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Model;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Configurations;

namespace SimpleFFmpegGUI.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class FFmpegControllerBase : ControllerBase
{
}