namespace YandexRPC.Models;

public sealed class TrackInfo
{
    public string Title { get; init; } = "";
    public string Artist { get; init; } = "";
    public string Album { get; init; } = "";
    public bool IsPlaying { get; init; }
    public TimeSpan Position { get; init; }
    public TimeSpan Duration { get; init; }

    public string? CoverUrl { get; set; }
    public string? TrackUrl { get; set; }

    // длительность в ключе: когда SMTC доберёт её после смены трека — статус переотправится с прогресс-баром
    public string Signature => $"{Title}|{Artist}|{Album}|{IsPlaying}|{Duration > TimeSpan.Zero}";
}
