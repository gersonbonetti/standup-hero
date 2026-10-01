using System.Diagnostics;
using System.Runtime.InteropServices;
using StandUpHero;

internal static class Program
{
    private static int checks;
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        Console.WriteLine("OK " + name); checks++;
    }

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--try-instance")
        {
            using var secondary = new SingleInstance(args[1]);
            if (secondary.IsPrimary) return 2;
            secondary.RequestShow();
            return 0;
        }
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        string name = "StandUpHero.Test." + Guid.NewGuid().ToString("N");
        using (var primary = new SingleInstance(name))
        {
            Check(primary.IsPrimary, "First process owns the instance lock");
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true };
            start.ArgumentList.Add("--try-instance"); start.ArgumentList.Add(name);
            using var child = Process.Start(start)!;
            Check(child.WaitForExit(10000) && child.ExitCode == 0, "Second process exits without creating a timer");
            Check(primary.ConsumeShowRequest(), "Second launch requests the existing mascot");
        }
        using (var reopened = new SingleInstance(name)) Check(reopened.IsPrimary, "Closing releases the instance lock");

        Preferences ShortRoutine() => new() { SittingMinutes = 1, StandingMinutes = 1, Sound = false, Character = "Mulher", StandSound = "Arcade" };
        Preferences? persisted = null;
        using (var hero = new HeroWindow(ShortRoutine(), p => persisted = p))
        {
            hero.Show();
            using var dialog = new SettingsWindow(hero.CurrentRoutine.Settings, hero.ApplyPreferences);
            dialog.Show();
            var page = dialog.Controls.OfType<TabControl>().Single().TabPages[0];
            page.Controls.OfType<Button>().Single(b => b.Text == "Restaurar rotina padrão").PerformClick();
            dialog.Close(); // Intentionally do not press Save.
            Check(persisted is { SittingMinutes: 45, StandingMinutes: 15 }, "Reset saves immediately, even when settings are closed without Save");
            Check(hero.CurrentRoutine.Settings.Character == "Mulher" && hero.CurrentRoutine.Settings.StandSound == "Arcade", "Reset preserves saved character and sound");
            var monday = new DateTimeOffset(new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Local));
            hero.CurrentRoutine.Reset(); hero.CurrentRoutine.Tick(monday);
            Check(!hero.CurrentRoutine.Tick(monday.AddMinutes(1)) && hero.CurrentRoutine.Remaining.TotalMinutes == 44, "Reset replaces the previous one-minute deadline");
            Check(hero.CurrentRoutine.Tick(monday.AddMinutes(45)) && hero.CurrentRoutine.Remaining.TotalMinutes == 15, "Reset uses the full standing interval");
            hero.Hide();
            hero.Controls.OfType<PixelScene>().Single().ContextMenuStrip!.Items.OfType<ToolStripMenuItem>()
                .Single(item => item.Text == "Encerrar aplicativo").PerformClick();
            Check(hero.ResourcesDisposed && !hero.TimerRunning, "Exit from the tray menu stops resources even while hidden");
        }

        using (var hero = new HeroWindow(ShortRoutine(), _ => { }))
        {
            hero.Show();
            using var click = new System.Windows.Forms.Timer { Interval = 50 };
            click.Tick += (_, _) =>
            {
                click.Stop();
                Application.OpenForms.OfType<SettingsWindow>().Single().Controls.OfType<Button>()
                    .Single(button => button.Text == "Encerrar aplicativo").PerformClick();
            };
            click.Start(); hero.EditSettings();
            Check(hero.ResourcesDisposed && !hero.TimerRunning && !Application.OpenForms.OfType<SettingsWindow>().Any(), "Exit from settings closes dialog, mascot, and timer");
        }
        using (var hero = new HeroWindow(ShortRoutine(), _ => { }))
        {
            hero.Show(); hero.Close();
            Check(hero.ResourcesDisposed && !hero.TimerRunning, "Normal close releases all application resources");
        }
        var disposable = new HeroWindow(ShortRoutine(), _ => { });
        disposable.Dispose(); disposable.Dispose();
        Check(disposable.ResourcesDisposed && !disposable.TimerRunning, "Dispose without showing is silent and idempotent");

        using (var notice = new VisualNotice())
        {
            var screen = new Rectangle(-1920, 0, 1920, 1040);
            var anchor = new Rectangle(-140, 944, 140, 96);
            var foreground = GetForegroundWindow();
            notice.Present(true, 12, anchor, screen);
            Check(GetForegroundWindow() == foreground, "Showing the visual notice preserves the foreground window");
            Check(notice.Pending && !notice.Compact && notice.Visible && notice.DoesNotActivate && !notice.ShowInTaskbar, "Notice is visible without activation or taskbar entry");
            Check(screen.Contains(notice.Bounds) && notice.Bottom < anchor.Top, "Notice stays above mascot inside a negative-coordinate monitor");
            notice.Advance(TimeSpan.FromSeconds(0.6)); Check(notice.Pulse > 0.99, "First gentle pulse");
            notice.Advance(TimeSpan.FromSeconds(1.8)); Check(notice.Pulse > 0.99, "Second gentle pulse");
            notice.Advance(TimeSpan.FromSeconds(3)); Check(notice.Pulse == 0, "Pulsing stops after two pulses");
            notice.Advance(TimeSpan.FromSeconds(11.9)); Check(!notice.Compact, "Balloon respects selected duration");
            notice.Advance(TimeSpan.FromSeconds(12)); Check(notice.Compact && notice.Pending, "Balloon becomes a persistent badge");
            notice.Advance(TimeSpan.FromHours(2)); Check(notice.Pending, "Badge waits for dismissal");
            notice.Dismiss(); Check(!notice.Visible && !notice.Pending, "Dismiss clears and hides the notice");
            notice.Present(false, 5, new Rectangle(-1920, 0, 140, 96), screen);
            Check(!notice.Compact && screen.Contains(notice.Bounds) && notice.Top >= 96, "Next transition expands the badge and fits below a top-edge mascot");
        }
        using (var hero = new HeroWindow(ShortRoutine(), _ => { }))
        {
            var monday = new DateTimeOffset(new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Local));
            hero.CurrentRoutine.Reset(); hero.RefreshRoutine(monday); hero.RefreshRoutine(monday.AddMinutes(1));
            Check(hero.CurrentNotice.Pending, "Timed transition shows a notice even with sound disabled");
            var disabled = ShortRoutine(); disabled.VisualAlerts = false;
            hero.ApplyPreferences(disabled);
            Check(!hero.CurrentNotice.Pending, "Saving preferences clears stale notices");
            hero.CurrentRoutine.Reset(); hero.RefreshRoutine(monday); hero.RefreshRoutine(monday.AddMinutes(1));
            Check(!hero.CurrentNotice.Pending, "Disabled visual alerts stay hidden at a transition");
            var remaining = hero.CurrentRoutine.Remaining;
            hero.PreviewNotice(false, 10);
            Check(hero.CurrentNotice.Pending && hero.CurrentRoutine.Remaining == remaining, "Manual preview leaves the cycle unchanged");
            hero.Dispose();
            Check(hero.CurrentNotice.IsDisposed, "Shutdown disposes the persistent notice");
        }
        using (var settings = new SettingsWindow(new Preferences(), p => { persisted = p; return true; }))
        {
            settings.Show();
            var tabs = settings.Controls.OfType<TabControl>().Single(); tabs.SelectedIndex = 2;
            var visualPage = tabs.TabPages[2];
            visualPage.Controls.OfType<NumericUpDown>().Single().Value = 20;
            int previewSeconds = 0;
            settings.VisualPreviewRequested += (_, seconds) => previewSeconds = seconds;
            visualPage.Controls.OfType<Button>().Single(b => b.Text == "Testar: levantar").PerformClick();
            Check(previewSeconds == 20, "Preview uses the unsaved duration from settings");
            visualPage.Controls.OfType<CheckBox>().Single().Checked = false;
            settings.Controls.OfType<Button>().Single(b => b.Text == "Salvar rotina").PerformClick();
            Check(persisted is { VisualAlerts: false, VisualAlertSeconds: 20 }, "Settings save the visual toggle and duration");
        }
        var observedIdle = TimeSpan.FromMinutes(11);
        int idleReads = 0;
        Preferences ActiveRoutine() => new() { Sound = false, Days = Enum.GetValues<DayOfWeek>(), Slots = [new(TimeSpan.Zero, TimeSpan.FromDays(1))] };
        using (var hero = new HeroWindow(ActiveRoutine(), p => persisted = p, () => { idleReads++; return observedIdle; }))
        {
            hero.Show();
            var activityMenu = hero.Controls.OfType<PixelScene>().Single().ContextMenuStrip!.Items.OfType<ToolStripMenuItem>().Single(m => m.Text == "Atividade");
            Check(activityMenu.DropDownItems.Count == 2 && activityMenu.DropDownItems[0].Text == "Trabalhando" && activityMenu.DropDownItems[1].Text == "Relaxando", "Only working and relaxing are offered");
            hero.CheckIdle(TimeSpan.FromMinutes(11));
            Check(!hero.CurrentIdleNotice.Visible && hero.CurrentRoutine.Settings.Activity == "Trabalhando", "Idle time alone never changes status or displays an away prompt");
            observedIdle = TimeSpan.Zero;
            var foreground = GetForegroundWindow();
            var remaining = hero.CurrentRoutine.Remaining;
            hero.CheckIdle(TimeSpan.FromMinutes(11.1));
            Check(hero.CurrentIdleNotice.Visible && hero.CurrentIdleNotice.DoesNotActivate && GetForegroundWindow() == foreground, "Return suggestion does not take focus");
            Check(hero.CurrentRoutine.Settings.Activity == "Trabalhando" && hero.CurrentRoutine.Remaining == remaining, "A suggestion leaves status and countdown unchanged");
            hero.CurrentIdleNotice.Controls.OfType<Button>().Single(b => b.Text == "Continuar trabalhando").PerformClick();
            Check(!hero.CurrentIdleNotice.Visible && hero.CurrentRoutine.Remaining == remaining, "Declining dismisses the suggestion without restarting the timer");
            observedIdle = TimeSpan.FromMinutes(11); hero.CheckIdle(TimeSpan.FromMinutes(45));
            observedIdle = TimeSpan.Zero; hero.CheckIdle(TimeSpan.FromMinutes(45.1));
            hero.CurrentIdleNotice.Controls.OfType<Button>().Single(b => b.Text == "Relaxar").PerformClick();
            Check(persisted?.Activity == "Relaxando" && !hero.CurrentRoutine.Active && !hero.CurrentIdleNotice.Visible, "Only explicit acceptance saves Relaxing and stops the cycle");
            int readsBefore = idleReads; hero.CheckIdle(TimeSpan.FromMinutes(46));
            Check(idleReads == readsBefore, "Relaxing does not query user input timestamps");
            var disabled = ActiveRoutine(); disabled.SuggestRelaxing = false; hero.ApplyPreferences(disabled);
            hero.CheckIdle(TimeSpan.FromMinutes(60));
            Check(idleReads == readsBefore, "Disabled detection makes no input timestamp queries");
            hero.ApplyPreferences(ActiveRoutine()); hero.CurrentRoutine.TogglePause(DateTimeOffset.Now);
            hero.CheckIdle(TimeSpan.FromMinutes(61));
            Check(idleReads == readsBefore, "Paused routine makes no input timestamp queries");
            hero.Dispose();
            Check(!hero.IdleTimerRunning && hero.CurrentIdleNotice.IsDisposed, "Shutdown stops detection and disposes its suggestion window");
        }
        using (var settings = new SettingsWindow(new Preferences(), p => { persisted = p; return true; }))
        {
            settings.Show();
            var tabs = settings.Controls.OfType<TabControl>().Single(); tabs.SelectedIndex = 3;
            tabs.TabPages[3].Controls.OfType<NumericUpDown>().Single().Value = 25;
            tabs.TabPages[3].Controls.OfType<CheckBox>().Single().Checked = false;
            settings.Controls.OfType<Button>().Single(b => b.Text == "Salvar rotina").PerformClick();
            Check(persisted is { SuggestRelaxing: false, IdleMinutes: 25 }, "Settings persist detection toggle and threshold");
        }
        string? startupEntry = null;
        int writes = 0, preferenceSaves = 0;
        var startup = new StartupPreferenceWriter(() => startupEntry, command => { startupEntry = command; writes++; }, () => @"C:\Mascot Folder\StandUpHero.exe");
        startup.Save(new Preferences(), _ => preferenceSaves++);
        Check(writes == 0 && preferenceSaves == 1, "Startup remains disabled without writing an autorun entry");
        startup.Save(new Preferences { StartWithWindows = true }, _ => preferenceSaves++);
        Check(startupEntry == "\"C:\\Mascot Folder\\StandUpHero.exe\" --startup", "Startup quotes executable paths with spaces");
        startup.Save(new Preferences { StartWithWindows = false }, _ => preferenceSaves++);
        Check(startupEntry is null && preferenceSaves == 3, "Disabling startup removes its entry");
        startupEntry = "previous-command";
        try { startup.Save(new Preferences { StartWithWindows = true }, _ => throw new IOException("Simulated save failure")); }
        catch (IOException) { }
        Check(startupEntry == "previous-command", "Failed settings save rolls back the startup entry");
        int failedSaveCount = 0;
        var deniedStartup = new StartupPreferenceWriter(() => null, _ => throw new UnauthorizedAccessException(), () => @"C:\Mascot Folder\StandUpHero.exe");
        try { deniedStartup.Save(new Preferences { StartWithWindows = true }, _ => failedSaveCount++); }
        catch (UnauthorizedAccessException) { }
        Check(failedSaveCount == 0, "Registration failure cannot save a misleading enabled preference");
        bool rejected = false;
        try { StartupPreferenceWriter.BuildCommand("relative.exe"); } catch (IOException) { rejected = true; }
        Check(rejected, "Startup refuses relative executable paths");

        using (var settings = new SettingsWindow(new Preferences(), p => { persisted = p; return true; }))
        {
            settings.Show();
            var tabs = settings.Controls.OfType<TabControl>().Single(); tabs.SelectedIndex = 4;
            var toggles = tabs.TabPages[4].Controls.OfType<CheckBox>().ToArray();
            Check(!toggles.Single(c => c.Text == "Iniciar com o Windows").Checked, "Startup checkbox is opt-in");
            toggles.Single(c => c.Text == "Iniciar com o Windows").Checked = true;
            toggles.Single(c => c.Text == "Pausar ao bloquear o computador").Checked = false;
            settings.Controls.OfType<Button>().Single(b => b.Text == "Salvar rotina").PerformClick();
            Check(persisted is { StartWithWindows: true, PauseOnLock: false }, "System options save through the settings dialog");
        }
        using (var hero = new HeroWindow(ActiveRoutine(), _ => { }, () => throw new Exception("Input must not be queried while locked")))
        {
            hero.Show();
            var now = DateTimeOffset.Now;
            hero.HandleSessionLock(true, now);
            var remaining = hero.CurrentRoutine.Remaining;
            hero.CheckIdle(TimeSpan.Zero);
            Check(hero.CurrentRoutine.SessionLocked && hero.CurrentRoutine.Paused && !hero.CurrentNotice.Pending, "Lock pauses the live app and suppresses input polling and notices");
            var foreground = GetForegroundWindow();
            hero.HandleSessionLock(false, now);
            Check(hero.CurrentResumeNotice.Visible && GetForegroundWindow() == foreground && hero.CurrentRoutine.Remaining == remaining, "Unlock asks to resume without focus theft or countdown loss");
            hero.CurrentResumeNotice.Controls.OfType<Button>().Single(b => b.Text == "Manter pausado").PerformClick();
            Check(!hero.CurrentResumeNotice.Visible && hero.CurrentRoutine.Paused, "Keep-paused choice leaves the routine stopped");
            hero.HandleSessionLock(false, now);
            Check(!hero.CurrentResumeNotice.Visible, "Duplicate unlock notifications do not reopen the prompt");
            hero.CurrentRoutine.TogglePause(now);
            hero.HandleSessionLock(true, now); hero.HandleSessionLock(false, now);
            hero.CurrentResumeNotice.Controls.OfType<Button>().Single(b => b.Text == "Continuar").PerformClick();
            Check(!hero.CurrentRoutine.Paused && !hero.CurrentResumeNotice.Visible, "Continue button resumes and closes the prompt");
            hero.CurrentRoutine.TogglePause(now);
            hero.HandleSessionLock(true, now); hero.HandleSessionLock(false, now);
            Check(hero.CurrentRoutine.Paused && !hero.CurrentResumeNotice.Visible, "A pre-existing manual pause remains silent after unlock");
            hero.Dispose();
            Check(hero.CurrentResumeNotice.IsDisposed, "Shutdown disposes the unlock prompt");
        }
        using (var hero = new HeroWindow(ActiveRoutine(), _ => { }))
        {
            hero.Show();
            var now = DateTimeOffset.Now;
            // Expire this stage without making the wall clock or configured window change.
            hero.CurrentRoutine.Skip(now.AddMinutes(-46));
            hero.RefreshRoutine(now);
            Check(hero.CurrentNotice.Controls.OfType<Button>().Single().Enabled, "Real posture reminder offers snooze");
            hero.CurrentNotice.Controls.OfType<Button>().Single().PerformClick();
            Check(hero.CurrentRoutine.Snoozed && hero.CurrentRoutine.Remaining.TotalMinutes == 5 && !hero.CurrentNotice.Pending, "Snooze button replaces the deadline and closes the reminder");
            hero.PreviewNotice(true, 12);
            Check(!hero.CurrentNotice.Controls.OfType<Button>().Single().Enabled && !hero.SnoozeNotice(now), "A visual preview cannot snooze the real cycle");
        }
        Console.WriteLine($"{checks} lifecycle checks passed.");
        return 0;
    }
}
