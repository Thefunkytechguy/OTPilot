using System.IO;
using System.Text.Json;
using OTPilot.Models;

namespace OTPilot.Services;

public static class AppConfigService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OTPilot", "config.json");

    public static async Task<AppConfig> LoadAsync()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return new AppConfig();

            var json = await File.ReadAllTextAsync(ConfigPath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public static async Task SaveAsync(AppConfig config)
    {
        var dir = Path.GetDirectoryName(ConfigPath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(ConfigPath, json);
    }
}
