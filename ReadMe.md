# 远程 FFmpeg 工具箱

使用 Vue 3 + Element Plus + ASP.NET Core 构建的远程 FFmpeg Web GUI 程序，支持视频转码、拼接、合并、媒体信息查询等功能。

## 架构

| 项目名                 | 说明                                       |
| ---------------------- | ------------------------------------------ |
| Core                   | 核心库 — 实体模型、DTO、服务接口、日志等   |
| Host                   | 主机 — 通过 NamedPipe 向 WebAPI 提供服务    |
| Host.Console           | 主机（控制台入口）                          |
| Host.WindowsService    | 主机（Windows 服务入口）                    |
| WebAPI                 | 后端 API — ASP.NET Core 控制器              |
| Web                    | 前端 — Vue 3 + Element Plus + Vite          |

> WPF 桌面客户端已从当前分支移除。

## 截图

### 网页版

![](imgs/code.png)
![](imgs/info.png)
![](imgs/tasks.png)

## 部署 / 运行

### 获取程序包

在 [GitHub Releases](https://github.com/f-shake/RemoteFFmpegGUI/releases) 下载最新的发布包。

### 部署基于 Windows + IIS 的 Web 版本

1. 进入 `Generation/Publish/WebPackage`
2. 编辑 `api` 的 `appsettings.json`，主要修改 `InputDir` 和 `OutputDir` 项，指定输入和输出目录。其它修改项详见文件内的注释。
3. 在合适的位置新建一个网站文件夹，将 `Generation/Publish/WebPackage` 内的所有内容复制到新建的文件夹之中。
4. 确保安装了 .NET 10 Hosting Bundle，并在 Windows 中启用了 IIS。
5. 在 IIS 中新建网站，指定物理目录为之前新建的目录。右键其中的 `api` 目录，设置为虚拟应用程序。
6. 启动 Host。共有两种方式：
   - 运行 `SimpleFFmpegGUI.Host.Console.exe`，将打开一个控制台程序。
   - 在 Windows 系统中，可以运行 `CreateWindowsService.bat`（将自动申请管理员权限），将 Host 注册为自启动服务并立即启动。
7. 打开 IIS 中设置的 URL，检查网站运行是否正常。

**注意：**

- 若输入或输出文件夹位于网络位置等 IIS 无权限的位置，则需要：
  1. 设置 `appsettings.json` 中的 `InputDirAccessable` 和/或 `OutputDirAccessable` 为 `false`，告知程序无权限访问，那么后端将通过 Host 对文件进行访问。
  2. 这种模式下，HTTP 上传和下载功能将不可用。

### 直接运行

```bash
# 启动后端 API
cd SimpleFFmpegGUI.WebAPI
dotnet run

# 启动前端开发服务器（另一个终端）
cd SimpleFFmpegGUI.Web
npm install
npm run dev
```

前端开发服务器默认代理 API 请求到 `http://localhost:5001`。

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
- `-w`：生成 Web（Web + WebAPI + Host）
- `-d`：生成 WPF（标准、单文件、自包含）— 仅 `master` 分支可用

生成文件位于 `Generation/Publish` 下，其中 `WebPackage` 为 Web 部署包。

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
- ASP.NET Core
- Entity Framework Core + SQLite
- NamedPipe IPC（Host ↔ WebAPI）
- Token 认证

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
├── common.ts        # 通用工具函数
├── net.ts           # HTTP 请求封装
├── parameters.ts    # 固定参数
└── main.ts          # 入口文件
```

### API 设计

所有 API 仅使用 GET（查询）和 POST（写入）两种方法，路由统一风格。

### 注意事项

- `libs` 目录中的二进制文件来自于 [FzLib](https://github.com/autodotua/FzLib) 和 [Wpf.Notifications](https://github.com/autodotua/Wpf.Notifications)，均已开源。
