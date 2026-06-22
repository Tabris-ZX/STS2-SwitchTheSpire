using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace swapRelicsAndCards;

/// <summary>
/// 模组配置，控制奖励互换的开关状态。支持 JSON 持久化，F7 热重载。
/// </summary>
internal sealed class Config
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    internal static Config Default => new();

    /// <summary>是否启用奖励互换。</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>配置文件路径（DLL 同目录下的 config.cfg）。</summary>
    private static string FilePath =>
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "config.cfg");

    internal static Config Load()
    {
        if (!File.Exists(FilePath))
        {
            Default.Save();
            return Default;
        }
        try
        {
            return JsonSerializer.Deserialize<Config>(File.ReadAllText(FilePath), ReadOptions) ?? Default;
        }
        catch (Exception)
        {
            return Default;
        }
    }

    internal void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, WriteOptions));
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save config: {ex.Message}");
        }
    }
}
