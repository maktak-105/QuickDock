using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Windows.Forms;

namespace QuickDock;

internal sealed class MainForm : Form
{
    const int Cell = 56;
    const int Pad = 4;
    const int IconPx = 48;
    const int MaxItemCols = 6;

    static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDock");
    static readonly string PinsDir = Path.Combine(ConfigDir, "pins");
    static readonly string OrderPath = Path.Combine(ConfigDir, "order.txt");
    static readonly string ConfigPath = Path.Combine(ConfigDir, "items.txt");
    static readonly string SettingsPath = Path.Combine(ConfigDir, "settings.txt");
    const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string StartupValueName = "QuickDock";

    readonly List<string> _items = new();
    readonly Dictionary<string, Icon?> _icons = new(StringComparer.OrdinalIgnoreCase);
    readonly ToolTip _tip = new() { ShowAlways = true, InitialDelay = 0, ReshowDelay = 0, AutoPopDelay = 8000 };
    readonly NotifyIcon _tray;
    readonly Timer _poll;
    bool _expanded;
    bool _topMost = true;
    bool _menuOpen;
    bool _fileDragging;
    bool _dragHome;
    bool _didDrag;
    bool _reorder;
    int _dragItem = -1;
    int _hoverItem = -1;
    int _dragOffX, _dragOffY;
    int _homeX, _homeY;
    DateTime _collapseAt = DateTime.MaxValue;

    public MainForm()
    {
        Text = "QuickDock";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = true;
        BackColor = Color.FromArgb(16, 20, 28);
        MinimumSize = new Size(1, 1);
        DoubleBuffered = true;
        AllowDrop = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

        var appIcon = AppIcon.Create();
        Icon = appIcon;

        Directory.CreateDirectory(PinsDir);
        MigrateOldItems();
        LoadOrder();
        foreach (var item in _items) IconFor(item);
        LoadSettings();
        TopMost = _topMost;
        _expanded = false;
        LayoutDock();

        DragEnter += OnDragEnter;
        DragOver += OnDragOver;
        DragLeave += (_, _) => { _fileDragging = false; };
        DragDrop += OnDragDrop;
        MouseDown += OnDown;
        MouseMove += OnMove;
        MouseUp += OnUp;
        MouseLeave += (_, _) => HideTip();

        _poll = new Timer { Interval = 50 };
        _poll.Tick += (_, _) => PollCursor();
        _poll.Start();

        _tray = new NotifyIcon
        {
            Visible = true,
            Text = "QuickDock 1.0.0",
            Icon = appIcon,
            ContextMenuStrip = BuildMenu()
        };
        FormClosed += (_, _) =>
        {
            _poll.Stop();
            _tray.Visible = false;
            _tray.Dispose();
            foreach (var ico in _icons.Values) ico?.Dispose();
        };
        Dlog("start items=" + _items.Count);
    }

    void OnDragEnter(object? sender, DragEventArgs e)
    {
        _fileDragging = true;
        e.Effect = DragDropEffects.Copy;
        Dlog("WF DragEnter");
    }

    void OnDragOver(object? sender, DragEventArgs e)
    {
        _fileDragging = true;
        e.Effect = DragDropEffects.Copy;
    }

    void OnDragDrop(object? sender, DragEventArgs e)
    {
        _fileDragging = false;
        e.Effect = DragDropEffects.Copy;
        var files = GetFiles(e);
        Dlog("WF Drop n=" + files.Length);
        AcceptFiles(files);
    }

    static string[] GetFiles(DragEventArgs e)
    {
        try
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                return e.Data.GetData(DataFormats.FileDrop) as string[] ?? [];
        }
        catch (Exception ex) { Dlog("GetFiles " + ex.Message); }
        return [];
    }

    void AcceptFiles(string[] files)
    {
        foreach (var src in files)
        {
            try
            {
                if (!File.Exists(src) || Directory.Exists(src)) continue;
                var dest = UniqueDest(Path.GetFileName(src));
                File.Copy(src, dest);
                _items.Add(dest);
                IconFor(dest);
                Dlog("copied " + dest);
            }
            catch (Exception ex) { Dlog("copy " + ex.Message); }
        }
        SaveOrder();
        _expanded = true;
        LayoutDock();
        _hoverItem = -1;
        BeginInvoke(new Action(() => UpdateHoverTip(true)));
    }

    string UniqueDest(string name)
    {
        var dest = Path.Combine(PinsDir, name);
        if (!File.Exists(dest)) return dest;
        var stem = Path.GetFileNameWithoutExtension(name);
        var ext = Path.GetExtension(name);
        for (int i = 2; i < 1000; i++)
        {
            dest = Path.Combine(PinsDir, stem + " (" + i + ")" + ext);
            if (!File.Exists(dest)) return dest;
        }
        return Path.Combine(PinsDir, Guid.NewGuid().ToString("N") + ext);
    }

    int FirstRowItems() => Math.Min(MaxItemCols, _items.Count);

    int ColCount()
    {
        if (!_expanded) return 1;
        return _items.Count == 0 ? 1 : Math.Min(MaxItemCols + 1, FirstRowItems() + 1);
    }

    int RowCount()
    {
        if (!_expanded) return 1;
        int rest = Math.Max(0, _items.Count - FirstRowItems());
        return 1 + (rest + MaxItemCols - 1) / MaxItemCols;
    }

    void LayoutDock()
    {
        int cols = ColCount();
        int rows = RowCount();
        int w = Pad * 2 + Cell * cols;
        int h = Pad * 2 + Cell * rows;
        EnsureHome();
        int x = _homeX - Pad - Cell * (cols - 1);
        int y = _homeY - Pad;
        if (!IsHandleCreated)
        {
            Bounds = new Rectangle(x, y, w, h);
            return;
        }
        // Without NOCOPYBITS the old client bits land in the wrong cell and stay visible until the repaint below.
        Native.SetWindowPos(Handle, IntPtr.Zero, x, y, w, h,
            Native.SWP_NOZORDER | Native.SWP_NOACTIVATE | Native.SWP_NOCOPYBITS);
        Invalidate();
        Update();
    }

    Icon? IconFor(string path)
    {
        if (_icons.TryGetValue(path, out var cached)) return cached;
        Icon? ico = null;
        try { ico = Native.FileIcon(path, IconPx); }
        catch (Exception ex) { Dlog("icon " + ex.Message); }
        _icons[path] = ico;
        return ico;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        using (var edge = new Pen(Color.FromArgb(0, 220, 230), 1))
            g.DrawRectangle(edge, 0, 0, Width - 1, Height - 1);

        int n = _items.Count;
        int cols = ColCount();
        int rows = RowCount();
        int first = FirstRowItems();

        if (_expanded)
        {
            for (int i = 0; i < n; i++)
            {
                CellPos(i, cols, first, out int col, out int row);
                DrawItem(g, col, row, i);
            }
        }
        DrawHome(g, cols - 1, 0);
    }

    void CellPos(int item, int cols, int first, out int col, out int row)
    {
        if (item < first)
        {
            col = item;
            row = 0;
            return;
        }
        int rest = item - first;
        row = 1 + rest / MaxItemCols;
        col = rest % MaxItemCols;
    }

    int HitItem(int mx, int my)
    {
        if (!_expanded) return -1;
        HitCell(mx, my, out int col, out int row);
        int cols = ColCount();
        int first = FirstRowItems();
        if (row == 0 && col == cols - 1) return -1;
        if (row == 0)
            return col < first ? col : -1;
        int i = first + (row - 1) * MaxItemCols + col;
        return i >= 0 && i < _items.Count ? i : -1;
    }

    bool HitHome(int mx, int my)
    {
        HitCell(mx, my, out int col, out int row);
        return row == 0 && col == ColCount() - 1;
    }

    void HitCell(int mx, int my, out int col, out int row)
    {
        col = (mx - Pad) / Cell;
        row = (my - Pad) / Cell;
    }

    void DrawHome(Graphics g, int col, int row)
    {
        int x = Pad + col * Cell;
        int y = Pad + row * Cell;
        var dest = new Rectangle(x + 2, y + 2, Cell - 4, Cell - 4);
        var img = AppIcon.Bitmap();
        if (img != null)
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            using var path = RoundRect(dest, 10);
            var old = g.Save();
            g.SetClip(path);
            g.DrawImage(img, dest);
            g.Restore(old);
            using var edge = new Pen(Color.FromArgb(0, 220, 230), 1);
            g.DrawPath(edge, path);
            return;
        }
        using var b = new SolidBrush(Color.FromArgb(0, 200, 210));
        g.FillEllipse(b, dest);
    }

    static GraphicsPath RoundRect(Rectangle r, int rad)
    {
        var p = new GraphicsPath();
        int d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    void DrawItem(Graphics g, int col, int row, int index)
    {
        int x = Pad + col * Cell;
        int y = Pad + row * Cell;
        if (index == _dragItem && _reorder)
        {
            using var hi = new SolidBrush(Color.FromArgb(40, 0, 220, 230));
            g.FillRectangle(hi, x, y, Cell, Cell);
        }
        var ico = IconFor(_items[index]);
        if (ico != null)
        {
            int m = (Cell - IconPx) / 2;
            g.DrawIcon(ico, new Rectangle(x + m, y + m, IconPx, IconPx));
        }
        else
        {
            using var b = new SolidBrush(Color.FromArgb(50, 60, 80));
            g.FillRectangle(b, x + 4, y + 4, Cell - 8, Cell - 8);
        }
    }

    void PollCursor()
    {
        if (_menuOpen || _dragHome || _reorder || _fileDragging) return;
        if ((Native.GetAsyncKeyState(1) & 0x8000) != 0) return;
        var pt = Cursor.Position;
        var rc = RectangleToScreen(ClientRectangle);
        bool over = pt.X >= rc.Left - 24 && pt.X <= rc.Right + 12
            && pt.Y >= rc.Top - 24 && pt.Y <= rc.Bottom + 24;
        if (over)
        {
            _collapseAt = DateTime.MaxValue;
            if (!_expanded)
            {
                _expanded = true;
                LayoutDock();
                _hoverItem = -1;
                UpdateHoverTip(true);
            }
        }
        else if (_expanded)
        {
            HideTip();
            if (_collapseAt == DateTime.MaxValue)
                _collapseAt = DateTime.Now.AddMilliseconds(2500);
            else if (DateTime.Now >= _collapseAt)
            {
                _expanded = false;
                LayoutDock();
                _collapseAt = DateTime.MaxValue;
            }
        }
    }

    void OnDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        int item = HitItem(e.X, e.Y);
        if (item >= 0)
        {
            _dragItem = item;
            _didDrag = false;
            _reorder = false;
            _dragOffX = e.X;
            _dragOffY = e.Y;
            Capture = true;
            return;
        }
        if (HitHome(e.X, e.Y))
        {
            _dragHome = true;
            _didDrag = false;
            _dragOffX = e.X;
            _dragOffY = e.Y;
            Capture = true;
        }
    }

    void OnMove(object? sender, MouseEventArgs e)
    {
        if (_dragHome && e.Button == MouseButtons.Left)
        {
            if (Math.Abs(e.X - _dragOffX) > 4 || Math.Abs(e.Y - _dragOffY) > 4)
                _didDrag = true;
            if (_didDrag)
            {
                var scr = PointToScreen(e.Location);
                _homeX = scr.X - _dragOffX + Pad + Cell * (ColCount() - 1);
                _homeY = scr.Y - _dragOffY + Pad;
                ClampHome();
                LayoutDock();
            }
            return;
        }
        if (_dragItem >= 0 && e.Button == MouseButtons.Left)
        {
            if (Math.Abs(e.X - _dragOffX) > 6 || Math.Abs(e.Y - _dragOffY) > 6)
                _reorder = true;
            if (_reorder)
            {
                int dest = HitItem(e.X, e.Y);
                if (dest >= 0 && dest != _dragItem)
                {
                    var path = _items[_dragItem];
                    _items.RemoveAt(_dragItem);
                    _items.Insert(dest, path);
                    _dragItem = dest;
                    SaveOrder();
                    Invalidate();
                }
            }
            return;
        }
        UpdateHoverTip(false);
        if (!_expanded && !_fileDragging)
        {
            _expanded = true;
            LayoutDock();
            _hoverItem = -1;
            UpdateHoverTip(true);
        }
    }

    void UpdateHoverTip(bool force)
    {
        if (!_expanded || _fileDragging || _reorder || _dragHome)
        {
            if (force) HideTip();
            return;
        }
        var pt = PointToClient(Cursor.Position);
        int hover = HitItem(pt.X, pt.Y);
        if (!force && hover == _hoverItem) return;
        _hoverItem = hover;
        _tip.Hide(this);
        if (hover < 0 || hover >= _items.Count) return;
        _tip.Show(DisplayName(_items[hover]), this, pt.X, Math.Max(0, pt.Y - 28), 8000);
    }

    void OnUp(object? sender, MouseEventArgs e)
    {
        if (_dragHome)
        {
            Capture = false;
            bool moved = _didDrag;
            _dragHome = false;
            _didDrag = false;
            if (moved) { SaveSettings(); return; }
            if (e.Button == MouseButtons.Right) ShowMenu();
            return;
        }
        if (_dragItem >= 0)
        {
            int item = _dragItem;
            bool reordered = _reorder;
            _dragItem = -1;
            _reorder = false;
            Capture = false;
            if (reordered) { Invalidate(); return; }
            if (e.Button == MouseButtons.Left && item >= 0 && item < _items.Count)
                Launch(_items[item]);
            return;
        }
        if (e.Button == MouseButtons.Right)
        {
            int item = HitItem(e.X, e.Y);
            if (item >= 0) ShowItemMenu(item);
            else if (HitHome(e.X, e.Y)) ShowMenu();
        }
    }

    void HideTip()
    {
        _hoverItem = -1;
        _tip.Hide(this);
    }

    static string DisplayName(string path)
    {
        var n = Path.GetFileNameWithoutExtension(path);
        return n.Length > 0 ? n : Path.GetFileName(path);
    }

    void ShowItemMenu(int vis)
    {
        if (vis < 0 || vis >= _items.Count) return;
        string path = _items[vis];
        _menuOpen = true;
        var menu = new ContextMenuStrip();
        menu.Items.Add("管理者として実行", null, (_, _) => LaunchAsAdmin(path));
        menu.Items.Add("削除", null, (_, _) => ConfirmRemove(vis));
        menu.Closed += (_, _) => { _menuOpen = false; };
        menu.Show(Cursor.Position);
    }

    static void LaunchAsAdmin(string path)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path)
            {
                UseShellExecute = true,
                Verb = "runas"
            });
        }
        catch { }
    }

    void ConfirmRemove(int vis)
    {
        _menuOpen = true;
        string name = DisplayName(_items[vis]);
        var r = MessageBox.Show(this, name + " を削除しますか？", "QuickDock",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        _menuOpen = false;
        if (r != DialogResult.Yes) return;
        try { File.Delete(_items[vis]); } catch { }
        if (_icons.Remove(_items[vis], out var gone)) gone?.Dispose();
        _items.RemoveAt(vis);
        SaveOrder();
        LayoutDock();
    }

    ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        var startup = new ToolStripMenuItem("Windows起動時に実行")
        {
            Checked = IsStartupEnabled(),
            CheckOnClick = true
        };
        bool refreshingStartup = false;
        startup.CheckedChanged += (_, _) =>
        {
            if (refreshingStartup) return;
            try
            {
                SetStartupEnabled(startup.Checked);
            }
            catch (Exception ex)
            {
                refreshingStartup = true;
                startup.Checked = !startup.Checked;
                refreshingStartup = false;
                MessageBox.Show(this, "起動設定を保存できませんでした。\n" + ex.Message,
                    "QuickDock", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        menu.Items.Add(startup);
        var top = new ToolStripMenuItem("常に前面") { Checked = _topMost, CheckOnClick = true };
        top.CheckedChanged += (_, _) =>
        {
            _topMost = top.Checked;
            TopMost = _topMost;
            SaveSettings();
        };
        menu.Items.Add(top);
        menu.Items.Add("終了", null, (_, _) => Close());
        menu.Closed += (_, _) => { _menuOpen = false; };
        menu.Opening += (_, _) =>
        {
            _menuOpen = true;
            refreshingStartup = true;
            startup.Checked = IsStartupEnabled();
            refreshingStartup = false;
            top.Checked = _topMost;
        };
        return menu;
    }

    static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, writable: false);
        return key?.GetValue(StartupValueName) is string;
    }

    static void SetStartupEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(StartupRegistryPath, writable: true)
            ?? throw new IOException("ユーザーの起動設定を開けませんでした。");
        if (enabled)
            key.SetValue(StartupValueName, "\"" + Application.ExecutablePath + "\"", RegistryValueKind.String);
        else
            key.DeleteValue(StartupValueName, throwOnMissingValue: false);
    }

    void ShowMenu()
    {
        _menuOpen = true;
        BuildMenu().Show(Cursor.Position);
    }

    void EnsureHome()
    {
        if (_homeX != 0 || _homeY != 0) return;
        var wa = Screen.PrimaryScreen!.WorkingArea;
        _homeX = wa.Right - Pad - Cell;
        _homeY = wa.Top + Math.Max(0, (wa.Height - Cell) / 2);
    }

    void ClampHome()
    {
        var v = SystemInformation.VirtualScreen;
        _homeX = Math.Max(v.Left, Math.Min(v.Right - Cell, _homeX));
        _homeY = Math.Max(v.Top, Math.Min(v.Bottom - Cell, _homeY));
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

    static void Launch(string path)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch { }
    }

    void LoadOrder()
    {
        _items.Clear();
        var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(OrderPath))
        {
            foreach (var line in File.ReadAllLines(OrderPath))
            {
                var name = line.Trim();
                if (name.Length == 0) continue;
                var p = Path.Combine(PinsDir, name);
                if (File.Exists(p) && have.Add(p)) _items.Add(p);
            }
        }
        foreach (var p in Directory.GetFiles(PinsDir))
        {
            if (have.Add(p)) _items.Add(p);
        }
    }

    void SaveOrder()
    {
        Directory.CreateDirectory(ConfigDir);
        var names = new string[_items.Count];
        for (int i = 0; i < _items.Count; i++)
            names[i] = Path.GetFileName(_items[i]);
        File.WriteAllLines(OrderPath, names);
    }

    void MigrateOldItems()
    {
        if (!File.Exists(ConfigPath)) return;
        foreach (var line in File.ReadAllLines(ConfigPath))
        {
            var src = line.Trim();
            if (src.Length == 0 || !File.Exists(src)) continue;
            var dest = Path.Combine(PinsDir, Path.GetFileName(src));
            try
            {
                if (!File.Exists(dest)) File.Copy(src, dest, false);
            }
            catch { }
        }
    }

    void LoadSettings()
    {
        var wa = Screen.PrimaryScreen!.WorkingArea;
        _homeX = wa.Right - Pad - Cell;
        _homeY = wa.Top + Math.Max(0, (wa.Height - Cell) / 2);
        _topMost = true;
        if (!File.Exists(SettingsPath)) return;
        foreach (var line in File.ReadAllLines(SettingsPath))
        {
            int eq = line.IndexOf('=');
            if (eq <= 0) continue;
            string k = line[..eq].Trim();
            string v = line[(eq + 1)..].Trim();
            if (k == "x" && int.TryParse(v, out var x)) _homeX = x;
            else if (k == "y" && int.TryParse(v, out var y)) _homeY = y;
            else if (k == "topmost") _topMost = v != "0";
        }
    }

    void SaveSettings()
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllLines(SettingsPath, [
            "x=" + _homeX,
            "y=" + _homeY,
            "topmost=" + (_topMost ? "1" : "0")
        ]);
    }
}

internal static class AppIcon
{
    static Image? _bmp;
    static Icon? _icon;

    public static Image? Bitmap()
    {
        if (_bmp != null) return _bmp;
        using var s = typeof(AppIcon).Assembly.GetManifestResourceStream("QuickDock.icon.png");
        if (s == null) return null;
        _bmp = Image.FromStream(s);
        return _bmp;
    }

    public static Icon Create()
    {
        if (_icon != null) return _icon;
        var img = Bitmap();
        if (img != null)
        {
            using var scaled = new Bitmap(img, 32, 32);
            IntPtr h = scaled.GetHicon();
            try { _icon = (Icon)Icon.FromHandle(h).Clone(); }
            finally { Native.DestroyIcon(h); }
            return _icon;
        }
        using var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(16, 20, 28));
            using var cyan = new SolidBrush(Color.FromArgb(0, 200, 210));
            g.FillEllipse(cyan, 4, 4, 24, 24);
        }
        IntPtr h2 = bmp.GetHicon();
        try { _icon = (Icon)Icon.FromHandle(h2).Clone(); }
        finally { Native.DestroyIcon(h2); }
        return _icon;
    }
}

internal static class Native
{
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr hIcon);

    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_NOCOPYBITS = 0x0100;

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SHGetFileInfo(string pszPath, uint fa, ref SHFILEINFO psfi, uint cb, uint flags);

    [DllImport("shell32.dll")]
    static extern int SHGetImageList(int iImageList, ref Guid riid, out IntPtr ppv);

    [DllImport("comctl32.dll")]
    static extern IntPtr ImageList_GetIcon(IntPtr himl, int i, int flags);

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

    public static Icon? FileIcon(string path, int px)
    {
        var sh = new SHFILEINFO();
        var iid = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
        if (SHGetImageList(2, ref iid, out var himl) == 0)
        {
            SHGetFileInfo(path, 0, ref sh, (uint)Marshal.SizeOf<SHFILEINFO>(), 0x4000);
            IntPtr h = ImageList_GetIcon(himl, sh.iIcon, 1);
            if (h != IntPtr.Zero)
            {
                try { return (Icon)Icon.FromHandle(h).Clone(); }
                finally { DestroyIcon(h); }
            }
        }
        return Icon.ExtractAssociatedIcon(path);
    }
}
