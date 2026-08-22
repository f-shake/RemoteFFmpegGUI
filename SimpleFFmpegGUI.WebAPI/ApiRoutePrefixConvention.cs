using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace SimpleFFmpegGUI.WebAPI;

/// <summary>
/// 给所有控制器的路由统一加 "/api" 前缀。
/// 前端以根路径提供 SPA 页面（history 路由），API 全部挂在 /api 下，二者互不冲突。
/// 否则 history 路由（如 /log、/preset）会与 /Log、/Preset 控制器（ASP.NET 路由大小写不敏感）碰撞，
/// 导致这些页面刷新时被 API 吞掉。该前缀同时复刻了原先 nginx 剥离 "/api" 的部署约定。
/// </summary>
public sealed class ApiRoutePrefixConvention : IApplicationModelConvention
{
    private const string Prefix = "api/";

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var selector in controller.Selectors)
            {
                // 说明：下方 string.IsNullOrEmpty(template) 分支会把"没有任何 [Route]、原本走常规路由"的控制器
                // 悄悄转成属性路由 api/[controller]。本项目所有控制器均继承 FFmpegControllerBase 的 [Route("[controller]")]，
                // 该分支当前不会命中；新增控制器时请显式声明 [Route("api/[controller]")]，避免依赖此兜底。
                selector.AttributeRouteModel ??= new AttributeRouteModel();
                var template = selector.AttributeRouteModel.Template;
                if (string.IsNullOrEmpty(template))
                {
                    selector.AttributeRouteModel.Template = Prefix + "[controller]";
                }
                else if (!template.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                {
                    selector.AttributeRouteModel.Template = Prefix + template;
                }
            }
        }
    }
}
