using System.Text.Json;

namespace YandexRPC.Settings;

public sealed class AppSettings
{
    public bool Enabled { get; set; } = true;
    public string DiscordAppId { get; set; } = "";
    public bool RunAtStartup { get; set; }
    public bool ShowButton { get; set; } = true;
    public bool HideWhenPaused { get; set; }

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YandexRPC", "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOpts));
        }
        catch { }
    }
}
