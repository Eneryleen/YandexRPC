using System.Reflection;
using YandexRPC.Models;
using YandexRPC.Services;
using YandexRPC.Settings;

namespace YandexRPC;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly AppSettings _settings;
    private readonly NotifyIcon _tray;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly Icon? _ownedIcon;
    private readonly YandexMetadataService _yandex = new();
    private readonly DiscordPresenceService _discord = new();
    private readonly System.Windows.Forms.Timer _debounce = new() { Interval = 1500 };
    private readonly System.Windows.Forms.Timer _init = new() { Interval = 150 };

    private MediaMonitor? _monitor;
    private TrackInfo? _current;
    private TrackInfo? _pending;
    private string _lastSig = "";
    private SettingsForm? _settingsForm;
    private bool _cleaned;

    public TrayApplicationContext()
    {
        _settings = AppSettings.Load();

        _toggleItem = new ToolStripMenuItem("Включить статус", null, (_, _) => ToggleEnabled())
        {
            Checked = _settings.Enabled
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("Настройки…", null, (_, _) => OpenSettings()));
        menu.Items.Add(_toggleItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Выход", null, (_, _) => Exit()));

        var (icon, owned) = LoadIcon();
        if (owned) _ownedIcon = icon;

        _tray = new NotifyIcon
        {
            Icon = icon,
            Text = "Яндекс Музыка RPC",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => OpenSettings();

        _debounce.Tick += OnDebounce;
        _init.Tick += OnInit;
        _init.Start();
    }

    // отложенная инициализация: к первому тику работает цикл сообщений и есть UI-контекст для SMTC
    private async void OnInit(object? sender, EventArgs e)
    {
        _init.Stop();
        _monitor = new MediaMonitor();
        _monitor.Changed += OnTrackChanged;

        StartupManager.Set(_settings.RunAtStartup);
        ConnectDiscord();

        try { await _monitor.StartAsync(); }
        catch { _tray.Text = "SMTC недоступен (нужна Windows 10 1809+)"; }

        if (string.IsNullOrWhiteSpace(_settings.DiscordAppId))
            OpenSettings();
    }

    private void OnTrackChanged(TrackInfo? info)
    {
        _current = info;

        if (!_settings.Enabled || info == null || (_settings.HideWhenPaused && !info.IsPlaying))
        {
            _debounce.Stop();
            _discord.Clear();
            _lastSig = "";
            _tray.Text = "Яндекс Музыка RPC";
            return;
        }

        _pending = info;
        _debounce.Stop();
        _debounce.Start();
    }

    private async void OnDebounce(object? sender, EventArgs e)
    {
        _debounce.Stop();
        var t = _pending;
        if (t == null || t.Signature == _lastSig) return;
        _lastSig = t.Signature;

        try
        {
            _tray.Text = Cap($"{t.Title} — {t.Artist}", 63);
            await _yandex.EnrichAsync(t);
            if (!ReferenceEquals(_current, t)) return; // трек сменился/пауза за время запроса — не шлём устаревшее
            if (!_settings.ShowButton) t.TrackUrl = null;
            _discord.Update(t);
        }
        catch
        {
            _lastSig = ""; // дать повторную попытку на следующем событии
        }
    }

    private void ConnectDiscord()
    {
        if (_settings.Enabled && !string.IsNullOrWhiteSpace(_settings.DiscordAppId))
            _discord.Connect(_settings.DiscordAppId);
        else
            _discord.Disconnect();
    }

    private void ToggleEnabled()
    {
        _settings.Enabled = !_settings.Enabled;
        _settings.Save();
        ApplySettings();
    }

    private void OpenSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.Activate();
            return;
        }
        _settingsForm = new SettingsForm(_settings, ApplySettings);
        _settingsForm.FormClosed += (_, _) => _settingsForm = null;
        _settingsForm.Show();
    }

    private void ApplySettings()
    {
        _toggleItem.Checked = _settings.Enabled;
        StartupManager.Set(_settings.RunAtStartup);
        ConnectDiscord();
        _lastSig = "";
        OnTrackChanged(_current);
    }

    private void Exit()
    {
        Cleanup();
        ExitThread();
    }

    private void Cleanup()
    {
        if (_cleaned) return;
        _cleaned = true;
        _debounce.Stop();
        _debounce.Dispose();
        _init.Dispose();
        _monitor?.Dispose();
        _discord.Dispose();
        if (_settingsForm is { IsDisposed: false }) _settingsForm.Close();
        _tray.Visible = false;
        _tray.Dispose();
        _ownedIcon?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Cleanup();
        base.Dispose(disposing);
    }

    private static string Cap(string s, int max)
    {
        if (s.Length <= max) return s;
        s = s[..max];
        if (char.IsHighSurrogate(s[^1])) s = s[..^1]; // не рвём суррогатную пару (эмодзи)
        return s;
    }

    private static (Icon icon, bool owned) LoadIcon()
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico");
        return s != null ? (new Icon(s), true) : (SystemIcons.Application, false);
    }
}
