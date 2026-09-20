$ErrorActionPreference = 'Stop'
$exe = Join-Path $PSScriptRoot 'QuickDockProto.exe'
$cfgDir = Join-Path $env:APPDATA 'QuickDock'
New-Item -ItemType Directory -Path $cfgDir -Force | Out-Null
@(
  "$env:WINDIR\System32\notepad.exe",
  "$env:WINDIR\System32\calc.exe"
) | Set-Content -Path (Join-Path $cfgDir 'items.txt') -Encoding UTF8

Stop-Process -Name QuickDockProto -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 400
Start-Process $exe
Start-Sleep -Seconds 1

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class HT {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  public struct RECT { public int L,T,R,B; }
}
"@

$p = Get-Process QuickDockProto
if (-not $p) { throw 'process missing' }
$r = New-Object HT+RECT
[void][HT]::GetWindowRect($p.MainWindowHandle, [ref]$r)
$w0 = $r.R - $r.L
$h0 = $r.B - $r.T
Write-Output "COLLAPSED L=$($r.L) T=$($r.T) R=$($r.R) B=$($r.B) w=$w0 h=$h0"

if ($w0 -gt 80) { throw "collapsed too wide: $w0" }
if ($h0 -gt 80) { throw "collapsed too tall: $h0" }

$cx = [int](($r.L + $r.R) / 2)
$cy = [int](($r.T + $r.B) / 2)
[void][HT]::SetCursorPos($cx, $cy)
Start-Sleep -Milliseconds 250
[void][HT]::GetWindowRect($p.MainWindowHandle, [ref]$r)
$w1 = $r.R - $r.L
Write-Output "EXPANDED L=$($r.L) T=$($r.T) R=$($r.R) B=$($r.B) w=$w1 h=$($r.B-$r.T)"

if ($w1 -le $w0) { throw "did not expand: $w0 -> $w1" }
if ($r.R -lt $r.L + 100) { throw "not a row of icons" }
Write-Output "PASS hover-expand"
