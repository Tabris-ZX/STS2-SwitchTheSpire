using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SwitchTheSpire;

/// <summary>
/// 互换目标类型。
/// </summary>
public enum SwapTarget
{
    //卡牌
    Card = 0,
    //遗物
    Relic = 1,
    //药水
    Potion = 2
}

/// <summary>
/// 模组配置，控制奖励互换的映射关系。支持 JSON 持久化，F7 热重载。
/// </summary>
internal sealed class Config
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    internal static Config Default => new();

    /// <summary>是否启用奖励互换。</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>卡牌获取转换</summary>
    [JsonPropertyName("card_becomes")]
    public SwapTarget CardBecomes { get; set; } = SwapTarget.Card;

    /// <summary>遗物获取转换</summary>
    [JsonPropertyName("relic_becomes")]
    public SwapTarget RelicBecomes { get; set; } = SwapTarget.Relic;

    /// <summary>药水获取转换</summary>
    [JsonPropertyName("potion_becomes")]
    public SwapTarget PotionBecomes { get; set; } = SwapTarget.Potion;

    /// <summary>配置文件路径(dll同目录下的 config.json)</summary>
    private static string FilePath =>
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "config.json");

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
