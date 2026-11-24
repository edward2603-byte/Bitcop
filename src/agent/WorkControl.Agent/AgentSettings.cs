using System.Text.Json;

namespace WorkControl.Agent;

public sealed record AgentSettings(string ApiUrl, string AgentId)
{
    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WorkControl",
        "agent-settings.json");

    public static AgentSettings Load()
    {
        var configuredUrl = Environment.GetEnvironmentVariable("WORKCONTROL_API_URL");
        if (File.Exists(SettingsPath))
        {
            var saved = JsonSerializer.Deserialize<AgentSettings>(File.ReadAllText(SettingsPath));
            if (saved is not null) return string.IsNullOrWhiteSpace(configuredUrl) ? saved : saved with { ApiUrl = configuredUrl };
        }
        return new AgentSettings(configuredUrl ?? "http://localhost:5080", $"agent-{Guid.NewGuid():N}");
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
