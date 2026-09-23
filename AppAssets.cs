using System.Media;

namespace StandUpHero;

internal static class AppAssets
{
    public static Icon Icon { get; } = LoadIcon();
    private static Icon LoadIcon()
    {
        using var stream = typeof(AppAssets).Assembly.GetManifestResourceStream("StandUpHero.Assets.StandUpHero.ico")!;
        return new Icon(stream, 32, 32);
    }
}

internal sealed class PostureAudio : IDisposable
{
    private readonly Dictionary<string, SoundPlayer> players = new();
    private readonly List<Stream> streams = new();
    public PostureAudio()
    {
        string[] files = ["rise", "fall", "bell", "arcade"];
        for (int i = 0; i < files.Length; i++)
        {
            var stream = typeof(PostureAudio).Assembly.GetManifestResourceStream($"StandUpHero.Assets.{files[i]}.wav")!;
            streams.Add(stream);
            var player = new SoundPlayer(stream);
            player.Load();
            players.Add(Preferences.SoundChoices[i], player);
        }
    }
    public void Play(string choice)
    {
        foreach (var player in players.Values) player.Stop();
        if (players.TryGetValue(choice, out var selected)) selected.Play();
    }
    public void Dispose()
    {
        foreach (var player in players.Values) { player.Stop(); player.Dispose(); }
        foreach (var stream in streams) stream.Dispose();
    }
}
