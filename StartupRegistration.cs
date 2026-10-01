using Microsoft.Win32;

namespace StandUpHero;

internal sealed class StartupPreferenceWriter(Func<string?> read, Action<string?> write, Func<string> executable)
{
    internal static string BuildCommand(string path)
    {
        if (!Path.IsPathFullyQualified(path) || !path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || path.IndexOfAny(['"', '\r', '\n']) >= 0)
            throw new IOException("O caminho do executável não é válido para iniciar com o Windows.");
        string command = $"\"{path}\" --startup";
        if (command.Length > 260) throw new IOException("Mova o aplicativo para uma pasta com um caminho mais curto antes de ativar a inicialização automática.");
        return command;
    }
    internal void Save(Preferences preferences, Action<Preferences> saveFile)
    {
        var previous = read();
        var desired = preferences.StartWithWindows ? BuildCommand(executable()) : null;
        bool changed = previous != desired;
        if (changed) write(desired);
        try { saveFile(preferences); }
        catch (Exception saveError)
        {
            if (changed)
            {
                try { write(previous); }
                catch (Exception rollbackError) { throw new IOException("Não foi possível salvar nem restaurar a opção de inicialização. Confira-a nas configurações.", new AggregateException(saveError, rollbackError)); }
            }
            throw;
        }
    }
}

internal static class StartupRegistration
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "StandUpHero";
    private static string? Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        return key?.GetValue(ValueName) as string;
    }
    private static void Write(string? command)
    {
        if (command is null)
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        else
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);
            key.SetValue(ValueName, command, RegistryValueKind.String);
        }
    }
    internal static void Save(Preferences preferences)
    {
        var writer = new StartupPreferenceWriter(Read, Write, () =>
        {
            string path = Path.Combine(AppContext.BaseDirectory, "StandUpHero.exe");
            if (!File.Exists(path)) throw new IOException("Não foi encontrado o executável para iniciar com o Windows.");
            return path;
        });
        writer.Save(preferences, PreferenceStore.Save);
    }
}
