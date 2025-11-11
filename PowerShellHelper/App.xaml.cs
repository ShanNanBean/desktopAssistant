using System.Windows;

namespace PowerShellHelper;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 全局异常处理
        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show(
                $"发生未处理的异常:\n{ex.Exception.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            ex.Handled = true;
        };

        // 初始化配置
        _ = Services.ConfigManager.Instance;

        // 清理过期历史记录
        var config = Services.ConfigManager.Instance.Config;
        if (config.AutoCleanup)
        {
            Services.HistoryService.Instance.CleanupOldRecords(config.HistoryRetentionDays);
        }
    }
}
