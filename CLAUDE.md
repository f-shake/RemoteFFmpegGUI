# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

FFmpeg 工具箱（RemoteFFmpegGUI）：一个 FFmpeg 的 Web（Vue 3 + ASP.NET）与 WPF 桌面双端 GUI，支持视频转码、拼接、裁剪、编码性能测试等。**代码注释、日志消息、UI 文本以及 README 均为中文**——新增的注释/界面文案请同样使用中文。

解决方案：`SimpleFFmpegGUI.sln`（.NET 10、C#），另含一个 Vue 3 前端项目与一个 WebTest 集成测试项目。

## 构建与开发命令

- 需要 .NET 10 SDK 与 npm。NuGet 包版本集中管理在 `Directory.Packages.props`（不要在 csproj 中直接写版本号）。
- `./build.ps1 -w`：发布 Web 前端 + WebAPI 到 `Generation/Publish/WebPackage`；`-d`：发布 WPF（单文件版 + 自包含版）到 `Generation/Publish/WPF_*`；不加参数时两者都执行。前提是根目录 `bin/` 中已放置 ffmpeg 共享库二进制、`MediaInfo.exe`，以及（如需编码测试）`test.mp4` 和 VMAF 模型——具体目录结构见 ReadMe。若 PowerShell 报"禁止执行脚本"，先以管理员运行 `set-executionpolicy remotesigned`。
- 快速编译：`dotnet build SimpleFFmpegGUI.sln`（各项目输出路径重定向到 `Generation/bin/Debug/<项目名>` / `Generation/bin/Release/<项目名>`，不是各自的 `bin/`）。
- Web 开发：`cd SimpleFFmpegGUI.Web && npm run dev`（API 直连 `http://localhost:5001`）；`npm run build`。**无 lint 脚本**（未配置 ESLint）。
- `clean.ps1` 清理 `Generation` 目录。

## 架构

共 5 个 .NET 项目 + 1 个 Vue 项目。**所有 FFmpeg 与业务逻辑都在 `SimpleFFmpegGUI.Core` 中**，其余 .NET 项目只是入口或薄适配层。

```
Web (Vue3) ──HTTP──> WebAPI ──执行──> ffmpeg
WPF（进程内直接使用 Core；也可通过 HTTP 向远程 WebAPI 提交任务）
```

- **Core**：各 Service、EF Core SQLite 数据库、Dto、FFmpeg 参数生成、编解码器库。
  - `Services/`：`TaskService`（任务增删改查）、`QueueService`（队列循环、定时计划、暂停/取消、独立任务，任务管理器为并发字典）、`FFmpegTaskService`（单任务 ffmpeg 进程：进度/PSNR/SSIM/VMAF 解析、二次编码、暂停恢复、取消）、`ConfigService`（config.json 配置：默认进程优先级、快照尺寸）、`PresetService`、`DbLoggerService`（数据库日志，10 秒周期落库 + 事件）、`PowerService`（CPU 占用、队列完成后关机）、`MediaInfoService`（MediaInfo.exe 与截图）、`AppLifetimeService`（启动时 ffmpeg 目录配置与遗留 Processing 任务复位）、`FtpService`（按目录启停的 FTP）。
  - `FFmpegArgument/`：静态类 `ArgumentsGenerator` 根据 `OutputParameters` 拼接 ffmpeg 命令行；按类别拆分的生成器（`VideoArgumentsGenerator`、`AudioArgumentsGenerator`、`StreamArgumentsGenerator` 等）。
  - `FFmpegLib/`：编解码器类（`X264`、`X265`、`XVP9`、`AomAV1`、`SVTAV1`，硬件编码 `N_H264`/`N_H265`/`N_AV1`，通用 `General*`），继承自 `CodecBase`/`VideoCodec`/`AudioCodec`，产出 `FFmpegArgumentItem`。`VideoFormat.Formats` 是容器格式列表。
  - `Models/`：`Entities/TaskEntity`（状态 `Queue`/`Processing`/`Done`/`Error`/`Cancel`，软删除用 `IsDeleted`）、`Entities/PresetEntity`、`Entities/LogEntity`。`Inputs`/`Parameters` 通过 `EFJsonConverter` 以 JSON 形式存入 SQLite。`MediaParameters/` 是前后端共享的参数模型（`OutputParameters` 含 `Video`/`Audio`/`Mux`/`Stream`/`ProcessedOperationParameters`）。
  - `Compatibility/`：`DatabaseMigrator`（v1.1→v2.0 手工迁移，含 Custom 枚举 3→99、迁移用户配置到 config.json）、`PresetConverter`（v1 预设 JSON 导入转换）。
  - 依赖注入：`DependencyInjectionExtension.cs` 中的 `AddFFmpegServices()` 注册 DbContext 和各 Service（QueueService/ConfigService/PowerService/DbLoggerService 为单例，其余为瞬时），并注册 `DbLoggerService`/`AppLifetimeService` 为 HostedService（WPF 未用 Host 管线，在 `App.xaml.cs` 手动启动它们）。WebAPI 与 WPF 都调用它——两者的进程内行为完全一致。
- **WebAPI**：ASP.NET Core 10 minimal hosting（无 Host/NamedPipe/Furion，ffmpeg 由 WebAPI 进程直接执行）。`AppActionFilter`（全局 Action 过滤器）校验 `Authorization: Bearer {Token}` 头（Token 为空则不做鉴权），并统一处理控制器异常（HttpStatusCodeException 保留状态码，其余异常返回通用 500）。关键配置：`InputDir`/`OutputDir`（相对部署目录）、`Token`、`FFmpegDir`。文件操作只接受相对路径（绝对路径与 `..` 穿越被 `FilePathHelper` 拒绝）。`CreateWindowsService.bat` 可将 WebAPI 注册为 Windows 服务。
- **Web**：Vue 3 + TypeScript + Element Plus + Vite。`src/api.ts` 是完整的 API 映射（每个接口一个函数）。开发环境通过 CORS 直连 `http://localhost:5001`，生产构建请求相对路径 `api/{controller}`。新增任务相关视图在 `src/views/Add/`。
- **WPF**：进程内直连 Core（自带队列执行、自带工作目录下的 SQLite）。MVVM 采用 `CommunityToolkit.Mvvm`（`[ObservableProperty]`），界面为 WinUI 风格（`iNKORE.UI.WPF.Modern`），DI 配置在 `App.xaml.cs` 的 `ConfigureServices`。`CutWindow` 用 FFME（`Unosquare.FFME`）做视频预览，使用较旧的 `ffmpeg_FFME` 二进制目录。`Config.cs`（`config.json`，含 `RemoteHosts` 列表：Address + Token）支持把任务提交到远程 WebAPI 服务器而不是本地执行。

## 开发注意事项

- `libs/` 目录已完全移除。Enterwell 通知控件（`Enterwell.Clients.Wpf.Notifications` v1.4.2 修改版）已内化源码到 `SimpleFFmpegGUI.WPF/Enterwell/`（**保留原命名空间**，主题字典 `SimpleFFmpegGUI.WPF/Themes/Generic.xaml` 由 AssemblyInfo.cs 的 ThemeInfo 加载，源码来自 https://github.com/f-shake/Wpf.Notifications）。FzLib 采用双轨：WPF 内化老版 FzLib 源码（`SimpleFFmpegGUI.WPF/FzLib/`，命名空间 `SimpleFFmpegGUI.WPF.FzLib.*`，来自 FzLib 仓库 v1_final 标签），其余项目通过 NuGet 引用新版 FzLib（版本在 `Directory.Packages.props`）。
- 根目录 `bin/`（git 忽略）存放 ffmpeg/MediaInfo 运行时，执行任何 ffmpeg 相关操作都依赖它。有两套 ffmpeg：`ffmpeg/`（当前版本，编码用）与 `ffmpeg_FFME`（旧版 6.1.x，仅供 WPF FFME 预览用）。
- SQLite `db.sqlite` 手工版本管理：v2 用 `Compatibility/DatabaseMigrator.MigrateIfNeeded()`（检测 v1 库 → 备份 → 重命名列/转换 JSON/修复 Custom 枚举/迁移用户配置 → DROP Configs 表 → 写 `_MigrationHistory`），随后 `EnsureCreated()`。并非 EF Migrations。
- `日志.md` 是持续更新的开发日志（中文）——动手前可读近期条目了解现状的来龙去脉。
- 历史沿革：v2（master_v2 分支）已完成"原生 ASP.NET Core 10、去掉 Furion、取消 Host（WebAPI 直接执行 ffmpeg）"，并**决定保留 WPF**（Avalonia 重构搁置）；Postgres 支持仍未实施。
