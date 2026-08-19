param(
    [Parameter()]
    [switch]$w,
    [switch]$d
)
try {

    Write-Output "-w：生成Web（Web、WebAPI）"
    Write-Output "-d：生成WPF"
    Write-Output ""
    
    Write-Output "请先阅读ReadMe"
    Write-Output "请确保："
    Write-Output "已经安装npm（Node.JS）"
    Write-Output "已经安装.NET 10 SDK"
    Write-Output "已经将ffmpeg相关二进制文件、MediaInfo.exe、性能测试视频（若需要）放置到./bin中"

    # 非交互/输入重定向（如 CI）时自动跳过 pause
    if ($Host.UI.RawUI -and -not [Console]::IsInputRedirected) {
        pause
    }
    if (-not [Console]::IsInputRedirected) { Clear-Host }

    
    if(!$w -and !$d){
        $w = $true
        $d = $true
    }
    
    if (!(Test-Path bin)) {
        throw "不存在bin目录"
    }
    # 校验 ffmpeg/MediaInfo 二进制就位，避免产出缺运行库的残缺发布包
    if (!(Test-Path bin/ffmpeg/ffmpeg.exe)) {
        throw "bin/ffmpeg/ffmpeg.exe 不存在：请先将 ffmpeg 共享库二进制放入 bin/ffmpeg"
    }
    if (!(Test-Path bin/MediaInfo.exe)) {
        throw "bin/MediaInfo.exe 不存在：请先将 MediaInfo.exe 放入 bin/"
    }
    try {
        npm
    }
    catch {
        throw "不存在npm命令"
    }
    
    try {
        dotnet
    }
    catch {
        throw "未安装.NET SDK"
    }
    
    if (-not [Console]::IsInputRedirected) { Clear-Host }
    if (Test-Path Generation/Publish) {
        Remove-Item Generation/Publish -Recurse
    }
    
    if ($w) {
        mkdir -Force Generation/Publish/WebPackage
        mkdir -Force Generation/Publish/WebPackage/api

        if (-not [Console]::IsInputRedirected) { Clear-Host }

        Write-Output "正在发布Web"
        Set-Location SimpleFFmpegGUI.Web
        npm install
        npm run build
        Set-Location ..
        Write-Output "正在复制Web"
        Copy-Item SimpleFFmpegGUI.Web/dist/* Generation/Publish/WebPackage -Force -Recurse

        Write-Output "正在发布WebAPI"
        dotnet publish SimpleFFmpegGUI.WebAPI -c Release -o Generation/Publish/WebPackage/api

        Write-Output "正在复制Windows服务安装脚本"
        Copy-Item SimpleFFmpegGUI.WebAPI/CreateWindowsService.bat Generation/Publish/WebPackage/api
        Copy-Item SimpleFFmpegGUI.WebAPI/DeleteWindowsService.bat Generation/Publish/WebPackage/api

        Write-Output "正在复制二进制库"
        Copy-Item bin/* Generation/Publish/WebPackage/api -Force -Recurse

        Write-Output "正在清理"
        Remove-Item SimpleFFmpegGUI.Web/dist -Recurse
    }

    
    if ($d) {
        Write-Output "正在发布WPF（单文件）"
        dotnet publish SimpleFFmpegGUI.WPF -c Release -o Generation/Publish/WPF_SingleFile -r win-x64 --no-self-contained /p:PublishSingleFile=true
        Write-Output "正在复制二进制库"
        Copy-Item bin/* Generation/Publish/WPF_SingleFile -Force -Recurse  

        Write-Output "正在发布WPF（自包含）"
        dotnet publish SimpleFFmpegGUI.WPF -c Release -o Generation/Publish/WPF_SelfContained -r win-x64 --self-contained /p:PublishSingleFile=true
        Write-Output "正在复制二进制库"
        Copy-Item bin/* Generation/Publish/WPF_SelfContained -Force -Recurse
    }

    Write-Output "操作完成，生成的文件位于Generation/Publish"

    if (-not [Console]::IsInputRedirected) {
        Invoke-Item Generation/Publish
        pause
    }
}
catch {
    Write-Error $_
}