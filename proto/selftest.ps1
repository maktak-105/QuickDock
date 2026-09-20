$ErrorActionPreference = 'Stop'
$proto = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $proto
$exe = Join-Path $root 'dist\QuickDock.exe'
$cfg = Join-Path $env:APPDATA 'QuickDock'
$flag = Join-Path $env:TEMP 'quickdock-launched.txt'
$probe = Join-Path $proto 'probe.bat'
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'

function Fail($m) { throw $m }
function Log($m) { Write-Output $m }

Stop-Process -Name QuickDock,QuickDockProto,DropTest -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500
& cmd /c "$root\scripts\build.bat"
if ($LASTEXITCODE -ne 0) { Fail 'build dock' }
& $csc /nologo /t:exe /out:"$proto\DropTest.exe" /r:System.Windows.Forms.dll "$proto\DropTest.cs"
if ($LASTEXITCODE -ne 0) { Fail 'build droptest' }

New-Item -ItemType Directory -Force -Path $cfg | Out-Null
Set-Content -Path (Join-Path $cfg 'items.txt') -Value $null -Encoding ASCII
$pins = Join-Path $cfg 'pins'
New-Item -ItemType Directory -Force -Path $pins | Out-Null
Get-ChildItem $pins -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $cfg 'drop.log') -Force -ErrorAction SilentlyContinue
Remove-Item $flag -Force -ErrorAction SilentlyContinue

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class ST8 {
  public delegate bool CB(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(CB cb, IntPtr l);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint procId);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [StructLayout(LayoutKind.Sequential)]
  public struct POINT { public int X; public int Y; }
  [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT pt);
  public struct RECT { public int L,T,R,B; }
  public static bool Find(uint pid, out RECT r, out int w, out int h, out IntPtr win) {
    RECT found = new RECT(); IntPtr fh = IntPtr.Zero; bool ok=false;
    EnumWindows((wh, l) => {
      uint wp; GetWindowThreadProcessId(wh, out wp);
      if (wp != pid) return true;
      RECT rr; GetWindowRect(wh, out rr);
      int ww = rr.R-rr.L, hh = rr.B-rr.T;
      if (IsWindowVisible(wh) && ww>=40 && hh>=40 && ww<1200) { found = rr; fh = wh; ok = true; }
      return true;
    }, IntPtr.Zero);
    r = found; w = found.R-found.L; h = found.B-found.T; win = fh; return ok;
  }
}
"@
[void][ST8]::SetCursorPos(200, 200)

Start-Process $exe
Start-Sleep -Seconds 1
$procId = [uint32](Get-Process QuickDock).Id
$r = New-Object ST8+RECT; $w=0; $h=0; $hwnd = [IntPtr]::Zero
if (-not [ST8]::Find($procId, [ref]$r, [ref]$w, [ref]$h, [ref]$hwnd)) { Fail 'dock window missing' }
Log "T1 collapsed ${w}x${h} L=$($r.L) R=$($r.R)"
if ($h -gt 80) { Fail "not icon height: $h" }
if ($w -gt 80) { Fail "not icon width at rest: $w" }
Log 'PASS T1 icon-size'
$dx0 = [int](($r.L + $r.R) / 2)
$dy0 = [int](($r.T + $r.B) / 2)
$ptHit = New-Object ST8+POINT
$ptHit.X = $dx0; $ptHit.Y = $dy0
$hit = [ST8]::WindowFromPoint($ptHit)
$hitPid = 0
[void][ST8]::GetWindowThreadProcessId($hit, [ref]$hitPid)
Log "T1b hit pid=$hitPid dock=$procId at $dx0,$dy0"
if ($hitPid -ne $procId) { Fail "hit-test not dock (pid $hitPid)" }
Log 'PASS T1b hit-test'

[void][ST8]::SetForegroundWindow($hwnd)
$desk = [Environment]::GetFolderPath('Desktop')
$lnk = Join-Path $desk 'QuickDockProbe.lnk'
$sh = New-Object -ComObject WScript.Shell
$sc = $sh.CreateShortcut($lnk)
$sc.TargetPath = $probe
$sc.Save()

$got = $false
$items = @()
for ($try = 1; $try -le 3 -and -not $got; $try++) {
    [void][ST8]::Find($procId, [ref]$r, [ref]$w, [ref]$h, [ref]$hwnd)
    [void][ST8]::SetCursorPos($r.L + 20, [int](($r.T + $r.B) / 2))
    Start-Sleep -Milliseconds 350
    [void][ST8]::Find($procId, [ref]$r, [ref]$w, [ref]$h, [ref]$hwnd)
    $dx = $r.L + 24
    $dy = [int](($r.T + $r.B) / 2)
    Log "T2 try $try drop=$dx,$dy size=${w}x${h}"
    [void][ST8]::SetForegroundWindow($hwnd)
    $dp = Start-Process -FilePath "$proto\DropTest.exe" -ArgumentList @($lnk, "$dx", "$dy") -Wait -PassThru -NoNewWindow
    if ($dp.ExitCode -eq $null) { }
    Start-Sleep -Milliseconds 400
    $items = @()
    if (Test-Path (Join-Path $cfg 'items.txt')) {
        $items += @(Get-Content (Join-Path $cfg 'items.txt') -ErrorAction SilentlyContinue | Where-Object { $_.Trim() })
    }
    if (Test-Path $pins) {
        $items += @(Get-ChildItem $pins -File | ForEach-Object { $_.FullName })
    }
    foreach ($i in $items) { if ($i -like '*QuickDockProbe.lnk' -or $i -like '*probe.bat') { $got = $true } }
}
Log "T2 items=$($items -join ' | ')"
if (-not $got) { Fail "register fail items=$($items -join ',')" }
Log 'PASS T2 register-drop'

[void][ST8]::SetCursorPos(200, 200)
Start-Sleep -Milliseconds 2800
[void][ST8]::Find($procId, [ref]$r, [ref]$w, [ref]$h, [ref]$hwnd)
$w0 = $w; $right0 = $r.R
[void][ST8]::SetCursorPos($r.L + 20, [int](($r.T+$r.B)/2))
Start-Sleep -Milliseconds 400
[void][ST8]::Find($procId, [ref]$r, [ref]$w, [ref]$h, [ref]$hwnd)
Log "T3 expand $w0 -> $w  right $right0 -> $($r.R)"
if ($w -le 80) { Fail "did not grow" }
Log 'PASS T3 expand'

$ix = $r.L + 24; $iy = [int](($r.T+$r.B)/2)
[void][ST8]::SetCursorPos($ix, $iy)
Start-Sleep -Milliseconds 200
[ST8]::mouse_event(2,0,0,0,[UIntPtr]::Zero)
[ST8]::mouse_event(4,0,0,0,[UIntPtr]::Zero)
Start-Sleep -Milliseconds 900
if (-not (Test-Path $flag)) { Fail 'launch fail' }
Log 'PASS T4 click-launch'

Remove-Item $lnk -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $pins 'QuickDockProbe.lnk') -Force -ErrorAction SilentlyContinue
Log 'ALL PASS'
