using System.IO;
using System.Text.Json;
using PowerShellHelper.Models;

namespace PowerShellHelper.Services;

/// <summary>
/// 配置管理器
/// </summary>
public class ConfigManager
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PowerShellHelper"
    );
    
    private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");
    
    private AppConfig? _config;
    private static ConfigManager? _instance;
    private static readonly object _lock = new object();

    public static ConfigManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ConfigManager();
                }
            }
            return _instance;
        }
    }

    private ConfigManager()
    {
        EnsureConfigDirectory();
        LoadConfig();
    }

    /// <summary>
    /// 获取配置
    /// </summary>
    public AppConfig Config => _config ??= new AppConfig();

    /// <summary>
    /// 保存配置
    /// </summary>
    public void SaveConfig()
    {
        try
        {
            EnsureConfigDirectory();
            
            // 创建配置副本用于序列化
            var configToSave = new AppConfig
            {
                AIConfig = new AIModelConfig
                {
                    Provider = Config.AIConfig.Provider,
                    ApiKey = EncryptApiKey(Config.AIConfig.ApiKey),
                    ApiEndpoint = Config.AIConfig.ApiEndpoint,
                    ModelName = Config.AIConfig.ModelName,
                    TimeoutSeconds = Config.AIConfig.TimeoutSeconds,
                    MaxTokens = Config.AIConfig.MaxTokens
                },
                SecurityLevel = Config.SecurityLevel,
                CustomBlacklist = Config.CustomBlacklist,
                CustomWhitelist = Config.CustomWhitelist,
                AllowUnknownCommands = Config.AllowUnknownCommands,
                Theme = Config.Theme,
                FontSize = Config.FontSize,
                DefaultEditor = Config.DefaultEditor,
                GlobalHotkey = Config.GlobalHotkey,
                HistoryRetentionDays = Config.HistoryRetentionDays,
                RecordSensitiveCommands = Config.RecordSensitiveCommands,
                AutoCleanup = Config.AutoCleanup,
                WindowLeft = Config.WindowLeft,
                WindowTop = Config.WindowTop,
                WindowWidth = Config.WindowWidth,
                WindowHeight = Config.WindowHeight,
                WindowTopmost = Config.WindowTopmost
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(configToSave, options);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private void LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                var loadedConfig = JsonSerializer.Deserialize<AppConfig>(json);
                
                if (loadedConfig != null)
                {
                    _config = loadedConfig;
                    // 解密API密钥
                    _config.AIConfig.ApiKey = DecryptApiKey(_config.AIConfig.ApiKey);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载配置失败: {ex.Message}");
        }

        // 加载失败或文件不存在,使用默认配置
        _config = new AppConfig();
    }

    /// <summary>
    /// 重置为默认配置
    /// </summary>
    public void ResetToDefault()
    {
        _config = new AppConfig();
        SaveConfig();
    }

    /// <summary>
    /// 确保配置目录存在
    /// </summary>
    private void EnsureConfigDirectory()
    {
        if (!Directory.Exists(ConfigDirectory))
        {
            Directory.CreateDirectory(ConfigDirectory);
        }
    }

    /// <summary>
    /// 加密API密钥
    /// </summary>
    private string EncryptApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return string.Empty;
        
        return EncryptionHelper.Encrypt(apiKey);
    }

    /// <summary>
    /// 解密API密钥
    /// </summary>
    private string DecryptApiKey(string encryptedKey)
    {
        if (string.IsNullOrEmpty(encryptedKey))
            return string.Empty;
        
        return EncryptionHelper.Decrypt(encryptedKey);
    }

    /// <summary>
    /// 导出配置(不含敏感信息)
    /// </summary>
    public string ExportConfig()
    {
        var exportConfig = new AppConfig
        {
            AIConfig = new AIModelConfig
            {
                Provider = Config.AIConfig.Provider,
                ApiEndpoint = Config.AIConfig.ApiEndpoint,
                ModelName = Config.AIConfig.ModelName,
                TimeoutSeconds = Config.AIConfig.TimeoutSeconds,
                MaxTokens = Config.AIConfig.MaxTokens
            },
            SecurityLevel = Config.SecurityLevel,
            CustomBlacklist = Config.CustomBlacklist,
            CustomWhitelist = Config.CustomWhitelist,
            Theme = Config.Theme,
            FontSize = Config.FontSize,
            DefaultEditor = Config.DefaultEditor
        };

        return JsonSerializer.Serialize(exportConfig, new JsonSerializerOptions { WriteIndented = true });
    }
}
