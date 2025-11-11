using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace PowerShellHelper.Services;

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
                UseShellExecute = true
            };

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 在文件资源管理器中打开文件所在目录
    /// </summary>
    public bool OpenInExplorer(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                Process.Start("explorer.exe", $"/select,\"{path}\"");
                return true;
            }
            else if (Directory.Exists(path))
            {
                Process.Start("explorer.exe", $"\"{path}\"");
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

        // 尝试使用 where 命令查找
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = "code",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                if (!string.IsNullOrWhiteSpace(output))
                {
                    var firstLine = output.Split('\n')[0].Trim();
                    if (File.Exists(firstLine))
                        return firstLine;
                }
            }
        }
        catch { }

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

/// <summary>
/// 编辑器信息
/// </summary>
public class EditorInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}
