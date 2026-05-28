using Velopack;

namespace YandexRPC;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // обязателен первым: обрабатывает хуки установки/обновления Velopack
        VelopackApp.Build().Run();

        using var mutex = new Mutex(false, @"Local\YandexRPC_SingleInstance");
        if (!mutex.WaitOne(TimeSpan.Zero)) return; // уже запущено

        // фоновое исключение не должно ронять трей — пишем в лог и продолжаем
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Log(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log(e.ExceptionObject as Exception);

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }

    private static void Log(Exception? ex)
    {
        if (ex == null) return;
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YandexRPC");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "error.log"), $"{DateTime.Now:O} {ex}\n");
        }
        catch { }
    }
}
