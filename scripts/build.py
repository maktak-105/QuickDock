#!/usr/bin/env python3
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CSC = Path(r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe")
OUT = ROOT / "dist" / "QuickDock.exe"
SRC = ROOT / "src" / "app" / "QuickDock.cs"


def main() -> int:
    (ROOT / "dist").mkdir(exist_ok=True)
    cmd = [
        str(CSC),
        "/nologo",
        "/t:winexe",
        f"/out:{OUT}",
        "/r:System.Windows.Forms.dll",
        "/r:System.Drawing.dll",
        str(SRC),
    ]
    r = subprocess.run(cmd)
    if r.returncode == 0:
        print("built", OUT)
    return r.returncode


if __name__ == "__main__":
    raise SystemExit(main())
