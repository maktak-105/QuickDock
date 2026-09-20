@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
"%CSC%" /nologo /t:winexe /out:"%~dp0QuickDockProto.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll "%~dp0DockProto.cs"
if errorlevel 1 exit /b 1
echo built %~dp0QuickDockProto.exe
