using System.Text.Json;

namespace StandUpHero;

public sealed record TimeSlot(TimeSpan Start, TimeSpan End);

public sealed class Preferences
{
    public int SittingMinutes { get; set; } = 45;
    public int StandingMinutes { get; set; } = 15;
    public DayOfWeek[] Days { get; set; } = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
    public List<TimeSlot> Slots { get; set; } = [new(TimeSpan.FromHours(9), TimeSpan.FromHours(18))];
    public bool AlwaysOnTop { get; set; } = true;
    public bool Sound { get; set; } = true;
    public string Character { get; set; } = "Homem";
    public string StandSound { get; set; } = "Subida suave";
    public string SitSound { get; set; } = "Descida suave";
    public static readonly string[] SoundChoices = ["Subida suave", "Descida suave", "Sino", "Arcade", "Sem som"];
    public string Activity { get; set; } = "Trabalhando";

    public Preferences WithDefaultRoutine()
    {
        var restored = (Preferences)MemberwiseClone();
        var defaults = new Preferences();
        restored.SittingMinutes = defaults.SittingMinutes;
        restored.StandingMinutes = defaults.StandingMinutes;
        restored.Days = defaults.Days;
        restored.Slots = defaults.Slots;
        return restored;
    }

    public bool IsValid() => SittingMinutes is >= 1 and <= 240 && StandingMinutes is >= 1 and <= 240
        && Days is { Length: > 0 } && Days.All(d => (int)d is >= 0 and <= 6)
        && Slots is { Count: > 0 } && Slots.All(s => s.Start >= TimeSpan.Zero && s.End <= TimeSpan.FromDays(1) && s.Start < s.End)
        && Activity is "Trabalhando" or "Jogando" or "Relaxando"
        && Character is "Homem" or "Mulher" && SoundChoices.Contains(StandSound) && SoundChoices.Contains(SitSound);

    public DateTime? WindowStart(DateTime now)
    {
        if (!Days.Contains(now.DayOfWeek)) return null;
        // Merge overlapping or adjacent windows so an overlap never restarts a cycle.
        DateTime? start = null;
        var end = TimeSpan.Zero;
        foreach (var slot in Slots.OrderBy(s => s.Start))
        {
            if (start is null || slot.Start > end)
            {
                if (start is not null && now.TimeOfDay >= start.Value.TimeOfDay && now.TimeOfDay < end) return start;
                start = now.Date + slot.Start;
                end = slot.End;
            }
            else if (slot.End > end) end = slot.End;
        }
        return start is not null && now.TimeOfDay >= start.Value.TimeOfDay && now.TimeOfDay < end ? start : null;
    }
}

public sealed class Routine
{
    public Preferences Settings { get; private set; }
    public bool Paused { get; private set; }
    public bool Standing { get; private set; }
    public bool Active { get; private set; }
    public TimeSpan Remaining { get; private set; }
    private DateTime? window;
    private DateTimeOffset deadline;
    public Routine(Preferences settings) { Settings = settings; }
    private TimeSpan Duration => TimeSpan.FromMinutes(Standing ? Settings.StandingMinutes : Settings.SittingMinutes);
    public void Configure(Preferences settings) { Settings = settings; Reset(); }
    public void Reset() { window = null; Active = false; Standing = false; Paused = false; Remaining = Duration; }
    public void TogglePause(DateTimeOffset now)
    {
        if (!Active) return;
        if (Paused) deadline = now + Remaining;
        else Remaining = deadline > now ? deadline - now : TimeSpan.Zero;
        Paused = !Paused;
    }
    public void Skip(DateTimeOffset now)
    {
        if (!Active) return;
        Standing = !Standing; Remaining = Duration; deadline = now + Remaining;
    }
    public bool Tick(DateTimeOffset now)
    {
        var currentWindow = Settings.WindowStart(now.LocalDateTime);
        if (Settings.Activity == "Relaxando" || currentWindow is null) { Reset(); return false; }
        if (!Active || currentWindow != window)
        {
            Active = true; window = currentWindow; Standing = false; Paused = false;
            deadline = now + Duration;
        }
        if (Paused) return false;
        bool changed = false;
        if (now >= deadline)
        {
            // After sleep, start one fresh interval; don't replay missed reminders.
            Standing = !Standing; deadline = now + Duration; changed = true;
        }
        Remaining = deadline - now;
        return changed;
    }
}

public static class PreferenceStore
{
    private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StandUpHero", "settings.json");
    public static Preferences Load()
    {
        try { var p = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(FilePath)); return p is not null && p.IsValid() ? p : new(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or ArgumentException) { return new(); }
    }
    public static void Save(Preferences settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath + ".tmp", JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(FilePath + ".tmp", FilePath, true);
    }
}
