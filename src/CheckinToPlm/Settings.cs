using System.Text.Json;

namespace CheckinToPlm;

/// <summary>Per-user preferences, shared by every host process through one JSON file.</summary>
internal sealed class Settings
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public bool AssignItem { get; set; } = true;

    public bool SyncToFusionManage { get; set; } = true;

    public List<string> FileExtensions { get; set; } = [".ipt", ".iam", ".dwg", ".sldprt", ".sldasm"];

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CheckinToPlm", "settings.json");

    /// <summary>Reads the settings file; a missing or unreadable file yields the defaults.</summary>
    public static Settings Load(string? path = null)
    {
        path ??= DefaultPath;
        try
        {
            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? new Settings();
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Could not read settings from {path}; using defaults", ex);
        }

        return new Settings();
    }

    public void Save(string? path = null)
    {
        path ??= DefaultPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }

    public bool IsInScope(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        return extension.Length > 0 &&
               FileExtensions.Any(e => string.Equals(NormalizeExtension(e), extension, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Parses "ipt, .iam;dwg" style input into a clean, distinct list of ".ext" values.</summary>
    public static List<string> ParseExtensions(string text) =>
        text.Split([' ', ',', ';', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeExtension)
            .Where(e => e.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string NormalizeExtension(string value)
    {
        value = value.Trim().ToLowerInvariant();
        return value.StartsWith('.') ? value : "." + value;
    }
}
