using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using PowerShellHelper.Models;
using PowerShellHelper.Services;

namespace PowerShellHelper.ViewModels;

/// <summary>
/// 主窗口ViewModel
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly AIService _aiService;
    private readonly SafetyCheckerService _safetyChecker;
    private readonly PowerShellExecutor _executor;
    private readonly HistoryService _historyService;
    private readonly FileHelperService _fileHelper;

    private string _userInput = string.Empty;
    private bool _isBusy = false;
    private string _statusMessage = "就绪";

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public string UserInput
    {
        get => _userInput;
        set => SetProperty(ref _userInput, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand SendMessageCommand { get; }
    public ICommand ExecuteCommandCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenHistoryCommand { get; }
    public ICommand CopyCommandCommand { get; }
    public ICommand OpenFileCommand { get; }

    public MainWindowViewModel()
    {
        _aiService = new AIService();
        _safetyChecker = new SafetyCheckerService();
        _executor = new PowerShellExecutor();
        _historyService = HistoryService.Instance;
        _fileHelper = new FileHelperService();

        SendMessageCommand = new RelayCommand(async () => await SendMessageAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(UserInput));
        ExecuteCommandCommand = new RelayCommand<PowerShellCommand>(async cmd => await ExecuteCommandAsync(cmd));
        CopyCommandCommand = new RelayCommand<string>(CopyCommand);
        OpenFileCommand = new RelayCommand<string>(OpenFile);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenHistoryCommand = new RelayCommand(OpenHistory);

        // 添加欢迎消息
        Messages.Add(new ChatMessage
        {
            IsUser = false,
            Content = "你好!我是PowerShell助手。请告诉我你想执行什么操作,我会为你生成相应的PowerShell命令。",
            Timestamp = DateTime.Now
        });
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput))
            return;

        var userMessage = UserInput;
        UserInput = string.Empty;

        // 添加用户消息
        Messages.Add(new ChatMessage
        {
            IsUser = true,
            Content = userMessage,
            Timestamp = DateTime.Now
        });

        IsBusy = true;
        StatusMessage = "正在生成命令...";

        try
        {
            // 调用AI服务
            var response = await _aiService.GenerateCommandAsync(userMessage, Messages.ToList());

            if (!response.Success)
            {
                Messages.Add(new ChatMessage
                {
                    IsUser = false,
                    Content = $"抱歉,生成命令时出错: {response.ErrorMessage}",
                    Timestamp = DateTime.Now
                });
                StatusMessage = "生成失败";
                return;
            }

            if (response.Command == null || string.IsNullOrEmpty(response.Command.CommandText))
            {
                Messages.Add(new ChatMessage
                {
                    IsUser = false,
                    Content = response.Content ?? "未能生成有效的命令",
                    Timestamp = DateTime.Now
                });
                StatusMessage = "未生成命令";
                return;
            }

            // 安全检查
            var safetyResult = _safetyChecker.AnalyzeCommand(response.Command);
            response.Command.RiskLevel = safetyResult.RiskLevel;
            response.Command.RiskScore = safetyResult.RiskScore;

            // 添加AI回复
            var aiMessage = new ChatMessage
            {
                IsUser = false,
                Content = response.Content ?? string.Empty,
                Command = response.Command,
                Timestamp = DateTime.Now
            };

            Messages.Add(aiMessage);

            // 根据安全检查结果处理
            if (!safetyResult.IsAllowed)
            {
                Messages.Add(new ChatMessage
                {
                    IsUser = false,
                    Content = $"⚠️ 安全警告: {safetyResult.Reason}",
                    Timestamp = DateTime.Now
                });
                
                // 记录到历史(未执行)
                _historyService.AddRecord(new CommandHistoryRecord
                {
                    Timestamp = DateTime.Now,
                    UserInput = userMessage,
                    GeneratedCommand = response.Command.CommandText,
                    Status = ExecutionStatus.NotExecuted,
                    RiskLevel = safetyResult.RiskLevel
                });

                StatusMessage = "命令被拦截";
            }
            else if (safetyResult.RequiresConfirmation)
            {
                StatusMessage = "等待确认";
                // 这里应该弹出确认对话框,暂时简化处理
                var confirmResult = MessageBox.Show(
                    $"命令: {response.Command.CommandText}\n\n说明: {response.Command.Description}\n\n风险等级: {safetyResult.RiskLevel}\n\n确定要执行吗?",
                    "确认执行",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (confirmResult == MessageBoxResult.Yes)
                {
                    await ExecuteCommandAsync(response.Command);
                }
                else
                {
                    Messages.Add(new ChatMessage
                    {
                        IsUser = false,
                        Content = "已取消执行",
                        Timestamp = DateTime.Now
                    });

                    _historyService.AddRecord(new CommandHistoryRecord
                    {
                        Timestamp = DateTime.Now,
                        UserInput = userMessage,
                        GeneratedCommand = response.Command.CommandText,
                        Status = ExecutionStatus.Cancelled,
                        RiskLevel = safetyResult.RiskLevel
                    });

                    StatusMessage = "已取消";
                }
            }
            else
            {
                // 低风险命令可以直接执行或提示用户
                StatusMessage = "命令已生成";
            }
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage
            {
                IsUser = false,
                Content = $"发生错误: {ex.Message}",
                Timestamp = DateTime.Now
            });
            StatusMessage = "发生错误";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteCommandAsync(PowerShellCommand? command)
    {
        if (command == null || string.IsNullOrEmpty(command.CommandText))
            return;

        IsBusy = true;
        StatusMessage = "正在执行命令...";

        try
        {
            var result = await _executor.ExecuteCommandAsync(command.CommandText);

            var resultMessage = new ChatMessage
            {
                IsUser = false,
                Content = result.Success ? "✅ 执行成功" : "❌ 执行失败",
                ExecutionResult = result.Success ? result.Output : result.ErrorOutput,
                Status = result.Success ? ExecutionStatus.Success : ExecutionStatus.Failed,
                Timestamp = DateTime.Now
            };

            Messages.Add(resultMessage);

            // 记录到历史
            _historyService.AddRecord(new CommandHistoryRecord
            {
                Timestamp = DateTime.Now,
                UserInput = Messages.LastOrDefault(m => m.IsUser)?.Content ?? string.Empty,
                GeneratedCommand = command.CommandText,
                Status = result.Success ? ExecutionStatus.Success : ExecutionStatus.Failed,
                ExecutionResult = result.Success ? result.Output : result.ErrorOutput,
                RiskLevel = command.RiskLevel
            });

            StatusMessage = result.Success ? "执行成功" : "执行失败";
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage
            {
                IsUser = false,
                Content = $"执行异常: {ex.Message}",
                Status = ExecutionStatus.Failed,
                Timestamp = DateTime.Now
            });
            StatusMessage = "执行异常";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CopyCommand(string? commandText)
    {
        if (!string.IsNullOrEmpty(commandText))
        {
            Clipboard.SetText(commandText);
            StatusMessage = "已复制到剪贴板";
        }
    }

    private void OpenFile(string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath))
        {
            _fileHelper.OpenInEditor(filePath);
        }
    }

    private void OpenSettings()
    {
        var settingsWindow = new Views.SettingsWindow
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        settingsWindow.ShowDialog();
    }

    private void OpenHistory()
    {
        var historyWindow = new Views.HistoryWindow
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        historyWindow.ShowDialog();
    }
}
