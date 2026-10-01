using System.Diagnostics;

namespace StandUpHero;

internal sealed class IdleSuggestionNotice : Form
{
    private readonly Stopwatch age = new();
    internal event EventHandler? RelaxRequested;
    internal bool DoesNotActivate => (CreateParams.ExStyle & 0x08000000) != 0;
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ExStyle |= 0x08000000 | 0x00000080; return cp; }
    }
    internal IdleSuggestionNotice()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false; TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(346, 192);
        BackColor = Color.FromArgb(29, 34, 49); ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        Controls.Add(new Label { Text = "Fez uma pausa?", AutoSize = true, Location = new Point(16, 13), ForeColor = Color.FromArgb(167, 230, 135), Font = new Font("Segoe UI", 11, FontStyle.Bold) });
        Controls.Add(new Label { Text = "Você ficou um tempo sem usar mouse\nou teclado. Quer mudar para \"Relaxando\"\ne parar os lembretes de postura?", Location = new Point(16, 43), Size = new Size(315, 72) });
        var relax = new Button { Text = "Relaxar", Location = new Point(16, 137), Size = new Size(98, 34), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(43, 63, 48), AccessibleName = "Mudar status para Relaxando" };
        var keep = new Button { Text = "Continuar trabalhando", Location = new Point(126, 137), Size = new Size(204, 34), FlatStyle = FlatStyle.Flat };
        relax.Click += (_, _) => RelaxRequested?.Invoke(this, EventArgs.Empty);
        keep.Click += (_, _) => Dismiss();
        Controls.AddRange([relax, keep]);
    }
    internal void Present(Rectangle anchor, Rectangle screen)
    {
        Follow(anchor, screen); age.Restart(); Show();
    }
    internal void Follow(Rectangle anchor, Rectangle screen)
    {
        int y = anchor.Top - Height - 6;
        if (y < screen.Top) y = anchor.Bottom + 6;
        Location = new Point(Math.Clamp(anchor.Left + (anchor.Width - Width) / 2, screen.Left, Math.Max(screen.Left, screen.Right - Width)),
            Math.Clamp(y, screen.Top, Math.Max(screen.Top, screen.Bottom - Height)));
    }
    internal void Tick() { if (Visible && age.Elapsed >= TimeSpan.FromSeconds(30)) Dismiss(); }
    internal void Dismiss() { age.Stop(); Hide(); }
}
