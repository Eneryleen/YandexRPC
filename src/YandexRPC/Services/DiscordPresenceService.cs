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

    // buttons — кандидаты в порядке приоритета; Discord берёт максимум 2
    public void Update(TrackInfo t, IReadOnlyList<(string Label, string Url)> buttons)
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

        var valid = new List<DiscordRPC.Button>(2);
        foreach (var (label, url) in buttons)
        {
            if (valid.Count == 2) break;
            if (!IsHttp(url) || url.Length > 512) continue;
            var lab = ByteCap(label, 31); // лимит метки кнопки Discord — 31 байт UTF-8
            if (lab.Length == 0) continue;
            valid.Add(new DiscordRPC.Button { Label = lab, Url = url });
        }
        if (valid.Count > 0) presence.Buttons = valid.ToArray();

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

    private static string SafeImage(string? url) =>
        IsHttps(url) && url!.Length <= 256 ? url : "logo";

    private static bool IsHttps(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttps;

    private static bool IsHttp(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) &&
        (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);

    private static string ByteCap(string s, int maxBytes)
    {
        s = s.Trim();
        if (System.Text.Encoding.UTF8.GetByteCount(s) <= maxBytes) return s;
        int bytes = 0, i = 0;
        while (i < s.Length)
        {
            // шаг по кодовой точке, чтобы не разрезать суррогатную пару
            int step = char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]) ? 2 : 1;
            int rb = System.Text.Encoding.UTF8.GetByteCount(s.Substring(i, step));
            if (bytes + rb > maxBytes) break;
            bytes += rb;
            i += step;
        }
        return s[..i];
    }

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
