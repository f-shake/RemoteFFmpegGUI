@echo off

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo need Administrator
    
    powershell -Command "Start-Process '%~0' -Verb RunAs"
    exit /b
)


set "service_name=SimpleFFmpegService"
sc stop %service_name%
sc delete %service_name%

echo Service has been deleted.
pause


