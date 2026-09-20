using System.Text.Json.Serialization;

namespace SCLogMate.Core.Plugins;

/// <summary>
/// Sidebar navigation entry descriptor defined in a plugin manifest.
/// </summary>
public class PluginSidebarConfig
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("group")]
    public string Group { get; set; } = "Erweiterungen";

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "Puzzle";

    [JsonPropertyName("order")]
    public int Order { get; set; } = 100;
}

/// <summary>
/// Data model representing plugin metadata declared in manifest.json.
/// </summary>
public class PluginManifest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("homepage")]
    public string? Homepage { get; set; }

    [JsonPropertyName("entry")]
    public string Entry { get; set; } = "index.html";

    [JsonPropertyName("backendDll")]
    public string? BackendDll { get; set; }

    [JsonPropertyName("sidebar")]
    public PluginSidebarConfig? Sidebar { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    // Runtime metadata populated by PluginManager
    [JsonIgnore]
    public string PluginDirectory { get; set; } = string.Empty;

    [JsonIgnore]
    public bool HasBackend => !string.IsNullOrWhiteSpace(BackendDll);

    [JsonIgnore]
    public bool HasWebUi => !string.IsNullOrWhiteSpace(Entry);
}
