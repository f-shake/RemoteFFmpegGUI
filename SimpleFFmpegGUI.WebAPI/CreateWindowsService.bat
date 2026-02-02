@echo off

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo need Administrator
    
    powershell -Command "Start-Process '%~0' -Verb RunAs"
    exit /b
)


set "current_dir=%~dp0"
set "service_name=SimpleFFmpegService"
set "exe_path=%current_dir%SimpleFFmpegGUI.WebAPI.exe"

sc create %service_name% binPath= "%exe_path%" start= auto
sc start %service_name%

echo Service has been created and set to start automatically at system boot.
pause


