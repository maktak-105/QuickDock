using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace QuickDock;

// Help (使い方) and About (バージョン情報) windows, following the Quick-series UI spec (11_アプリ仕様.md, sections 8-9).
internal static class HelpUi
{
    public static readonly Color Back = Color.FromArgb(16, 20, 28);
    public static readonly Color Panel = Color.FromArgb(28, 34, 44);
    public static readonly Color Border = Color.FromArgb(52, 66, 84);
    public static readonly Color Accent = Color.FromArgb(0, 200, 230);
    public static readonly Color Text = Color.FromArgb(235, 240, 245);
    public static readonly Color SubText = Color.FromArgb(150, 160, 175);

    // Version comes from <Version> in QuickDock.csproj (single source).
    // ProductVersion may carry a "+commit" suffix from SourceLink; the user sees only x.y.z.
    public static string Version
    {
        get
        {
            var v = Application.ProductVersion;
            int plus = v.IndexOf('+');
            return plus >= 0 ? v.Substring(0, plus) : v;
        }
    }

    // Japanese UI unless the OS language is something else; the pill button switches it.
    public static bool English = !CultureInfo.CurrentUICulture.Name.StartsWith("ja", StringComparison.OrdinalIgnoreCase);

    public static string T(string ja, string en) => English ? en : ja;

    public static Image? LoadBadge()
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("QuickDock.badge.jpg");
        if (s == null) return null;
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        ms.Position = 0;
        return Image.FromStream(ms);
    }
}

// Borderless dark card with a thin cyan outline (spec 9.2). Used by both Help and About.
internal abstract class DarkCard : Form
{
    readonly Button _lang = new();

    protected DarkCard(int width)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        BackColor = HelpUi.Back;
        ForeColor = HelpUi.Text;
        Font = new Font("Segoe UI", 9.5f);
        ClientSize = new Size(width, 400);
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };

        _lang.FlatStyle = FlatStyle.Flat;
        _lang.FlatAppearance.BorderColor = HelpUi.Border;
        _lang.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 52, 66);
        _lang.BackColor = HelpUi.Panel;
        _lang.ForeColor = HelpUi.Accent;
        _lang.Cursor = Cursors.Hand;
        _lang.Size = new Size(108, 28);
        _lang.Location = new Point(width - 24 - 108, 18);
        _lang.TabStop = false;
        _lang.Click += (_, _) =>
        {
            HelpUi.English = !HelpUi.English;
            _lang.Text = HelpUi.T("\U0001F310 English", "\U0001F310 日本語");
            ApplyText();
        };
        Controls.Add(_lang);
    }

    // Subclasses call this once their controls exist and again on every language switch.
    protected void SetLanguageButton()
    {
        _lang.Text = HelpUi.T("\U0001F310 English", "\U0001F310 日本語");
    }

    protected abstract void ApplyText();

    protected void Place(Control c, ref int y, int gap = 8)
    {
        c.Location = new Point(24, y);
        Controls.Add(c);
        y = c.Bottom + gap;
    }

    protected static Label Heading(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = HelpUi.Accent,
        Font = new Font("Segoe UI", 10f, FontStyle.Bold),
    };

    protected static Label Body(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = HelpUi.Text,
        MaximumSize = new Size(380, 0),
    };

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(HelpUi.Accent, 1f);
        var r = new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        using var path = RoundedRect(r, 12);
        g.DrawPath(pen, path);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // Round the card corners; the outline is drawn in OnPaint.
        using var path = RoundedRect(new Rectangle(0, 0, ClientSize.Width, ClientSize.Height), 12);
        Region = new Region(path);
    }

    static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    protected Button MakeOk()
    {
        var ok = new Button
        {
            Text = "OK",
            FlatStyle = FlatStyle.Flat,
            BackColor = HelpUi.Panel,
            ForeColor = HelpUi.Accent,
            Size = new Size(120, 34),
            DialogResult = DialogResult.OK,
        };
        ok.FlatAppearance.BorderColor = HelpUi.Border;
        ok.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 52, 66);
        return ok;
    }
}

internal sealed class HelpCard : DarkCard
{
    readonly Label _title = new();
    readonly Label _body = new();
    readonly Button _ok;

    public HelpCard() : base(540)
    {
        Text = "QuickDock";
        _title.AutoSize = true;
        _title.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
        _title.ForeColor = HelpUi.Text;
        _body.AutoSize = true;
        _body.ForeColor = HelpUi.Text;
        _body.MaximumSize = new Size(480, 0);
        _ok = MakeOk();

        int y = 20;
        Place(_title, ref y, 14);
        Place(_body, ref y, 18);
        _ok.Location = new Point((ClientSize.Width - _ok.Width) / 2, y);
        Controls.Add(_ok);
        ClientSize = new Size(ClientSize.Width, _ok.Bottom + 22);
        SetLanguageButton();
        ApplyText();
    }

    protected override void ApplyText()
    {
        _title.Text = HelpUi.T("QuickDock の使い方", "How to use QuickDock");
        _body.Text = HelpUi.T(
            "・ファイルやショートカットをドックにドロップすると登録されます。\n" +
            "・マウスを乗せると左へ広がり、項目をクリックすると起動します。\n" +
            "・ドックの背景をドラッグすると移動できます。\n" +
            "・右クリックでメニューを開きます。\n" +
            "　「Windows起動時に実行」「常に前面」「ヘルプ」「終了」があります。\n" +
            "・項目を右クリックすると「管理者として実行」「削除」ができます。\n" +
            "　削除の前には確認が出ます。\n" +
            "・タスクトレイのアイコンからも表示と終了ができます。\n" +
            "・エクスプローラーからダブルクリックで起動してください。\n" +
            "　管理者権限のターミナルから起動すると、エクスプローラーからのドロップはできません。",
            "・Drop a file or shortcut onto the dock to register it.\n" +
            "・Hover to expand the dock to the left; click an item to launch it.\n" +
            "・Drag the dock background to move it.\n" +
            "・Right-click to open the menu:\n" +
            "　Run at Windows startup, Always on top, Help, Exit.\n" +
            "・Right-click an item for Run as administrator or Delete.\n" +
            "　Delete asks for confirmation first.\n" +
            "・The tray icon also shows the dock or exits it.\n" +
            "・Start QuickDock.exe by double-clicking it in Explorer.\n" +
            "　If started from an elevated terminal, Explorer cannot drop onto it.");
        Text = HelpUi.T("使い方", "Help");
        _ok.Text = "OK";
    }
}

internal sealed class AboutCard : DarkCard
{
    const int CardWidth = 440;
    const int BadgeWidth = 160;

    readonly Label _name = new();
    readonly Label _ver = new();
    readonly Label _devHead = Heading("");
    readonly Label _dev = Body("");
    readonly Label _authorHead = Heading("");
    readonly Label _author = Body("");
    readonly PictureBox _badge = new();
    readonly Button _ok;

    public AboutCard() : base(CardWidth)
    {
        Text = "QuickDock";
        _name.AutoSize = true;
        _name.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
        _name.ForeColor = Color.White;
        _name.Text = "QuickDock";
        _ver.AutoSize = true;
        _ver.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        _ver.ForeColor = HelpUi.Accent;
        _ver.Text = "Ver. " + HelpUi.Version;
        _ok = MakeOk();

        // Layout top to bottom: language pill (drawn at the top right), name, version, divider, sections, badge, OK.
        int y = 56;
        Place(_name, ref y, 0);
        _ver.Location = new Point(24, y);
        Controls.Add(_ver);
        y = _ver.Bottom + 14;
        int dividerY = y;
        y += 14;
        Place(_devHead, ref y, 2);
        Place(_dev, ref y, 14);
        Place(_authorHead, ref y, 2);
        Place(_author, ref y, 16);

        var badge = HelpUi.LoadBadge();
        if (badge != null)
        {
            _badge.Image = badge;
            _badge.SizeMode = PictureBoxSizeMode.StretchImage;
            _badge.Size = new Size(BadgeWidth, BadgeWidth * badge.Height / badge.Width);
            _badge.Location = new Point((CardWidth - BadgeWidth) / 2, y);
            Controls.Add(_badge);
            y = _badge.Bottom + 16;
        }
        _ok.Location = new Point((CardWidth - _ok.Width) / 2, y);
        Controls.Add(_ok);
        ClientSize = new Size(CardWidth, _ok.Bottom + 22);

        // Divider line under the version (spec 9.2).
        Paint += (_, e) =>
        {
            using var pen = new Pen(HelpUi.Border, 1f);
            e.Graphics.DrawLine(pen, 24, dividerY, CardWidth - 24, dividerY);
        };

        SetLanguageButton();
        ApplyText();
    }

    protected override void ApplyText()
    {
        _devHead.Text = "[" + HelpUi.T("開発環境", "Development") + "]";
        _dev.Text = "C# / .NET 10 / WinForms" + "\n" +
                    HelpUi.T("Win32 API（ドラッグ＆ドロップ、タスクトレイ）", "Win32 API (drag and drop, tray icon)");
        _authorHead.Text = "[" + HelpUi.T("制作者", "Author") + "]";
        _author.Text = "maktak-105\nhttps://github.com/maktak-105";
        _ok.Text = "OK";
    }
}

internal static class HelpDialogs
{
    // Dims the whole screen behind the modal card (spec 9.1).
    static void ShowModal(DarkCard card)
    {
        var bounds = Screen.PrimaryScreen!.Bounds;
        using var overlay = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Bounds = bounds,
            BackColor = Color.Black,
            Opacity = 0.55,
            ShowInTaskbar = false,
            TopMost = true,
        };
        overlay.Show();
        try
        {
            card.ShowDialog(overlay);
        }
        finally
        {
            overlay.Hide();
            card.Dispose();
        }
    }

    public static void ShowHelp() => ShowModal(new HelpCard());

    public static void ShowAbout() => ShowModal(new AboutCard());
}
