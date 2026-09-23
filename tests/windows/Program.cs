using System.Diagnostics;
using StandUpHero;

internal static class Program
{
    private static int checks;
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
        Console.WriteLine($"{checks} lifecycle checks passed.");
        return 0;
    }
}
