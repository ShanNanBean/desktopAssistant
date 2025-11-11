using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using PowerShellHelper.Models;

namespace PowerShellHelper.Services;

/// <summary>
/// PowerShell命令执行服务
/// </summary>
public class PowerShellExecutor
{
    private readonly int _defaultTimeoutSeconds = 60;

    /// <summary>
    /// 执行PowerShell命令
    /// </summary>
    public async Task<CommandExecutionResult> ExecuteCommandAsync(string command, int? timeoutSeconds = null)
    {
        return await Task.Run(() => ExecuteCommand(command, timeoutSeconds ?? _defaultTimeoutSeconds));
    }

    private CommandExecutionResult ExecuteCommand(string command, int timeoutSeconds)
    {
        var result = new CommandExecutionResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 创建独立的PowerShell运行空间
            using var runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();

            using var powershell = PowerShell.Create();
            powershell.Runspace = runspace;

            // 添加命令
            powershell.AddScript(command);

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            // 设置输出处理
            powershell.Streams.Information.DataAdded += (sender, e) =>
            {
                var info = powershell.Streams.Information[e.Index];
                outputBuilder.AppendLine(info.ToString());
            };

            powershell.Streams.Warning.DataAdded += (sender, e) =>
            {
                var warning = powershell.Streams.Warning[e.Index];
                outputBuilder.AppendLine($"WARNING: {warning}");
            };

            powershell.Streams.Error.DataAdded += (sender, e) =>
            {
                var error = powershell.Streams.Error[e.Index];
                errorBuilder.AppendLine(error.ToString());
            };

            // 执行命令(带超时)
            var asyncResult = powershell.BeginInvoke();
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            
            if (!asyncResult.AsyncWaitHandle.WaitOne(timeout))
            {
                powershell.Stop();
                result.Success = false;
                result.ErrorOutput = $"命令执行超时({timeoutSeconds}秒)";
                return result;
            }

            var output = powershell.EndInvoke(asyncResult);

            // 收集标准输出
            if (output != null)
            {
                foreach (var item in output)
                {
                    if (item != null)
                    {
                        outputBuilder.AppendLine(item.ToString());
                    }
                }
            }

            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;

            // 设置输出和错误
            result.Output = outputBuilder.ToString();
            result.ErrorOutput = errorBuilder.ToString();

            // 判断执行是否成功
            result.Success = !powershell.HadErrors && string.IsNullOrEmpty(errorBuilder.ToString());
            result.ExitCode = result.Success ? 0 : 1;

            // 如果有错误但输出为空,设置错误信息
            if (!result.Success && string.IsNullOrEmpty(result.ErrorOutput))
            {
                result.ErrorOutput = "命令执行失败,但未返回错误信息";
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.ErrorOutput = $"执行异常: {ex.Message}";
            result.ExecutionTime = stopwatch.Elapsed;
            result.ExitCode = -1;
        }

        return result;
    }

    /// <summary>
    /// 测试命令语法是否正确
    /// </summary>
    public bool ValidateCommandSyntax(string command)
    {
        try
        {
            using var runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            using var powershell = PowerShell.Create();
            powershell.Runspace = runspace;

            // 使用Get-Command尝试解析命令
            powershell.AddScript($"$null = {{ {command} }}");
            powershell.Invoke();

            return !powershell.HadErrors;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 提取命令中涉及的文件路径
    /// </summary>
    public List<string> ExtractFilePaths(string command)
    {
        var paths = new List<string>();
        
        // 简单的路径提取(可以改进)
        var pathPatterns = new[]
        {
            @"[A-Z]:\\(?:[^\\/:*?""<>|\r\n]+\\)*[^\\/:*?""<>|\r\n]*",  // 绝对路径
            @"\.\\.+",  // 相对路径
            @"~\\.+"    // 用户目录
        };

        foreach (var pattern in pathPatterns)
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(command, pattern);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (!string.IsNullOrWhiteSpace(match.Value))
                {
                    paths.Add(match.Value);
                }
            }
        }

        return paths.Distinct().ToList();
    }
}
