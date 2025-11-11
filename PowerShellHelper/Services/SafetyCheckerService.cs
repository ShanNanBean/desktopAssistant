using System.Text.RegularExpressions;
using PowerShellHelper.Models;

namespace PowerShellHelper.Services;

/// <summary>
/// 命令安全检查服务
/// </summary>
public class SafetyCheckerService
{
    private readonly ConfigManager _configManager;

    // 危险命令黑名单
    private static readonly HashSet<string> DangerousCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "Format-Volume", "diskpart", "Clear-Disk", "Initialize-Disk",
        "Remove-Partition", "Clear-RecycleBin",
        "Stop-Computer", "Restart-Computer",
        "Set-ExecutionPolicy"
    };

    // 危险动词
    private static readonly HashSet<string> DangerousVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "Remove", "Delete", "Clear", "Format", "Stop", "Disable", "Uninstall"
    };

    // 中风险动词
    private static readonly HashSet<string> MediumRiskVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "Set", "New", "Move", "Rename", "Copy", "Start", "Enable", "Install"
    };

    // 低风险动词(只读操作)
    private static readonly HashSet<string> SafeVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "Get", "Show", "Test", "Measure", "Find", "Search", "Read", "Select"
    };

    // 系统关键目录
    private static readonly HashSet<string> SystemPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "C:\\Windows", "C:\\Program Files", "C:\\Program Files (x86)",
        "$env:SystemRoot", "$env:ProgramFiles", "HKLM:", "HKCU:\\Software\\Microsoft\\Windows"
    };

    // 危险参数
    private static readonly HashSet<string> DangerousParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "-Force", "-Recurse", "-Confirm:$false", "-WhatIf:$false"
    };

    public SafetyCheckerService()
    {
        _configManager = ConfigManager.Instance;
    }

    /// <summary>
    /// 分析命令安全性
    /// </summary>
    public SafetyAnalysisResult AnalyzeCommand(PowerShellCommand command)
    {
        var result = new SafetyAnalysisResult
        {
            IsAllowed = true,
            RequiresConfirmation = false
        };

        if (string.IsNullOrWhiteSpace(command.CommandText))
        {
            result.IsAllowed = false;
            result.Reason = "命令为空";
            return result;
        }

        // 1. 检查黑名单
        if (IsInBlacklist(command.CommandText))
        {
            result.IsAllowed = false;
            result.RiskLevel = RiskLevel.High;
            result.Reason = "命令在黑名单中,禁止执行";
            return result;
        }

        // 2. 计算风险评分
        var riskScore = CalculateRiskScore(command.CommandText);
        result.RiskScore = riskScore;

        // 3. 确定命令类型
        result.CommandType = DetermineCommandType(command.CommandText);

        // 4. 根据安全策略级别判定
        var securityLevel = _configManager.Config.SecurityLevel;
        result.RiskLevel = DetermineRiskLevel(riskScore);

        // 5. 应用安全策略
        switch (securityLevel)
        {
            case SecurityLevel.Strict:
                ApplyStrictPolicy(result, command.CommandText);
                break;
            case SecurityLevel.Standard:
                ApplyStandardPolicy(result);
                break;
            case SecurityLevel.Relaxed:
                ApplyRelaxedPolicy(result);
                break;
        }

        // 6. 添加警告信息
        AddWarnings(result, command.CommandText);

        return result;
    }

    /// <summary>
    /// 检查是否在黑名单中
    /// </summary>
    private bool IsInBlacklist(string commandText)
    {
        // 检查内置黑名单
        foreach (var dangerous in DangerousCommands)
        {
            if (commandText.Contains(dangerous, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // 检查自定义黑名单
        foreach (var custom in _configManager.Config.CustomBlacklist)
        {
            if (commandText.Contains(custom, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 计算风险评分 (0-100)
    /// </summary>
    private double CalculateRiskScore(string commandText)
    {
        double score = 0;

        // 1. 命令动词风险 (40%)
        score += CalculateVerbRisk(commandText) * 0.4;

        // 2. 路径风险 (30%)
        score += CalculatePathRisk(commandText) * 0.3;

        // 3. 参数风险 (20%)
        score += CalculateParameterRisk(commandText) * 0.2;

        // 4. 影响范围 (10%)
        score += CalculateScopeRisk(commandText) * 0.1;

        return Math.Min(score, 100);
    }

    private double CalculateVerbRisk(string commandText)
    {
        var cmdlet = ExtractCmdlet(commandText);
        
        if (DangerousVerbs.Any(v => cmdlet.StartsWith(v + "-", StringComparison.OrdinalIgnoreCase)))
            return 100;
        
        if (MediumRiskVerbs.Any(v => cmdlet.StartsWith(v + "-", StringComparison.OrdinalIgnoreCase)))
            return 50;
        
        if (SafeVerbs.Any(v => cmdlet.StartsWith(v + "-", StringComparison.OrdinalIgnoreCase)))
            return 10;

        return 40; // 未知动词默认中等风险
    }

    private double CalculatePathRisk(string commandText)
    {
        foreach (var systemPath in SystemPaths)
        {
            if (commandText.Contains(systemPath, StringComparison.OrdinalIgnoreCase))
                return 100;
        }

        // 检查是否涉及用户目录
        if (commandText.Contains("$env:USERPROFILE", StringComparison.OrdinalIgnoreCase) ||
            commandText.Contains("~\\", StringComparison.OrdinalIgnoreCase))
            return 50;

        // 检查是否使用绝对路径
        if (Regex.IsMatch(commandText, @"[A-Z]:\\"))
            return 30;

        return 10; // 相对路径或无路径
    }

    private double CalculateParameterRisk(string commandText)
    {
        double risk = 0;
        
        foreach (var param in DangerousParameters)
        {
            if (commandText.Contains(param, StringComparison.OrdinalIgnoreCase))
                risk += 30;
        }

        return Math.Min(risk, 100);
    }

    private double CalculateScopeRisk(string commandText)
    {
        // 检查通配符
        if (commandText.Contains("*"))
            return 80;

        // 检查递归操作
        if (commandText.Contains("-Recurse", StringComparison.OrdinalIgnoreCase))
            return 90;

        // 检查管道操作(可能批量处理)
        if (commandText.Contains("|"))
            return 40;

        return 10;
    }

    private RiskLevel DetermineRiskLevel(double score)
    {
        if (score >= 70) return RiskLevel.High;
        if (score >= 40) return RiskLevel.Medium;
        return RiskLevel.Low;
    }

    private CommandType DetermineCommandType(string commandText)
    {
        var cmdlet = ExtractCmdlet(commandText);

        if (SafeVerbs.Any(v => cmdlet.StartsWith(v + "-", StringComparison.OrdinalIgnoreCase)))
            return CommandType.Query;

        if (cmdlet.Contains("File", StringComparison.OrdinalIgnoreCase) ||
            cmdlet.Contains("Item", StringComparison.OrdinalIgnoreCase) ||
            cmdlet.Contains("Content", StringComparison.OrdinalIgnoreCase))
            return CommandType.FileOperation;

        if (cmdlet.Contains("Service", StringComparison.OrdinalIgnoreCase) ||
            cmdlet.Contains("Process", StringComparison.OrdinalIgnoreCase) ||
            cmdlet.Contains("Registry", StringComparison.OrdinalIgnoreCase))
            return CommandType.SystemConfig;

        if (cmdlet.Contains("Net", StringComparison.OrdinalIgnoreCase) ||
            cmdlet.Contains("Web", StringComparison.OrdinalIgnoreCase))
            return CommandType.NetworkOperation;

        return CommandType.FileOperation;
    }

    private void ApplyStrictPolicy(SafetyAnalysisResult result, string commandText)
    {
        // 严格模式:仅允许只读命令
        if (result.CommandType != CommandType.Query)
        {
            result.IsAllowed = false;
            result.Reason = "严格模式下仅允许只读查询命令";
            return;
        }

        if (result.RiskLevel != RiskLevel.Low)
        {
            result.IsAllowed = false;
            result.Reason = "严格模式下禁止执行此命令";
        }
    }

    private void ApplyStandardPolicy(SafetyAnalysisResult result)
    {
        // 标准模式:禁止高风险,中风险需确认
        if (result.RiskLevel == RiskLevel.High)
        {
            result.IsAllowed = false;
            result.Reason = "命令风险过高,已被拦截";
            return;
        }

        if (result.RiskLevel == RiskLevel.Medium)
        {
            result.RequiresConfirmation = true;
        }
    }

    private void ApplyRelaxedPolicy(SafetyAnalysisResult result)
    {
        // 宽松模式:仅拦截极端危险命令
        if (result.RiskLevel == RiskLevel.High && result.RiskScore >= 80)
        {
            result.IsAllowed = false;
            result.Reason = "命令极度危险,已被拦截";
            return;
        }

        if (result.RiskLevel >= RiskLevel.Medium)
        {
            result.RequiresConfirmation = true;
        }
    }

    private void AddWarnings(SafetyAnalysisResult result, string commandText)
    {
        if (commandText.Contains("-Force", StringComparison.OrdinalIgnoreCase))
            result.Warnings.Add("使用-Force参数将跳过确认");

        if (commandText.Contains("-Recurse", StringComparison.OrdinalIgnoreCase))
            result.Warnings.Add("递归操作将影响子目录");

        if (commandText.Contains("*"))
            result.Warnings.Add("使用通配符可能影响多个项目");

        foreach (var systemPath in SystemPaths)
        {
            if (commandText.Contains(systemPath, StringComparison.OrdinalIgnoreCase))
            {
                result.Warnings.Add($"操作涉及系统目录: {systemPath}");
                break;
            }
        }
    }

    private string ExtractCmdlet(string commandText)
    {
        var match = Regex.Match(commandText.Trim(), @"^([\w-]+)");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
