using System.Net.Http;
using System.Text.Json;
using YandexRPC.Models;

namespace YandexRPC.Services;

// Дотягивает обложку и ссылку на трек через публичный поиск Яндекса (без токена)
public sealed class YandexMetadataService
{
    private const int MaxCache = 500;
    private static readonly HttpClient Http = CreateHttp();
    private readonly Dictionary<string, (string? cover, string? url)> _cache = new();

    private static HttpClient CreateHttp()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        var h = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(8),
            MaxResponseContentBufferSize = 2 * 1024 * 1024
        };
        h.DefaultRequestHeaders.UserAgent.ParseAdd("YandexRPC/1.0");
        return h;
    }

    public async Task EnrichAsync(TrackInfo t)
    {
        var key = $"{t.Title}|{t.Artist}";
        if (_cache.TryGetValue(key, out var hit))
        {
            t.CoverUrl = hit.cover;
            t.TrackUrl = hit.url;
            return;
        }

        var query = Uri.EscapeDataString($"{t.Artist} {t.Title}".Trim());
        var url = $"https://api.music.yandex.net/search?text={query}&type=track&page=0&nocorrect=false";

        try
        {
            using var resp = await Http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            await using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            var (cover, link) = Parse(doc.RootElement);
            if (_cache.Count >= MaxCache) _cache.Clear();
            _cache[key] = (cover, link);
            t.CoverUrl = cover;
            t.TrackUrl = link;
        }
        catch
        {
            // сеть/парсинг упали — не кэшируем, попробуем на следующей смене трека
            t.CoverUrl = null;
            t.TrackUrl = null;
        }
    }

    private static (string?, string?) Parse(JsonElement root)
    {
        if (!root.TryGetProperty("result", out var result) ||
            !result.TryGetProperty("tracks", out var tracks) ||
            !tracks.TryGetProperty("results", out var results) ||
            results.GetArrayLength() == 0)
            return (null, null);

        var best = results[0];

        string? cover = null;
        if (best.TryGetProperty("coverUri", out var cu) && cu.ValueKind == JsonValueKind.String)
            cover = BuildCover(cu.GetString());

        long? albumId = null;
        if (best.TryGetProperty("albums", out var albums) && albums.GetArrayLength() > 0)
        {
            var album = albums[0];
            if (cover == null && album.TryGetProperty("coverUri", out var acu) && acu.ValueKind == JsonValueKind.String)
                cover = BuildCover(acu.GetString());
            if (album.TryGetProperty("id", out var aid))
                albumId = ReadId(aid);
        }

        string? link = null;
        if (albumId is > 0 && best.TryGetProperty("id", out var tid))
        {
            var trackId = ReadId(tid);
            if (trackId is > 0)
                link = $"https://music.yandex.ru/album/{albumId}/track/{trackId}";
        }

        return (cover, link);
    }

    // coverUri вида "avatars.yandex.net/.../%%"; принимаем только https-хост *.yandex.net
    private static string? BuildCover(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        var url = "https://" + raw.Replace("%%", "400x400");
        return Uri.TryCreate(url, UriKind.Absolute, out var u) &&
               u.Scheme == Uri.UriSchemeHttps &&
               u.Host.EndsWith(".yandex.net", StringComparison.OrdinalIgnoreCase) &&
               url.Length <= 256
            ? url : null;
    }

    private static long? ReadId(JsonElement e)
    {
        if (e.ValueKind == JsonValueKind.Number) return e.GetInt64();
        return long.TryParse(e.GetString(), out var v) ? v : null;
    }
}
