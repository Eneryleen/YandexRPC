using System.Diagnostics;

namespace YandexRPC.Settings;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly Action _onSaved;

    private readonly TextBox _appId = new();
    private readonly CheckBox _enabled = new() { Text = "Включить статус в Discord", AutoSize = true };
    private readonly CheckBox _startup = new() { Text = "Запускать при старте Windows", AutoSize = true };
    private readonly CheckBox _button = new() { Text = "Кнопка «Открыть» (ссылка на трек)", AutoSize = true };
    private readonly CheckBox _download = new() { Text = "Кнопка «Скачать RPC» (ссылка на репозиторий)", AutoSize = true };
    private readonly CheckBox _hidePaused = new() { Text = "Скрывать статус на паузе", AutoSize = true };
    private readonly TextBox _customButtons = new();

    public SettingsForm(AppSettings settings, Action onSaved)
    {
        _settings = settings;
        _onSaved = onSaved;

        Text = "Настройки — Яндекс Музыка RPC";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(440, 404);
        ShowInTaskbar = false;

        var idLabel = new Label { Text = "Discord Application ID:", AutoSize = true, Location = new Point(16, 16) };
        _appId.Location = new Point(16, 38);
        _appId.Width = 408;

        var help = new LinkLabel
        {
            Text = "Создать своё приложение Discord (необязательно)",
            AutoSize = true,
            Location = new Point(16, 66)
        };
        help.LinkClicked += (_, _) => Open("https://discord.com/developers/applications");

        _enabled.Location = new Point(16, 98);
        _startup.Location = new Point(16, 124);
        _button.Location = new Point(16, 150);
        _download.Location = new Point(16, 176);
        _hidePaused.Location = new Point(16, 202);

        var customLabel = new Label
        {
            Text = "Свои кнопки — по одной на строку:  Название | https://ссылка",
            AutoSize = true,
            Location = new Point(16, 236)
        };
        _customButtons.Location = new Point(16, 258);
        _customButtons.Size = new Size(408, 94);
        _customButtons.Multiline = true;
        _customButtons.ScrollBars = ScrollBars.Vertical;
        _customButtons.AcceptsReturn = true;
        _customButtons.WordWrap = false;

        var save = new Button { Text = "Сохранить", Location = new Point(248, 364), Size = new Size(84, 30) };
        var cancel = new Button { Text = "Отмена", Location = new Point(340, 364), Size = new Size(84, 30) };
        save.Click += OnSave;
        cancel.Click += (_, _) => Close();
        AcceptButton = save;
        CancelButton = cancel;

        Controls.AddRange(new Control[]
        {
            idLabel, _appId, help, _enabled, _startup, _button, _download, _hidePaused,
            customLabel, _customButtons, save, cancel
        });

        LoadValues();
    }

    private void LoadValues()
    {
        _appId.Text = _settings.DiscordAppId;
        _enabled.Checked = _settings.Enabled;
        _startup.Checked = _settings.RunAtStartup;
        _button.Checked = _settings.ShowButton;
        _download.Checked = _settings.ShowDownloadButton;
        _hidePaused.Checked = _settings.HideWhenPaused;
        _customButtons.Lines = _settings.CustomButtons
            .Select(b => $"{b.Label} | {b.Url}")
            .ToArray();
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _settings.DiscordAppId = _appId.Text.Trim();
        _settings.Enabled = _enabled.Checked;
        _settings.RunAtStartup = _startup.Checked;
        _settings.ShowButton = _button.Checked;
        _settings.ShowDownloadButton = _download.Checked;
        _settings.HideWhenPaused = _hidePaused.Checked;
        _settings.CustomButtons = ParseButtons(_customButtons.Lines);
        _settings.Save();
        _onSaved();
        Close();
    }

    private static List<CustomButton> ParseButtons(string[] lines)
    {
        var result = new List<CustomButton>();
        foreach (var line in lines)
        {
            var s = line.Trim();
            if (s.Length == 0) continue;
            var parts = s.Split('|', 2);
            if (parts.Length != 2) continue;
            var label = parts[0].Trim();
            var url = parts[1].Trim();
            if (label.Length > 0 && url.Length > 0)
                result.Add(new CustomButton { Label = label, Url = url });
            if (result.Count >= 16) break; // Discord покажет максимум 2, остальное храним с запасом
        }
        return result;
    }

    private static void Open(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u) ||
            (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps))
            return;
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }
}
