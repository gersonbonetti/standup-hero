using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace StandUpHero;

// A separate non-activating surface keeps the mascot small and never steals typing focus.
internal sealed class VisualNotice : Form
{
    private readonly Stopwatch elapsed = new();
    private readonly Font titleFont = new("Segoe UI", 11, FontStyle.Bold);
    private readonly Font detailFont = new("Segoe UI", 9);
    private readonly ToolTip tooltip = new();
    private Rectangle anchor;
    private Rectangle workingArea;
    private bool standing;
    private int duration;
    private bool compact;
    private double pulse;
    private readonly Button snooze = new() { Text = "Daqui a 5 minutos", FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(43, 54, 70) };
    internal event EventHandler? SnoozeRequested;
    public bool Pending { get; private set; }
    internal bool Compact => compact;
    internal double Pulse => pulse;
    internal bool DoesNotActivate => (CreateParams.ExStyle & 0x08000000) != 0;
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ExStyle |= 0x08000000 | 0x00000080; return cp; }
    }

    public VisualNotice()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.FromArgb(29, 34, 49);
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        AccessibleName = "Aviso de mudança de postura. Clique para fechar o aviso.";
        snooze.Click += (_, _) => SnoozeRequested?.Invoke(this, EventArgs.Empty);
        Controls.Add(snooze);
    }

    internal void Present(bool isStanding, int seconds, Rectangle mascot, Rectangle screen, bool canSnooze = false)
    {
        standing = isStanding;
        duration = Math.Clamp(seconds, 5, 30);
        Pending = true;
        compact = false;
        snooze.Enabled = canSnooze;
        tooltip.SetToolTip(snooze, canSnooze ? "Adia a mudança e mantém a postura anterior por mais 5 minutos." : "Disponível em um aviso real; a prévia não altera a rotina.");
        SetSize();
        Follow(mascot, screen);
        elapsed.Restart();
        tooltip.SetToolTip(this, isStanding ? "Hora de ficar em pé · clique para fechar o aviso" : "Pode sentar novamente · clique para fechar o aviso");
        Show();
        Advance(TimeSpan.Zero);
    }

    internal void Follow(Rectangle mascot, Rectangle screen)
    {
        anchor = mascot; workingArea = screen;
        int x = mascot.Left + (mascot.Width - Width) / 2;
        int y = mascot.Top - Height - 6;
        if (y < screen.Top) y = mascot.Bottom + 6;
        Location = new Point(Math.Clamp(x, screen.Left, Math.Max(screen.Left, screen.Right - Width)),
            Math.Clamp(y, screen.Top, Math.Max(screen.Top, screen.Bottom - Height)));
    }

    internal void Tick() => Advance(elapsed.Elapsed);
    internal void Advance(TimeSpan age)
    {
        if (!Pending) return;
        // Exactly two slow pulses, then a steady surface; no continuous flashing.
        pulse = age.TotalSeconds < 2.4 ? Math.Pow(Math.Sin(Math.PI * age.TotalSeconds / 1.2), 2) : 0;
        if (!compact && age.TotalSeconds >= duration)
        {
            compact = true;
            tooltip.SetToolTip(this, "Clique na seta para rever o aviso e suas opções.");
            SetSize();
            Follow(anchor, workingArea);
        }
        Invalidate();
    }

    private void SetSize()
    {
        float scale = DeviceDpi / 96f;
        ClientSize = compact ? new Size((int)(34 * scale), (int)(34 * scale)) : new Size((int)(268 * scale), (int)(118 * scale));
        snooze.Visible = !compact;
        snooze.SetBounds((int)(16 * scale), (int)(75 * scale), (int)(236 * scale), (int)(30 * scale));
        using var outline = Outline(ClientRectangle, compact ? Height / 2f : 12 * scale);
        var old = Region;
        Region = new Region(outline);
        old?.Dispose();
    }

    internal void Dismiss()
    {
        Pending = false; pulse = 0; elapsed.Stop(); Hide();
    }
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        if (Pending) { SetSize(); Follow(anchor, workingArea); }
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;
        if (compact)
        {
            compact = false; elapsed.Restart(); SetSize(); Follow(anchor, workingArea); Invalidate();
            tooltip.SetToolTip(this, "Clique no texto para fechar o aviso.");
        }
        else Dismiss();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float scale = DeviceDpi / 96f;
        var accent = standing ? Color.FromArgb(167, 230, 135) : Color.FromArgb(121, 187, 208);
        using var outline = Outline(new RectangleF(1, 1, Width - 3, Height - 3), compact ? Height / 2f - 1 : 11 * scale);
        using var border = new Pen(Color.FromArgb((int)(120 + pulse * 135), accent), (float)((1.5 + pulse * 2) * scale));
        g.DrawPath(border, outline);
        if (compact)
        {
            TextRenderer.DrawText(g, standing ? "↑" : "↓", titleFont, ClientRectangle, accent,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }
        TextRenderer.DrawText(g, standing ? "↑  Hora de ficar em pé" : "↓  Pode sentar novamente", titleFont,
            new Rectangle((int)(16 * scale), (int)(13 * scale), Width - (int)(28 * scale), (int)(28 * scale)), accent, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(g, "Clique para fechar o aviso", detailFont,
            new Rectangle((int)(16 * scale), (int)(44 * scale), Width - (int)(28 * scale), (int)(22 * scale)), Color.FromArgb(214, 220, 232), TextFormatFlags.Left);
    }

    private static GraphicsPath Outline(RectangleF r, float radius)
    {
        var path = new GraphicsPath(); float d = radius * 2;
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure(); return path;
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { elapsed.Stop(); tooltip.Dispose(); titleFont.Dispose(); detailFont.Dispose(); }
        base.Dispose(disposing);
    }
}
