using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Native.SetProcessDPIAware();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new DockForm());
    }
}

internal sealed class DockForm : Form
{
    const int Cell = 48;
    const int Pad = 4;

    static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDock");
    static readonly string ConfigPath = Path.Combine(ConfigDir, "items.txt");

    readonly List<string> _items = new List<string>();
    readonly Timer _poll;
    readonly Panel _hit;
    bool _expanded;

    public DockForm()
    {
        Text = "QuickDock";
        FormBorderStyle = FormBorderStyle.None;
        AutoScaleMode = AutoScaleMode.None;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(16, 20, 28);
        AllowDrop = true;
        DoubleBuffered = true;
        Padding = new Padding(0);

        _hit = new Panel
        {
            Dock = DockStyle.Fill,
            AllowDrop = true,
            BackColor = Color.FromArgb(16, 20, 28)
        };
        _hit.DragEnter += OnDrag;
        _hit.DragOver += OnDrag;
        _hit.DragDrop += OnDrop;
        _hit.MouseClick += OnClick;
        _hit.MouseMove += (s, e) => { if (!_expanded) { _expanded = true; LayoutDock(); } };
        _hit.Paint += (s, e) => PaintDock(e.Graphics);
        Controls.Add(_hit);

        LoadItems();
        _expanded = false;
        LayoutDock();
        Dlog("start items=" + _items.Count + " w=" + Width);
        Shown += (s, e) =>
        {
            AllowDrop = true;
            _hit.AllowDrop = true;
            Native.DragAcceptFiles(Handle, true);
            Native.DragAcceptFiles(_hit.Handle, true);
            LayoutDock();
        };

        DragEnter += OnDrag;
        DragOver += OnDrag;
        DragDrop += OnDrop;

        _poll = new Timer { Interval = 50 };
        _poll.Tick += (s, e) => PollCursor();
        _poll.Start();

        FormClosed += (s, e) => _poll.Stop();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Native.DragAcceptFiles(Handle, true);
    }

    void OnDrag(object sender, DragEventArgs e)
    {
        Dlog("OnDrag present=" + (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop, true)));
        e.Effect = DragDropEffects.Copy;
    }

    void OnDrop(object sender, DragEventArgs e)
    {
        Dlog("OnDrop formats=" + string.Join(",", e.Data.GetFormats()));
        var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];
        if (files == null) return;
        foreach (var f in files) AddPath(f);
        _expanded = true;
        LayoutDock();
        Invalidate();
    }

    void PollCursor()
    {
        var pt = Cursor.Position;
        bool over = Bounds.Contains(pt);
        var wa = Screen.PrimaryScreen.WorkingArea;
        bool nearEdge = pt.X >= wa.Right - 36
            && pt.Y >= Top - 16 && pt.Y <= Bottom + 16;
        if (over || nearEdge)
        {
            if (!_expanded)
            {
                _expanded = true;
                LayoutDock();
            }
        }
        else if (_expanded)
        {
            _expanded = false;
            LayoutDock();
        }
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_DROPFILES = 0x0233;
        if (m.Msg == WM_DROPFILES)
        {
            var dropped = Native.DroppedFiles(m.WParam);
            Dlog("WM_DROPFILES n=" + dropped.Length);
            foreach (var p in dropped)
                AddPath(p);
            Native.DragFinish(m.WParam);
            _expanded = true;
            LayoutDock();
            Invalidate();
            return;
        }
        base.WndProc(ref m);
    }

    int Count()
    {
        return 1 + (_expanded ? Math.Max(_items.Count, 0) : 0);
    }

    void LayoutDock()
    {
        int n = 1 + (_expanded ? _items.Count : 0);
        int w = Pad * 2 + Cell * Math.Max(n, 1);
        int h = Pad * 2 + Cell;
        var wa = Screen.PrimaryScreen.WorkingArea;
        int x = wa.Right - w;
        int y = wa.Top + Math.Max(0, (wa.Height - h) / 2);
        if (IsHandleCreated)
        {
            Native.SetWindowPos(Handle, new IntPtr(-1), x, y, w, h, 0x0040);
            Dlog("layout n=" + n + " set=" + w + "x" + h + " at " + x + "," + y + " wa.R=" + wa.Right + " propW=" + Width);
        }
        else
        {
            Bounds = new Rectangle(x, y, w, h);
        }
        if (_hit != null) _hit.Invalidate();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        PaintDock(e.Graphics);
    }

    void PaintDock(Graphics g)
    {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        using (var edge = new Pen(Color.FromArgb(0, 220, 230), 1))
            g.DrawRectangle(edge, 0, 0, Width - 1, Height - 1);

        int n = 1 + (_expanded ? _items.Count : 0);
        for (int vis = 0; vis < n; vis++)
        {
            int x = Pad + vis * Cell;
            int y = Pad;
            var r = new Rectangle(x + 4, y + 4, Cell - 8, Cell - 8);
            if (vis == n - 1)
            {
                using (var b = new SolidBrush(Color.FromArgb(0, 200, 210)))
                    g.FillEllipse(b, r);
            }
            else
            {
                Icon ic = Native.FileIcon(_items[vis]);
                if (ic != null)
                {
                    g.DrawIcon(ic, new Rectangle(x + 8, y + 8, 32, 32));
                    ic.Dispose();
                }
                else
                {
                    using (var b = new SolidBrush(Color.FromArgb(50, 60, 80)))
                        g.FillRectangle(b, r);
                }
            }
        }
    }

    void OnClick(object sender, MouseEventArgs e)
    {
        int vis = (e.X - Pad) / Cell;
        int n = 1 + (_expanded ? _items.Count : 0);
        if (vis < 0 || vis >= n) return;
        if (vis == n - 1) return;
        if (e.Button == MouseButtons.Left)
            Launch(_items[vis]);
        else if (e.Button == MouseButtons.Right)
        {
            _items.RemoveAt(vis);
            SaveItems();
            LayoutDock();
        }
    }

    static void Dlog(string msg)
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            File.AppendAllText(Path.Combine(ConfigDir, "drop.log"),
                DateTime.Now.ToString("HH:mm:ss.fff ") + msg + Environment.NewLine);
        }
        catch { }
    }

    void AddPath(string path)
    {
        Dlog("AddPath in=" + path);
        try { path = Path.GetFullPath(path); } catch { Dlog("bad path"); return; }
        if (!File.Exists(path) && !Directory.Exists(path)) { Dlog("missing " + path); return; }
        if (_items.Contains(path)) { Dlog("dup " + path); return; }
        _items.Add(path);
        SaveItems();
        Dlog("saved " + path);
    }

    static void Launch(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch { }
    }

    void LoadItems()
    {
        Directory.CreateDirectory(ConfigDir);
        if (!File.Exists(ConfigPath)) return;
        foreach (var line in File.ReadAllLines(ConfigPath))
        {
            var p = line.Trim();
            if (p.Length > 0 && (File.Exists(p) || Directory.Exists(p)) && !_items.Contains(p))
                _items.Add(p);
        }
    }

    void SaveItems()
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllLines(ConfigPath, _items.ToArray());
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            return cp;
        }
    }
}

internal static class Native
{
    const int SHGFI_ICON = 0x100;
    const int SHGFI_SMALLICON = 0x1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SHGetFileInfo(string pszPath, uint fa, ref SHFILEINFO psfi, uint cb, uint flags);

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    public static extern bool SetProcessDPIAware();

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("shell32.dll")]
    public static extern void DragAcceptFiles(IntPtr hWnd, bool fAccept);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern uint DragQueryFile(IntPtr hDrop, uint iFile, IntPtr lpszFile, uint cch);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "DragQueryFileW")]
    static extern uint DragQueryFileSb(IntPtr hDrop, uint iFile, System.Text.StringBuilder lpszFile, uint cch);

    [DllImport("shell32.dll")]
    public static extern void DragFinish(IntPtr hDrop);

    [DllImport("kernel32.dll")]
    static extern IntPtr GlobalLock(IntPtr h);

    [DllImport("kernel32.dll")]
    static extern bool GlobalUnlock(IntPtr h);

    public static string[] DroppedFiles(IntPtr hDrop)
    {
        var sb = new System.Text.StringBuilder(32768);
        uint n = DragQueryFile(hDrop, 0xFFFFFFFF, IntPtr.Zero, 0);
        if (n == 0)
        {
            IntPtr p = GlobalLock(hDrop);
            if (p == IntPtr.Zero) return new string[0];
            try
            {
                int pFiles = Marshal.ReadInt32(p);
                int wide = Marshal.ReadInt32(p, 16);
                IntPtr str = new IntPtr(p.ToInt64() + pFiles);
                var list = new System.Collections.Generic.List<string>();
                if (wide != 0)
                {
                    while (true)
                    {
                        string s = Marshal.PtrToStringUni(str);
                        if (string.IsNullOrEmpty(s)) break;
                        list.Add(s);
                        str = new IntPtr(str.ToInt64() + (s.Length + 1) * 2);
                    }
                }
                return list.ToArray();
            }
            finally { GlobalUnlock(hDrop); }
        }
        var arr = new string[n];
        for (uint i = 0; i < n; i++)
        {
            sb.Length = 0;
            DragQueryFileSb(hDrop, i, sb, (uint)sb.Capacity);
            arr[i] = sb.ToString();
        }
        return arr;
    }

    public static Icon FileIcon(string path)
    {
        var sh = new SHFILEINFO();
        SHGetFileInfo(path, 0, ref sh, (uint)Marshal.SizeOf(sh), SHGFI_ICON | SHGFI_SMALLICON);
        if (sh.hIcon == IntPtr.Zero) return null;
        try
        {
            return (Icon)Icon.FromHandle(sh.hIcon).Clone();
        }
        finally
        {
            DestroyIcon(sh.hIcon);
        }
    }
}
