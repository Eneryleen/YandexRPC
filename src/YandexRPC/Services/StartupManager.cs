using Microsoft.Win32;

namespace YandexRPC.Services;

public static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "YandexRPC";

    private static string ExePath => Environment.ProcessPath ?? Application.ExecutablePath;

    public static void Set(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (key == null) return;
            if (enabled) key.SetValue(ValueName, $"\"{ExePath}\"");
            else if (key.GetValue(ValueName) != null) key.DeleteValue(ValueName, false);
        }
        catch { }
    }
}
