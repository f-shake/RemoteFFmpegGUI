param(
    [Parameter()]
    [switch]$w,
    [switch]$d,
    [switch]$s,
    [switch]$f
)
try {
    
    Write-Output "-w：生成Web（Web、WebAPI、Host）"
    Write-Output "-d：生成WPF"
    Write-Output ""  
    
    Write-Output "请先阅读ReadMe"
    Write-Output "请确保："
    Write-Output "已经安装npm（Node.JS）"
    Write-Output "已经安装.NET 10 SDK"
    Write-Output "已经将ffmpeg相关二进制文件、MediaInfo.dll、性能测试视频（若需要）放置到./bin中"

    pause
    Clear-Host

    
    if(!$w -and !$d){
        $w = $true
        $d = $true
    }
    
    if (!(Test-Path bin)) {
        throw "不存在bin目录"
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
    
    Clear-Host
    if (Test-Path Generation/Publish) {
        Remove-Item Generation/Publish -Recurse
    }
    
    if ($w) {
        mkdir -Force Generation/Publish/WebPackage
        mkdir -Force Generation/Publish/WebPackage/api
        mkdir -Force Generation/Publish/WebPackage/host

        Clear-Host
        
        Write-Output "正在发布Web"
        Set-Location SimpleFFmpegGUI.Web
        npm install
        npm run build
        Set-Location ..
        Write-Output "正在复制Web"
        Copy-Item SimpleFFmpegGUI.Web/dist/* Generation/Publish/WebPackage -Force -Recurse

        Write-Output "正在发布WebAPI"
        dotnet publish SimpleFFmpegGUI.WebAPI -c Release -o Generation/Publish/WebPackage/api

        Write-Output "正在发布Host"
        dotnet publish SimpleFFmpegGUI.Host -c Release -o Generation/Publish/WebPackage/host
        dotnet publish SimpleFFmpegGUI.Host.Console -c Release -o Generation/Publish/WebPackage/host
        dotnet publish SimpleFFmpegGUI.Host.WindowsService -c Release -o Generation/Publish/WebPackage/host
        Copy-Item SimpleFFmpegGUI.Host.WindowsService/*.bat Generation/Publish/WebPackage/host

        
        Write-Output "正在复制二进制库"
        Copy-Item bin/* Generation/Publish/WebPackage/host -Force -Recurse
        
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
            
            
        

    
    Write-Output "正在清理"
    Remove-Item Generation/Release -Recurse

    Write-Output "操作完成，生成的文件位于Generation/Publish"

    Invoke-Item Generation/Publish
    pause
}
catch {
    Write-Error $_
}