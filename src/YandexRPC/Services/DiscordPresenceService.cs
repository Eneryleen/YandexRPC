using DiscordRPC;
using YandexRPC.Models;

namespace YandexRPC.Services;

public sealed class DiscordPresenceService : IDisposable
{
    private DiscordRpcClient? _client;
    private string _appId = "";

    public void Connect(string? appId)
    {
        var id = appId?.Trim() ?? "";
        if (_client != null && _appId == id) return;
        Disconnect();
        if (id.Length == 0) return;
        try
        {
            _client = new DiscordRpcClient(id);
            _client.Initialize();
            _appId = id;
        }
        catch
        {
            Disconnect();
        }
    }

    public void Update(TrackInfo t)
    {
        if (_client == null) return;

        var presence = new RichPresence
        {
            Type = ActivityType.Listening,
            Details = Clean(t.Title, 128) ?? "Неизвестный трек",
            State = Clean(t.Artist, 128),
            Assets = new Assets
            {
                LargeImageKey = SafeImage(t.CoverUrl),
                LargeImageText = Clean(string.IsNullOrEmpty(t.Album) ? "Яндекс Музыка" : t.Album, 128),
                SmallImageKey = t.IsPlaying ? "play" : "pause",
                SmallImageText = t.IsPlaying ? "Играет" : "Пауза"
            }
        };

        if (t.IsPlaying && t.Duration > TimeSpan.Zero)
        {
            var now = DateTime.UtcNow;
            presence.Timestamps = new Timestamps
            {
                Start = now - t.Position,
                End = now + (t.Duration - t.Position)
            };
        }

        if (IsHttp(t.TrackUrl) && t.TrackUrl!.Length <= 512)
            presence.Buttons = new[] { new DiscordRPC.Button { Label = "Открыть", Url = t.TrackUrl } };

        try { _client.SetPresence(presence); } catch { }
    }

    public void Clear()
    {
        try { _client?.ClearPresence(); } catch { }
    }

    public void Disconnect()
    {
        _client?.Dispose();
        _client = null;
        _appId = "";
    }

    public void Dispose() => Disconnect();

    // только https и не длиннее лимита Discord — иначе библиотека бросает исключение на чужой/битой строке
    private static string SafeImage(string? url) =>
        IsHttps(url) && url!.Length <= 256 ? url : "logo";

    private static bool IsHttps(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttps;

    private static bool IsHttp(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) &&
        (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);

    // Discord требует у Details/State минимум 2 символа либо null
    private static string? Clean(string s, int max)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        if (s.Length > max)
        {
            s = s[..max];
            if (char.IsHighSurrogate(s[^1])) s = s[..^1]; // не рвём суррогатную пару
        }
        return s.Length < 2 ? null : s;
    }
}
