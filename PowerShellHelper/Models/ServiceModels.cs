namespace PowerShellHelper.Models;

/// <summary>
/// AI响应结果
/// </summary>
public class AIResponse
{
    public bool Success { get; set; }
    public string? Content { get; set; }
    public string? ErrorMessage { get; set; }
    public PowerShellCommand? Command { get; set; }
}

/// <summary>
/// 命令执行结果
/// </summary>
public class CommandExecutionResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string ErrorOutput { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public TimeSpan ExecutionTime { get; set; }
}
