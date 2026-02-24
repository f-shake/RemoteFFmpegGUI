using FzLib.Web;
using Microsoft.AspNetCore.Mvc;

namespace SimpleFFmpegGUI.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class FFmpegControllerBase : ControllerBase
{
}

public static class ServiceResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this ServiceResult<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return new ObjectResult(result.Message)
        {
            StatusCode = (int)result.StatusCode
        };
    }

    public static IActionResult ToActionResult(this ServiceResult result)
    {
        if (result.IsSuccess)
        {
            return new OkResult();
        }

        return new ObjectResult(result.Message)
        {
            StatusCode = (int)result.StatusCode
        };
    }
}