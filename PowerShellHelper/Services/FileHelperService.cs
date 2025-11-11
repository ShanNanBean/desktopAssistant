using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace PowerShellHelper.Services;

/// <summary>
/// 编辑器信息
/// </summary>
public class EditorInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}

/// <summary>
/// 文件操作辅助服务
/// </summary>
public class FileHelperService
{
    private readonly ConfigManager _configManager;

    public FileHelperService()
    {
        _configManager = ConfigManager.Instance;
    }

    /// <summary>
    /// 检测系统中安装的编辑器
    /// </summary>
    public List<EditorInfo> DetectInstalledEditors()
    {
        var editors = new List<EditorInfo>
        {
            // Notepad (系统内置)
            new EditorInfo
            {
                Name = "Notepad",
                Path = "notepad.exe",
                IsAvailable = true
            }
        };

        // 检测VS Code
        var vscodePath = FindVSCodePath();
        if (!string.IsNullOrEmpty(vscodePath))
        {
            editors.Add(new EditorInfo
            {
                Name = "Visual Studio Code",
                Path = vscodePath,
                IsAvailable = true
            });
        }

        // 检测Notepad++
        var notepadPPPath = FindNotepadPlusPlusPath();
        if (!string.IsNullOrEmpty(notepadPPPath))
        {
            editors.Add(new EditorInfo
            {
                Name = "Notepad++",
                Path = notepadPPPath,
                IsAvailable = true
            });
        }

        return editors;
    }

    /// <summary>
    /// 在编辑器中打开文件
    /// 采用异步后台启动方式，避免与PowerShell执行的资源竞争
    /// </summary>
    public bool OpenInEditor(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            var editorPath = _configManager.Config.DefaultEditor;

            // 如果配置的编辑器不可用,使用默认编辑器
            if (!IsEditorAvailable(editorPath))
            {
                editorPath = "notepad.exe";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = editorPath,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = true,
                CreateNoWindow = editorPath.EndsWith("Code.exe", StringComparison.OrdinalIgnoreCase) // VS Code 可隐藏控制台
            };

            // 关键：在后台线程中启动编辑器，避免占用UI线程和与PowerShell的资源竞争
            // 如果文件是临时脚本文件，编辑器会保持文件锁，直到进程关闭
            // 通过后台启动，确保PowerShell执行时编辑器已完全初始化
            _ = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    using var process = Process.Start(startInfo);
                    // 不等待进程完成，让编辑器在后台运行
                }
                catch
                {
                    // 忽略后台启动失败
                }
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 在文件资源管理器中打开文件所在目录
    /// 采用异步后台启动方式，避免资源竞争
    /// </summary>
    public bool OpenInExplorer(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                // 在后台线程启动explorer，避免阻塞主线程和与PowerShell竞争
                _ = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        using var process = Process.Start("explorer.exe", $"/select,\"{path}\"");
                    }
                    catch { }
                });
                return true;
            }
            else if (Directory.Exists(path))
            {
                _ = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        using var process = Process.Start("explorer.exe", $"\"{path}\"");
                    }
                    catch { }
                });
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查命令是否涉及文件修改
    /// </summary>
    public bool IsFileModificationCommand(string command)
    {
        var fileModificationVerbs = new[] { "Set-Content", "Add-Content", "Out-File", "New-Item" };
        
        foreach (var verb in fileModificationVerbs)
        {
            if (command.Contains(verb, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsEditorAvailable(string editorPath)
    {
        if (string.IsNullOrEmpty(editorPath))
            return false;

        // 如果是系统命令
        if (!Path.IsPathRooted(editorPath))
        {
            return true; // 假定系统命令可用
        }

        return File.Exists(editorPath);
    }

    private string? FindVSCodePath()
    {
        // 检查常见安装路径
        var possiblePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "Programs", "Microsoft VS Code", "Code.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                "Microsoft VS Code", "Code.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), 
                "Microsoft VS Code", "Code.exe")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return path;
        }

        // 尝试从注册表查找
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{771FD6B0-FA20-440A-A002-3B3BAC16DC50}_is1");
            if (key != null)
            {
                var installLocation = key.GetValue("InstallLocation") as string;
                if (!string.IsNullOrEmpty(installLocation))
                {
                    var exePath = Path.Combine(installLocation, "Code.exe");
                    if (File.Exists(exePath))
                        return exePath;
                }
            }
        }
        catch { }

        // 不再尝试使用 cmd /c where，直接返回 null
        // 这避免了任何外部进程执行导致的状态污染
        return null;
    }

    private string? FindNotepadPlusPlusPath()
    {
        var possiblePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                "Notepad++", "notepad++.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), 
                "Notepad++", "notepad++.exe")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }
}

