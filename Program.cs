namespace StandUpHero;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length == 2 && args[0] == "--check-ui")
        {
            Directory.CreateDirectory(args[1]);
            using var hero = new HeroWindow(new Preferences { Sound = false }, _ => { });
            using var settings = new SettingsWindow(new Preferences());
            foreach (var (form, name) in new (Form, string)[] { (hero, "companion"), (settings, "settings") })
            {
                form.Show();
                Application.DoEvents();
                using var bitmap = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                bitmap.Save(Path.Combine(args[1], name + ".png"));
                if (form is SettingsWindow)
                {
                    var tabs = form.Controls.OfType<TabControl>().Single();
                    tabs.SelectedIndex = 1;
                    Application.DoEvents();
                    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                    bitmap.Save(Path.Combine(args[1], "character-sounds.png"));
                    var preview = tabs.TabPages[1].Controls.OfType<PixelScene>().Single();
                    preview.Female = true;
                    preview.Standing = true;
                    preview.Stretching = true;
                    using var sprite = new Bitmap(preview.Width, preview.Height);
                    preview.DrawToBitmap(sprite, preview.ClientRectangle);
                    sprite.Save(Path.Combine(args[1], "woman-stretch.png"));
                    var options = tabs.TabPages[1].Controls.OfType<ComboBox>().ToArray();
                    options.Single(c => c.AccessibleName == "Personagem").SelectedItem = "Mulher";
                    options.Single(c => c.AccessibleName == "Som ao levantar").SelectedItem = "Arcade";
                    tabs.SelectedIndex = 0;
                    var page = tabs.TabPages[0];
                    foreach (var input in page.Controls.OfType<NumericUpDown>()) input.Value = 7;
                    page.Controls.OfType<TextBox>().Single().Text = "10:00-11:00";
                    var dayList = page.Controls.OfType<CheckedListBox>().Single();
                    for (int i = 0; i < 7; i++) dayList.SetItemChecked(i, i == 6);
                    page.Controls.OfType<Button>().Single(b => b.Text == "Restaurar rotina padrão").PerformClick();
                    form.Controls.OfType<Button>().Single(b => b.Text == "Salvar rotina").PerformClick();
                    var result = ((SettingsWindow)form).Result;
                    if (result.SittingMinutes != 45 || result.StandingMinutes != 15 || result.Days.Length != 5 || result.Days.Contains(DayOfWeek.Sunday)
                        || result.Slots.Single() != new TimeSlot(TimeSpan.FromHours(9), TimeSpan.FromHours(18)) || result.Character != "Mulher" || result.StandSound != "Arcade")
                        throw new InvalidOperationException("Reset/save UI verification failed.");
                    File.WriteAllText(Path.Combine(args[1], "ui-check.txt"), "Reset and save verified; character and sound preserved. Embedded audio resources loaded.");
                }
                form.Hide();
            }
            return;
        }
        using var instance = new SingleInstance();
        if (!instance.IsPrimary) { instance.RequestShow(); return; }
        using var window = new HeroWindow();
        using var activationTimer = new System.Windows.Forms.Timer { Interval = 250 };
        activationTimer.Tick += (_, _) => { if (instance.ConsumeShowRequest() && !window.IsDisposed) window.Show(); };
        activationTimer.Start();
        Application.Run(window);
    }
}

public sealed class HeroWindow : Form
{
    private readonly PostureAudio audio = new();
    private readonly Routine routine;
    private readonly Action<Preferences> savePreferences;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 100 };
    private readonly NotifyIcon tray;
    private readonly ToolTip tooltip = new() { InitialDelay = 250, ReshowDelay = 100, AutoPopDelay = 10000 };
    private readonly PixelScene scene = new() { Dock = DockStyle.Fill };
    private readonly ContextMenuStrip menu = new();
    private readonly ToolStripMenuItem status = new() { Enabled = false };
    private readonly ToolStripMenuItem pause = new("Pausar");
    private readonly ToolStripMenuItem skip = new("Trocar postura");
    private DateTime stretchUntil;
    private int frame;
    private Point? dragOrigin;
    private Point windowOrigin;
    private string lastTip = "";
    private SettingsWindow? settingsWindow;
    private bool resourcesDisposed;
    internal bool TimerRunning => timer.Enabled;
    internal bool ResourcesDisposed => resourcesDisposed;
    internal Routine CurrentRoutine => routine;

    // A tool window without activation: the companion does not interrupt typing.
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ExStyle |= 0x08000000 | 0x00000080; return cp; }
    }

    public HeroWindow() : this(PreferenceStore.Load(), PreferenceStore.Save) { }

    internal HeroWindow(Preferences preferences, Action<Preferences> savePreferences)
    {
        routine = new Routine(preferences);
        this.savePreferences = savePreferences;
        Text = "StandUp Hero";
        Icon = AppAssets.Icon;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(140, 96);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        BackColor = Color.Magenta;
        TransparencyKey = Color.Magenta;
        TopMost = routine.Settings.AlwaysOnTop;
        StartPosition = FormStartPosition.Manual;
        var area = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(area.Right - Width - 32, area.Bottom - Height);
        Controls.Add(scene);
        scene.Cursor = Cursors.SizeAll;
        scene.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            dragOrigin = Cursor.Position; windowOrigin = Location; scene.Capture = true;
        };
        scene.MouseMove += (_, _) =>
        {
            if (dragOrigin is not Point origin) return;
            var position = Cursor.Position;
            var bounds = Screen.FromPoint(position).WorkingArea;
            Location = new Point(Math.Clamp(windowOrigin.X + position.X - origin.X, bounds.Left, Math.Max(bounds.Left, bounds.Right - Width)),
                Math.Clamp(windowOrigin.Y + position.Y - origin.Y, bounds.Top, Math.Max(bounds.Top, bounds.Bottom - Height)));
        };
        scene.MouseUp += (_, e) => { if (e.Button == MouseButtons.Left) { dragOrigin = null; scene.Capture = false; } };
        scene.MouseCaptureChanged += (_, _) => { if (!scene.Capture) dragOrigin = null; };
        scene.DoubleClick += (_, _) => EditSettings();

        menu.Items.Add(status);
        menu.Items.Add(new ToolStripSeparator());
        var activities = new ToolStripMenuItem("Atividade");
        foreach (var activity in new[] { "Trabalhando", "Jogando", "Relaxando" })
        {
            var item = new ToolStripMenuItem(activity);
            item.Click += (_, _) =>
            {
                var old = routine.Settings.Activity;
                routine.Settings.Activity = activity;
                if (!Save()) routine.Settings.Activity = old;
                else routine.Reset();
                RefreshRoutine();
            };
            activities.DropDownItems.Add(item);
        }
        menu.Items.Add(activities);
        pause.Click += (_, _) => { RefreshRoutine(); routine.TogglePause(DateTimeOffset.Now); UpdateState(); };
        skip.Click += (_, _) => { RefreshRoutine(); routine.Skip(DateTimeOffset.Now); stretchUntil = DateTime.Now.AddSeconds(5); UpdateState(); };
        menu.Items.Add(pause); menu.Items.Add(skip);
        menu.Items.Add("Configurar rotina…", null, (_, _) => EditSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Mostrar companheiro", null, (_, _) => Show());
        menu.Items.Add("Ocultar (manter lembretes)", null, (_, _) => Hide());
        menu.Items.Add("Encerrar aplicativo", null, (_, _) => Close());
        menu.Opening += (_, _) =>
        {
            RefreshRoutine();
            foreach (ToolStripMenuItem item in activities.DropDownItems) item.Checked = item.Text == routine.Settings.Activity;
        };
        scene.ContextMenuStrip = menu;
        tray = new NotifyIcon { Icon = AppAssets.Icon, Visible = true, Text = "StandUp Hero", ContextMenuStrip = menu };
        tray.DoubleClick += (_, _) => Show();
        timer.Tick += (_, _) => { scene.Frame = frame++ / 5; RefreshRoutine(); };
        RefreshRoutine();
        timer.Start();
    }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (!e.Cancel) StopResources();
    }
    private void StopResources()
    {
        if (resourcesDisposed) return;
        resourcesDisposed = true;
        timer.Stop();
        tray.Visible = false;
        settingsWindow?.Close();
        timer.Dispose(); tray.Dispose(); tooltip.Dispose(); menu.Dispose(); audio.Dispose();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) StopResources();
        base.Dispose(disposing);
    }
    private bool Save()
    {
        try { savePreferences(routine.Settings); return true; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { MessageBox.Show(this, "Não foi possível salvar a rotina. " + e.Message, "StandUp Hero"); return false; }
    }
    internal bool ApplyPreferences(Preferences preferences)
    {
        try { savePreferences(preferences); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { MessageBox.Show(this, "Não foi possível salvar a rotina. " + e.Message, "StandUp Hero"); return false; }
        audio.Play("Sem som");
        routine.Configure(preferences);
        stretchUntil = DateTime.MinValue;
        TopMost = preferences.AlwaysOnTop;
        RefreshRoutine();
        return true;
    }
    internal void EditSettings()
    {
        if (settingsWindow is not null) { settingsWindow.Activate(); return; }
        using var dialog = new SettingsWindow(routine.Settings, ApplyPreferences);
        settingsWindow = dialog;
        dialog.ExitRequested += (_, _) => Close();
        // A normal independent dialog can receive focus, unlike the mascot.
        dialog.TopMost = TopMost;
        dialog.StartPosition = FormStartPosition.CenterScreen;
        try { dialog.ShowDialog(); }
        finally { settingsWindow = null; }
    }
    private void RefreshRoutine()
    {
        if (resourcesDisposed) return;
        if (routine.Tick(DateTimeOffset.Now))
        {
            stretchUntil = DateTime.Now.AddSeconds(5);
            tray.ShowBalloonTip(5000, "StandUp Hero", routine.Standing ? "Hora de levantar! Espreguice-se e continue em pé." : "Pode sentar. Um novo período está começando.", ToolTipIcon.Info);
            if (routine.Settings.Sound) audio.Play(routine.Standing ? routine.Settings.StandSound : routine.Settings.SitSound);
        }
        UpdateState();
    }
    private void UpdateState()
    {
        var seconds = (int)Math.Ceiling(routine.Remaining.TotalSeconds);
        status.Text = !routine.Active ? routine.Settings.Activity == "Relaxando" ? "Relaxando · sem lembretes" : "Fora do horário da rotina" :
            $"{(routine.Paused ? "Pausado" : routine.Standing ? "Em pé" : "Sentado")} · {seconds / 60:00}:{seconds % 60:00}";
        var tip = status.Text + "\nArraste para mover · botão direito para opções";
        if (lastTip != tip) { tooltip.SetToolTip(scene, tip); lastTip = tip; }
        tray.Text = "StandUp Hero · " + status.Text;
        pause.Text = routine.Paused ? "Continuar" : "Pausar";
        pause.Enabled = skip.Enabled = routine.Active;
        scene.Standing = routine.Active && routine.Standing;
        scene.Resting = !routine.Active;
        scene.Stretching = scene.Standing && DateTime.Now < stretchUntil;
        scene.Gaming = routine.Settings.Activity == "Jogando";
        scene.Paused = routine.Paused;
        scene.Female = routine.Settings.Character == "Mulher";
        scene.Invalidate();
    }
}

// Original low-resolution game sprite, drawn on an integer pixel grid.
public sealed class PixelScene : Control
{
    public bool Standing, Resting, Stretching, Gaming, Paused, Female, Preview;
    public int Frame;
    public PixelScene() { DoubleBuffered = true; AccessibleName = "Seu companheiro de rotina"; }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(Preview ? Color.FromArgb(29, 34, 49) : Color.Magenta);
        float scale = Math.Min(Width / 70f, Height / 48f);
        g.ScaleTransform(scale, scale);
        g.TranslateTransform(-37, 0);
        void R(string color, int x, int y, int w, int h) { using var b = new SolidBrush(ColorTranslator.FromHtml(color)); g.FillRectangle(b, x, y, w, h); }
        R("#414862", 42, 44, 60, 2);
        R("#65748A", 45, 44, 55, 1);
        int desk = Standing ? 25 : 33;
        R("#CD9A7A", 62, desk, 39, 3); R("#A37361", 65, desk + 3, 3, 44 - desk - 3); R("#A37361", 95, desk + 3, 3, 44 - desk - 3);
        R("#101A2C", 77, desk - 17, 18, 13); R(Gaming ? "#AB96E8" : "#79BBD0", 79, desk - 15, 14, 9);
        R("#C7ECDF", 81, desk - 13, 7, 1); R("#C7ECDF", 81, desk - 10, 10, 1);
        R("#8895AC", 85, desk - 4, 2, 4); R("#8895AC", 70, desk - 1, 11, 1);
        R("#65648C", 44, 29, 4, 11); R("#65648C", 44, 37, 15, 3); R("#414862", 48, 40, 3, 4);
        int x = Resting ? 47 : 52, y = Standing ? 9 : 19;
        int bob = !Paused && Frame % 6 == 0 ? 1 : 0;
        if (Female) { R("#6B4140", x - 3, y + 1 + bob, 5, 18); R("#6B4140", x - 6, y + 5 + bob, 4, 10); R("#E7B65A", x - 3, y + 5 + bob, 3, 2); }
        R("#EABA91", x, y + bob, 10, 10); R("#42333F", x - 1, y - 2 + bob, 12, 5); R("#42333F", x - 1, y + 2 + bob, 3, 5);
        R("#263046", x + 7, y + 4 + bob, 2, Frame % 17 == 0 ? 1 : 2);
        if (Female) { R("#6B4140", x - 1, y - 2 + bob, 12, 4); R("#6B4140", x - 1, y + 2 + bob, 3, 6); }
        R(Resting ? "#BAA3DC" : Female ? "#79BBD0" : "#A7E687", x, y + 10, 11, Standing ? 13 : 9);
        if (Stretching) { R("#EABA91", x - 4, y + 2, 4, 12); R("#EABA91", x + 11, y + 2, 4, 12); R("#EABA91", x - 6, y, 6, 3); R("#EABA91", x + 11, y, 6, 3); }
        else { R("#EABA91", x + 8, y + 12, 4, 5); R("#EABA91", x + 10, y + 15, 10, 3); }
        if (Standing) { R("#5777A4", x, y + 23, 4, 11); R("#5777A4", x + 7, y + 23, 4, 11); R("#E6E7EB", x, 42, 6, 2); R("#E6E7EB", x + 7, 42, 6, 2); }
        else { R("#5777A4", x, 37, 15, 4); R("#5777A4", x + 11, 39, 4, 4); R("#E6E7EB", x + 11, 42, 7, 2); }
        if (Resting) { R("#CFC3E9", 66, 12, 5, 1); R("#CFC3E9", 70, 13, 1, 2); R("#CFC3E9", 66, 15, 5, 1); }
    }
}
