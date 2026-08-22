using System.Text.Json;

namespace DesktopPetOrHuman;

internal static class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DesktopPetOrHuman",
        "settings.json");

    public static SettingsSnapshot Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new SettingsSnapshot();
            }

            return JsonSerializer.Deserialize<SettingsSnapshot>(File.ReadAllText(FilePath), JsonOptions)
                ?? new SettingsSnapshot();
        }
        catch (Exception)
        {
            return new SettingsSnapshot();
        }
    }

    public static void SaveLastCharacterKey(string key)
    {
        var settings = Load();
        settings.LastCharacterKey = key;
        Save(settings);
    }

    public static void SaveLastSize(double windowWidth, double windowHeight, double petSize)
    {
        var settings = Load();
        settings.WindowWidth = windowWidth;
        settings.WindowHeight = windowHeight;
        settings.PetSize = petSize;
        Save(settings);
    }

    private static void Save(SettingsSnapshot settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch (Exception)
        {
            // Settings are best-effort; a failed write should not close the pet.
        }
    }

    internal sealed class SettingsSnapshot
    {
        public string? LastCharacterKey { get; set; }
        public double? WindowWidth { get; set; }
        public double? WindowHeight { get; set; }
        public double? PetSize { get; set; }
    }
}
