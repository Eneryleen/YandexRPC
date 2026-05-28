using Windows.Foundation;
using Windows.Media.Control;
using YandexRPC.Models;

namespace YandexRPC.Services;

using Session = GlobalSystemMediaTransportControlsSession;
using Manager = GlobalSystemMediaTransportControlsSessionManager;

public sealed class MediaMonitor : IDisposable
{
    private static readonly string[] YandexIds =
    {
        "ru.yandex.desktop.music", "Yandex.Music", "YandexMusic", "Яндекс", "Yandex"
    };

    private readonly SynchronizationContext? _ui;
    private readonly TypedEventHandler<Session, MediaPropertiesChangedEventArgs> _onMedia;
    private readonly TypedEventHandler<Session, PlaybackInfoChangedEventArgs> _onPlayback;
    private readonly TypedEventHandler<Session, TimelinePropertiesChangedEventArgs> _onTimeline;
    private readonly TypedEventHandler<Manager, SessionsChangedEventArgs> _onSessionsChanged;

    private Manager? _manager;
    private Session? _session;
    private bool _disposed;

    public event Action<TrackInfo?>? Changed;

    public MediaMonitor()
    {
        _ui = SynchronizationContext.Current;
        _onMedia = (_, _) => RequestUpdate();
        _onPlayback = (_, _) => RequestUpdate();
        _onTimeline = (_, _) => RequestUpdate();
        _onSessionsChanged = (_, _) => RebindOnUi();
    }

    public async Task StartAsync()
    {
        _manager = await Manager.RequestAsync();
        _manager.SessionsChanged += _onSessionsChanged;
        Rebind();
    }

    private void RebindOnUi()
    {
        if (_ui != null) _ui.Post(_ => Rebind(), null);
        else Rebind();
    }

    private void Rebind()
    {
        var found = FindYandex();
        if (ReferenceEquals(found, _session)) return;

        Unhook();
        _session = found;
        if (_session != null)
        {
            _session.MediaPropertiesChanged += _onMedia;
            _session.PlaybackInfoChanged += _onPlayback;
            _session.TimelinePropertiesChanged += _onTimeline;
        }
        RequestUpdate();
    }

    private Session? FindYandex()
    {
        if (_manager == null) return null;
        foreach (var s in _manager.GetSessions())
        {
            var id = s.SourceAppUserModelId ?? "";
            foreach (var y in YandexIds)
                if (id.Contains(y, StringComparison.OrdinalIgnoreCase))
                    return s;
        }
        return null;
    }

    private void RequestUpdate() => _ = UpdateAsync();

    private async Task UpdateAsync()
    {
        try
        {
            var s = _session;
            if (s == null) { Emit(null); return; }
            var props = await s.TryGetMediaPropertiesAsync();
            if (props == null || string.IsNullOrWhiteSpace(props.Title)) { Emit(null); return; }

            var pb = s.GetPlaybackInfo();
            var tl = s.GetTimelineProperties();
            bool playing = pb?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

            var dur = tl == null ? TimeSpan.Zero : tl.EndTime - tl.StartTime;
            var pos = tl == null ? TimeSpan.Zero : tl.Position - tl.StartTime;
            // SMTC отдаёт позицию на момент LastUpdatedTime — докручиваем до «сейчас», пока играет
            if (playing && tl != null && tl.LastUpdatedTime.Year > 2000)
                pos += DateTimeOffset.UtcNow - tl.LastUpdatedTime;
            if (pos < TimeSpan.Zero) pos = TimeSpan.Zero;
            if (dur < TimeSpan.Zero) dur = TimeSpan.Zero;
            if (dur > TimeSpan.Zero && pos > dur) pos = dur;

            Emit(new TrackInfo
            {
                Title = props.Title ?? "",
                Artist = props.Artist ?? "",
                Album = props.AlbumTitle ?? "",
                IsPlaying = playing,
                Position = pos,
                Duration = dur
            });
        }
        catch
        {
            // SMTC бросает при гонке событий смены трека — игнорируем, придёт следующее событие
        }
    }

    private void Emit(TrackInfo? info)
    {
        if (_disposed) return;
        if (_ui != null) _ui.Post(_ => { if (!_disposed) Changed?.Invoke(info); }, null);
        else Changed?.Invoke(info);
    }

    private void Unhook()
    {
        if (_session == null) return;
        _session.MediaPropertiesChanged -= _onMedia;
        _session.PlaybackInfoChanged -= _onPlayback;
        _session.TimelinePropertiesChanged -= _onTimeline;
        _session = null;
    }

    public void Dispose()
    {
        _disposed = true;
        if (_manager != null) _manager.SessionsChanged -= _onSessionsChanged;
        Unhook();
    }
}
