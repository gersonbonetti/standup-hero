namespace StandUpHero;

internal sealed class ResumeNotice : Form
{
    internal event EventHandler? ResumeRequested;
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ExStyle |= 0x08000000 | 0x00000080; return cp; }
    }
    internal ResumeNotice()
    {
        FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
        StartPosition = FormStartPosition.Manual; AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(346, 166); BackColor = Color.FromArgb(29, 34, 49); ForeColor = Color.White;
        Font = new Font("Segoe UI", 10);
        Controls.Add(new Label { Text = "Continuar a rotina?", Location = new Point(16, 13), AutoSize = true, ForeColor = Color.FromArgb(167, 230, 135), Font = new Font("Segoe UI", 11, FontStyle.Bold) });
        Controls.Add(new Label { Text = "O computador foi bloqueado e o tempo\nficou pausado. Continue de onde parou\nquando estiver pronto.", Location = new Point(16, 44), Size = new Size(314, 62) });
        var resume = new Button { Text = "Continuar", Location = new Point(16, 118), Size = new Size(130, 32), FlatStyle = FlatStyle.Flat };
        var keep = new Button { Text = "Manter pausado", Location = new Point(160, 118), Size = new Size(170, 32), FlatStyle = FlatStyle.Flat };
        resume.Click += (_, _) => ResumeRequested?.Invoke(this, EventArgs.Empty);
        keep.Click += (_, _) => Hide();
        Controls.AddRange([resume, keep]);
    }
    internal void Present(Rectangle mascot, Rectangle area) { Follow(mascot, area); Show(); }
    internal void Follow(Rectangle mascot, Rectangle area)
    {
        var y = mascot.Top - Height - 6;
        if (y < area.Top) y = mascot.Bottom + 6;
        Location = new Point(Math.Clamp(mascot.Left + (mascot.Width - Width) / 2, area.Left, Math.Max(area.Left, area.Right - Width)),
            Math.Clamp(y, area.Top, Math.Max(area.Top, area.Bottom - Height)));
    }
}
