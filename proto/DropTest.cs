using System;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

internal static class DropTest
{
    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extra);

    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP = 0x0004;

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("usage: DropTest.exe <file> <x> <y>");
            return 2;
        }
        string path = args[0];
        int dx = int.Parse(args[1]);
        int dy = int.Parse(args[2]);
        if (!File.Exists(path))
        {
            Console.WriteLine("missing " + path);
            return 3;
        }

        var data = new DataObject();
        var list = new StringCollection();
        list.Add(path);
        data.SetFileDropList(list);

        var dummy = new Form
        {
            Opacity = 0.05,
            Width = 60,
            Height = 60,
            Left = 40,
            Top = 40,
            StartPosition = FormStartPosition.Manual,
            ShowInTaskbar = false,
            Text = "DropSrc"
        };
        dummy.Shown += (s, e) =>
        {
            var mover = new Thread(() =>
            {
                Thread.Sleep(200);
                int sx = dummy.Left + 20, sy = dummy.Top + 20;
                for (int i = 1; i <= 8; i++)
                {
                    SetCursorPos(sx + (dx - sx) * i / 8, sy + (dy - sy) * i / 8);
                    Thread.Sleep(30);
                }
                SetCursorPos(dx, dy);
                Thread.Sleep(250);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(200);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            });
            mover.IsBackground = true;
            mover.Start();

            SetCursorPos(dummy.Left + 20, dummy.Top + 20);
            Thread.Sleep(80);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            var result = dummy.DoDragDrop(data, DragDropEffects.Copy);
            Console.WriteLine("DoDragDrop=" + result);
            dummy.Close();
        };
        Application.Run(dummy);
        return 0;
    }
}
