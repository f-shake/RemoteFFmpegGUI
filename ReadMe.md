# 远程 FFmpeg 工具箱

使用 Vue 3 + Element Plus + ASP.NET Core 构建的 FFmpeg Web GUI 程序，支持视频转码、拼接、合并、媒体信息查询、编码性能测试等功能；另有 WPF 桌面客户端（进程内直连 Core，也可向远程 WebAPI 提交任务）。

## 架构

| 项目名                 | 说明                                                   |
| ---------------------- | ------------------------------------------------------ |
| Core                   | 核心库 — 实体模型、DTO、服务、FFmpeg 参数生成、数据库  |
| WebAPI                 | 后端 API — ASP.NET Core 控制器（直接执行 ffmpeg）      |
| Web                    | 前端 — Vue 3 + Element Plus + Vite                     |
| WPF                    | 桌面客户端（进程内直连 Core；支持提交到远程 WebAPI）   |
| WebTest                | WebAPI 集成测试（xUnit + WebApplicationFactory）       |
| Inkore.Extension       | WPF 用 iNKORE.UI.WPF.Modern 扩展与通用对话框           |

```
Web (Vue3) ──HTTP──> WebAPI ──执行──> ffmpeg
WPF ──（进程内直连 Core，自带队列与数据库）
WPF ──HTTP──> 远程 WebAPI（提交任务）
```

## 截图

### 网页版

![](imgs/code.png)
![](imgs/info.png)
![](imgs/tasks.png)

## 部署 / 运行

### 获取程序包

在 [GitHub Releases](https://github.com/f-shake/RemoteFFmpegGUI/releases) 下载最新的发布包。

### 部署 Web 版本

1. 进入 `Generation/Publish/WebPackage`
2. 编辑 `api` 的 `appsettings.json`，主要修改 `InputDir` 和 `OutputDir` 项（相对部署目录），指定输入和输出目录。**建议设置 `Token` 为强口令**（留空则不鉴权）。其它修改项详见文件内的注释。
3. 在合适的位置新建一个网站文件夹，将 `Generation/Publish/WebPackage` 内的所有内容复制到新建的文件夹之中。
4. 运行方式二选一：
   - 直接运行 `api/SimpleFFmpegGUI.WebAPI.exe`（控制台窗口）。
   - 在 Windows 系统中，右键 `api/CreateWindowsService.bat` 以管理员身份运行（将自动申请管理员权限），把 WebAPI 注册为自启动的 Windows 服务。
5. 打开浏览器访问 `http://localhost:5001`，检查服务是否正常（首页显示 "SimpleFFmpegGUI API is running!"）。
6. 前端页面为 `WebPackage` 根目录下的静态文件，需自行部署到 Web 服务器（如 IIS/Nginx），并把 API 请求代理到后端地址；或将前后端部署在同一个站点下（前端生产构建默认请求相对路径 `api/{controller}`）。

**注意：**

- WebAPI 与 WPF 单文件版均为 framework-dependent：目标机器需安装对应的 .NET 10 Runtime（WebAPI 需要 .NET 10 Runtime，WPF 单文件版需要 .NET 10 Desktop Runtime）；WPF 自包含版无需安装。
- FTP 服务（输入/输出目录的文件上传下载通道）默认关闭，可在前端文件服务页手动开启。FTP 采用匿名认证，请仅在可信网络环境中使用，或自行修改 `appsettings.json` 中的 `InputFtpPort`/`OutputFtpPort` 端口。

### 直接运行（开发）

```bash
# 启动后端 API（默认 http://localhost:5001）
cd SimpleFFmpegGUI.WebAPI
dotnet run

# 启动前端开发服务器（另一个终端）
cd SimpleFFmpegGUI.Web
npm install
npm run dev
```

前端开发环境通过 CORS 直连 `http://localhost:5001`。

### WPF 桌面客户端

运行 `SimpleFFmpegGUI.WPF.exe`（发布包内含 ffmpeg 运行库）。可在设置页配置远程主机（地址 + Token），将任务提交到远程 WebAPI 服务器执行。

## 构建

### 准备工作

1. 确保安装了 .NET 10 SDK
2. 确保安装了 Node.js 18+
3. 确保在根目录下的 `bin` 目录中放置了 FFmpeg 二进制文件（shared 版）：[下载](https://www.ffmpeg.org/download.html)
4. 若要使用媒体信息查询功能，应在根目录下的 `bin` 目录中放置 MediaInfo CLI 可执行文件：[下载](https://mediaarea.net/en/MediaInfo/Download)

`bin` 目录结构示例：

```
bin
├── MediaInfo.exe
├── ffmpeg
│   ├── ffmpeg.exe
│   ├── ffprobe.exe
│   └── *.dll
└── ffmpeg_FFME
    ├── ffmpeg.exe
    └── *.dll

> `ffmpeg_FFME` 用于 WPF 的视频裁剪预览功能，需使用 **FFmpeg 7.0 或更高版本**（当前为 **8.0.1**），直接复制 `ffmpeg` 目录的文件即可。
```

### 自动构建

执行 PowerShell：`./build.ps1`

参数：
- `-w`：生成 Web（Web 前端 + WebAPI）
- `-d`：生成 WPF（单文件版、自包含版）

不加参数时两者都执行。生成文件位于 `Generation/Publish` 下，其中 `WebPackage` 为 Web 部署包。

### 前端手动构建

```bash
cd SimpleFFmpegGUI.Web
npm install
npm run build      # 生产构建，输出到 dist/
```

## 开发

### 技术栈

**前端：**
- Vue 3（`<script setup>` 组合式 API）
- Element Plus（UI 组件库）
- Vite（构建工具）
- Vue Router（路由）
- Axios（HTTP 请求）
- Element Plus 图标（全局注册）

**后端：**
- ASP.NET Core 10（minimal hosting）
- Entity Framework Core + SQLite
- Serilog（文件日志）
- Token 认证（`Authorization: Bearer {Token}`，Token 为空时不鉴权）
- FubarDev.FtpServer（FTP 文件服务，默认关闭）

### 前端项目结构

```
SimpleFFmpegGUI.Web/src/
├── assets/          # 全局样式（global.css、page.css）
├── components/      # 通用 Vue 组件
│   ├── AddToTaskButtons.vue
│   ├── CodeArguments.vue
│   ├── CodeArgumentsDescription.vue
│   ├── FileIOGroup.vue
│   ├── FileSelect.vue
│   ├── JsonTree.vue
│   ├── StatusBar.vue
│   └── TimeInput.vue
├── views/           # 页面视图
│   ├── Add/         # 新建任务（转码、合并、对比、拼接、自定义）
│   ├── About.vue
│   ├── Files.vue
│   ├── Login.vue
│   ├── Logs.vue
│   ├── MediaInfo.vue
│   ├── Power.vue
│   ├── Presets.vue
│   ├── Tasks.vue
│   └── Welcome.vue
├── router/          # 路由配置
├── api.ts           # HTTP 请求封装
└── main.ts          # 入口文件
```

### API 设计

所有 API 仅使用 GET（查询）和 POST（写入）两种方法，路由统一风格。文件操作（下载、上传、媒体信息）只接受相对 `InputDir`/`OutputDir` 的路径，绝对路径与 `..` 穿越将被拒绝。

### 注意事项

- 第三方库均已内化源码，`libs` 目录已移除：[FzLib](https://github.com/autodotua/FzLib) 老版源码在 `SimpleFFmpegGUI.WPF/FzLib/`（其余项目通过 NuGet 引用新版）；[Wpf.Notifications](https://github.com/f-shake/Wpf.Notifications)（Enterwell 通知控件修改版，MIT，Copyright (c) 2017 Enterwell，许可证见 `SimpleFFmpegGUI.WPF/Enterwell/LICENSE`）在 `SimpleFFmpegGUI.WPF/Enterwell/`。
