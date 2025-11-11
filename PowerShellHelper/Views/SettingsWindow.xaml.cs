using System.Windows;
using System.Windows.Controls;
using PowerShellHelper.ViewModels;

namespace PowerShellHelper.Views;

/// <summary>
/// SettingsWindow.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsWindowViewModel _viewModel;

    public SettingsWindow()
    {
        InitializeComponent();
        
        _viewModel = (SettingsWindowViewModel)DataContext;
        _viewModel.CloseRequested += (s, e) => Close();

        // 加载API密钥到PasswordBox
        Loaded += (s, e) =>
        {
            ApiKeyBox.Password = _viewModel.ApiKey;
        };

        // 同步PasswordBox的值到ViewModel
        ApiKeyBox.PasswordChanged += (s, e) =>
        {
            _viewModel.ApiKey = ApiKeyBox.Password;
        };
    }
}
