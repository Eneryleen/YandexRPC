using System.Diagnostics;

namespace YandexRPC.Settings;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly Action _onSaved;

    private readonly TextBox _appId = new();
    private readonly CheckBox _enabled = new() { Text = "Включить статус в Discord", AutoSize = true };
    private readonly CheckBox _startup = new() { Text = "Запускать при старте Windows", AutoSize = true };
    private readonly CheckBox _button = new() { Text = "Показывать кнопку «Открыть»", AutoSize = true };
    private readonly CheckBox _hidePaused = new() { Text = "Скрывать статус на паузе", AutoSize = true };

    public SettingsForm(AppSettings settings, Action onSaved)
    {
        _settings = settings;
        _onSaved = onSaved;

        Text = "Настройки — Яндекс Музыка RPC";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(440, 280);
        ShowInTaskbar = false;

        var idLabel = new Label { Text = "Discord Application ID:", AutoSize = true, Location = new Point(16, 18) };
        _appId.Location = new Point(16, 40);
        _appId.Width = 408;

        var help = new LinkLabel { Text = "Как получить App ID?", AutoSize = true, Location = new Point(16, 68) };
        help.LinkClicked += (_, _) => Open("https://discord.com/developers/applications");

        _enabled.Location = new Point(16, 102);
        _startup.Location = new Point(16, 130);
        _button.Location = new Point(16, 158);
        _hidePaused.Location = new Point(16, 186);

        var save = new Button { Text = "Сохранить", Location = new Point(248, 232), Size = new Size(84, 30) };
        var cancel = new Button { Text = "Отмена", Location = new Point(340, 232), Size = new Size(84, 30) };
        save.Click += OnSave;
        cancel.Click += (_, _) => Close();
        AcceptButton = save;
        CancelButton = cancel;

        Controls.AddRange(new Control[]
        {
            idLabel, _appId, help, _enabled, _startup, _button, _hidePaused, save, cancel
        });

        LoadValues();
    }

    private void LoadValues()
    {
        _appId.Text = _settings.DiscordAppId;
        _enabled.Checked = _settings.Enabled;
        _startup.Checked = _settings.RunAtStartup;
        _button.Checked = _settings.ShowButton;
        _hidePaused.Checked = _settings.HideWhenPaused;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _settings.DiscordAppId = _appId.Text.Trim();
        _settings.Enabled = _enabled.Checked;
        _settings.RunAtStartup = _startup.Checked;
        _settings.ShowButton = _button.Checked;
        _settings.HideWhenPaused = _hidePaused.Checked;
        _settings.Save();
        _onSaved();
        Close();
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
