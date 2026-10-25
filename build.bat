@echo off
cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
  echo .NET 8 SDK not found. Download it here: https://dotnet.microsoft.com/download/dotnet/8.0
  pause
  exit /b 1
)

dotnet publish src\CpuTempTray\CpuTempTray.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
if errorlevel 1 (
  pause
  exit /b 1
)

echo.
echo Done: %~dp0publish\CpuTempTray.exe
explorer "%~dp0publish"
pause
