using System.Windows;

namespace PowerShellHelper;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // 从配置加载窗口位置
        LoadWindowPosition();
        
        // 窗口关闭时保存位置
        Closing += (s, e) =>
        {
            SaveWindowPosition();
        };
    }

    private void LoadWindowPosition()
    {
        var config = Services.ConfigManager.Instance.Config;
        
        if (config.WindowWidth > 0 && config.WindowHeight > 0)
        {
            Width = config.WindowWidth;
            Height = config.WindowHeight;
            Left = config.WindowLeft;
            Top = config.WindowTop;
            Topmost = config.WindowTopmost;
        }
    }

    private void SaveWindowPosition()
    {
        var config = Services.ConfigManager.Instance.Config;
        
        config.WindowWidth = Width;
        config.WindowHeight = Height;
        config.WindowLeft = Left;
        config.WindowTop = Top;
        config.WindowTopmost = Topmost;
        
        Services.ConfigManager.Instance.SaveConfig();
    }
}
