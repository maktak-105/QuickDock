@echo off
setlocal
cd /d "%~dp0\.."
if not exist dist mkdir dist
set DOTNET=
if exist "%ProgramFiles%\dotnet\dotnet.exe" set DOTNET=%ProgramFiles%\dotnet\dotnet.exe
if exist "%ProgramFiles(x86)%\dotnet\dotnet.exe" set DOTNET=%ProgramFiles(x86)%\dotnet\dotnet.exe
if "%DOTNET%"=="" set DOTNET=dotnet
"%DOTNET%" publish src\app\QuickDock.csproj -c Release -o dist --nologo
if errorlevel 1 exit /b 1
echo built dist\QuickDock.exe
endlocal
