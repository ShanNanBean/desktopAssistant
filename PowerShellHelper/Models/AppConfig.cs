using System.Collections.Generic;

namespace PowerShellHelper.Models;

/// <summary>
/// AI模型配置
/// </summary>
public class AIModelConfig
{
    public AIProvider Provider { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string? ApiEndpoint { get; set; }
    public string? ModelName { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxTokens { get; set; } = 2000;
}

/// <summary>
/// 应用配置
/// </summary>
public class AppConfig
{
    public AIModelConfig AIConfig { get; set; } = new();
    public SecurityLevel SecurityLevel { get; set; } = SecurityLevel.Standard;
    public List<string> CustomBlacklist { get; set; } = new();
    public List<string> CustomWhitelist { get; set; } = new();
    public bool AllowUnknownCommands { get; set; } = true;
    
    // 界面配置
    public string Theme { get; set; } = "Light";
    public int FontSize { get; set; } = 14;
    public string DefaultEditor { get; set; } = "notepad.exe";
    public string GlobalHotkey { get; set; } = "Ctrl+Shift+P";
    
    // 窗口配置
    public double WindowLeft { get; set; } = 100;
    public double WindowTop { get; set; } = 100;
    public double WindowWidth { get; set; } = 800;
    public double WindowHeight { get; set; } = 600;
    public bool WindowTopmost { get; set; } = false;
}
