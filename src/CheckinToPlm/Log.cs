using System.Diagnostics;

namespace CheckinToPlm;

/// <summary>Append-only log shared by every process that hosts the extension.</summary>
internal static class Log
{
    private static readonly object Gate = new();

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CheckinToPlm", "log.txt");

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : $"{message}: {ex}");

    private static void Write(string level, string message)
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] [{process.ProcessName}:{process.Id}] {message}{Environment.NewLine}";
            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                // Several processes (Vault client, Inventor) can write at once, so retry briefly on a sharing clash.
                for (var attempt = 0; ; attempt++)
                {
                    try
                    {
                        File.AppendAllText(FilePath, line);
                        return;
                    }
                    catch (IOException) when (attempt < 5)
                    {
                        Thread.Sleep(20);
                    }
                }
            }
        }
        catch
        {
            // Logging must never break a check-in.
        }
    }
}
