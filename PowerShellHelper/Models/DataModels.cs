using System;
using System.Collections.Generic;

namespace PowerShellHelper.Models;

/// <summary>
/// 对话消息
/// </summary>
public class ChatMessage
{
    public bool IsUser { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public PowerShellCommand? Command { get; set; }
    public string? ExecutionResult { get; set; }
    public ExecutionStatus? Status { get; set; }
}

/// <summary>
/// PowerShell命令信息
/// </summary>
public class PowerShellCommand
{
    public string CommandText { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public CommandType CommandType { get; set; }
    public double RiskScore { get; set; }
    public List<string> AffectedPaths { get; set; } = new();
    public int AffectedFileCount { get; set; }
    public string? NetworkTarget { get; set; }
}

/// <summary>
/// 命令安全分析结果
/// </summary>
public class SafetyAnalysisResult
{
    public bool IsAllowed { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public double RiskScore { get; set; }
    public CommandType CommandType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
    public bool RequiresConfirmation { get; set; }
}

/// <summary>
/// 历史记录实体
/// </summary>
public class CommandHistoryRecord
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string UserInput { get; set; } = string.Empty;
    public string GeneratedCommand { get; set; } = string.Empty;
    public ExecutionStatus Status { get; set; }
    public string? ExecutionResult { get; set; }
    public RiskLevel RiskLevel { get; set; }
}
