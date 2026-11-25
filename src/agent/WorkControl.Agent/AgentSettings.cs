using System.Text.Json;

namespace WorkControl.Agent;

public sealed record AgentSettings(string ApiUrl, string AgentId, Guid? DeviceId = null, Guid? KeyId = null)
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
        return new AgentSettings(configuredUrl ?? "http://localhost:5080", string.Empty);
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void EnsureApiUrl(string apiUrl)
    {
        if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && (uri.Scheme != Uri.UriSchemeHttp || !uri.IsLoopback)))
        {
            throw new InvalidOperationException(
                "Разрешён локальный HTTP (localhost/127.0.0.1) или доверенный HTTPS-адрес пилота. Проверка TLS не отключается.");
        }
    }
}
