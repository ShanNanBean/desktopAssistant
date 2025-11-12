using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
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
        var tempScriptFile = string.Empty;

        try
        {
            // 直接调用系统 PowerShell 5.1 可执行文件
            var powershellPath = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";
            
            if (!System.IO.File.Exists(powershellPath))
            {
                result.Success = false;
                result.ErrorOutput = $"PowerShell 可执行文件未找到: {powershellPath}";
                return result;
            }

            // 创建临时脚本文件
            tempScriptFile = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), 
                $"psh_{Guid.NewGuid()}.ps1"
            );

            // 将命令写入临时脚本文件
            // 这是最可靠的方法，完全避免了参数转义问题
            var scriptLines = new[]
            {
                "# 强制设置输出编码为UTF8，确保中文正常显示",
                "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8",
                "$OutputEncoding = [System.Text.Encoding]::UTF8",
                "$ErrorActionPreference = 'Continue'",
                "try {",
                $"  {command}",
                "} catch {",
                "  $_.Exception.Message",
                "}"
            };
            
            System.IO.File.WriteAllLines(tempScriptFile, scriptLines, Encoding.UTF8);

            // 执行PowerShell脚本
            var processInfo = new ProcessStartInfo
            {
                FileName = powershellPath,
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempScriptFile}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = Process.Start(processInfo);
            
            if (process == null)
            {
                result.Success = false;
                result.ErrorOutput = "无法启动 PowerShell 进程";
                return result;
            }

            // 关键修复：使用异步读取避免死锁
            // 当 RedirectStandardOutput 和 RedirectStandardError 同时为 true 时，
            // 必须异步读取，否则缓冲区满了会导致进程挂起
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            // 等待进程完成（带超时）
            bool exited = process.WaitForExit(timeoutSeconds * 1000);
            
            // 等待读取任务完成（避免丢失数据）
            var stdOutput = outputTask.Result;
            var stdError = errorTask.Result;
            
            if (!exited)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch { }
                
                result.Success = false;
                result.ErrorOutput = $"命令执行超时({timeoutSeconds}秒)";
                return result;
            }

            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
            result.ExitCode = process.ExitCode;

            // 设置输出和错误（数据已在上面异步读取完成）
            result.Output = stdOutput?.TrimEnd() ?? string.Empty;
            result.ErrorOutput = stdError?.TrimEnd() ?? string.Empty;
            result.Success = process.ExitCode == 0 && string.IsNullOrEmpty(result.ErrorOutput);

            // 如果执行失败但没有错误信息，设置默认错误信息
            if (!result.Success && string.IsNullOrEmpty(result.ErrorOutput) && process.ExitCode != 0)
            {
                result.ErrorOutput = $"命令执行失败，退出码: {process.ExitCode}";
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
        finally
        {
            // 清理临时脚本文件
            try
            {
                if (!string.IsNullOrEmpty(tempScriptFile) && System.IO.File.Exists(tempScriptFile))
                {
                    System.IO.File.Delete(tempScriptFile);
                }
            }
            catch { }
        }

        return result;
    }
}
