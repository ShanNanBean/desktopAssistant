using System;
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
                $"发生未处理的异常:\n{ex.Exception.Message}\n\n堆栈跟踪:\n{ex.Exception.StackTrace}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            ex.Handled = true;
        };

        // 捕获所有未处理的异常
        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            var exception = ex.ExceptionObject as Exception;
            MessageBox.Show(
                $"发生严重错误:\n{exception?.Message}\n\n堆栈跟踪:\n{exception?.StackTrace}",
                "严重错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        };

        try
        {
            // 初始化配置
            _ = Services.ConfigManager.Instance;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"初始化配置失败:\n{ex.Message}\n\n堆栈跟踪:\n{ex.StackTrace}",
                "初始化错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
}
