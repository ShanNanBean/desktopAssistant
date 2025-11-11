using System.Collections.ObjectModel;
using System.Windows.Input;
using PowerShellHelper.Models;
using PowerShellHelper.Services;

namespace PowerShellHelper.ViewModels;

/// <summary>
/// 设置窗口ViewModel
/// </summary>
public class SettingsWindowViewModel : ViewModelBase
{
    private readonly ConfigManager _configManager;
    private readonly FileHelperService _fileHelper;

    private AIProvider _selectedProvider;
    private string _apiKey = string.Empty;
    private string _apiEndpoint = string.Empty;
    private string _modelName = string.Empty;
    private SecurityLevel _securityLevel;
    private string _defaultEditor = string.Empty;
    private int _historyRetentionDays;

    public ObservableCollection<AIProvider> AIProviders { get; }
    public ObservableCollection<SecurityLevel> SecurityLevels { get; }
    public ObservableCollection<EditorInfo> AvailableEditors { get; }

    public AIProvider SelectedProvider
    {
        get => _selectedProvider;
        set
        {
            if (SetProperty(ref _selectedProvider, value))
            {
                UpdateDefaultEndpoint();
            }
        }
    }

    public string ApiKey
    {
        get => _apiKey;
        set => SetProperty(ref _apiKey, value);
    }

    public string ApiEndpoint
    {
        get => _apiEndpoint;
        set => SetProperty(ref _apiEndpoint, value);
    }

    public string ModelName
    {
        get => _modelName;
        set => SetProperty(ref _modelName, value);
    }

    public SecurityLevel SecurityLevel
    {
        get => _securityLevel;
        set => SetProperty(ref _securityLevel, value);
    }

    public string DefaultEditor
    {
        get => _defaultEditor;
        set => SetProperty(ref _defaultEditor, value);
    }

    public int HistoryRetentionDays
    {
        get => _historyRetentionDays;
        set => SetProperty(ref _historyRetentionDays, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetCommand { get; }

    public event EventHandler? CloseRequested;

    public SettingsWindowViewModel()
    {
        _configManager = ConfigManager.Instance;
        _fileHelper = new FileHelperService();

        AIProviders = new ObservableCollection<AIProvider>(Enum.GetValues<AIProvider>());
        SecurityLevels = new ObservableCollection<SecurityLevel>(Enum.GetValues<SecurityLevel>());
        AvailableEditors = new ObservableCollection<EditorInfo>(_fileHelper.DetectInstalledEditors());

        LoadSettings();

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        ResetCommand = new RelayCommand(Reset);
    }

    private void LoadSettings()
    {
        var config = _configManager.Config;

        SelectedProvider = config.AIConfig.Provider;
        ApiKey = config.AIConfig.ApiKey;
        ApiEndpoint = config.AIConfig.ApiEndpoint ?? string.Empty;
        ModelName = config.AIConfig.ModelName ?? string.Empty;
        SecurityLevel = config.SecurityLevel;
        DefaultEditor = config.DefaultEditor;
        HistoryRetentionDays = config.HistoryRetentionDays;
    }

    private void Save()
    {
        var config = _configManager.Config;

        config.AIConfig.Provider = SelectedProvider;
        config.AIConfig.ApiKey = ApiKey;
        config.AIConfig.ApiEndpoint = string.IsNullOrWhiteSpace(ApiEndpoint) ? null : ApiEndpoint;
        config.AIConfig.ModelName = string.IsNullOrWhiteSpace(ModelName) ? null : ModelName;
        config.SecurityLevel = SecurityLevel;
        config.DefaultEditor = DefaultEditor;
        config.HistoryRetentionDays = HistoryRetentionDays;

        _configManager.SaveConfig();

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Reset()
    {
        if (System.Windows.MessageBox.Show(
            "确定要重置所有设置为默认值吗?",
            "确认",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes)
        {
            _configManager.ResetToDefault();
            LoadSettings();
        }
    }

    private void UpdateDefaultEndpoint()
    {
        if (string.IsNullOrWhiteSpace(ApiEndpoint))
        {
            ApiEndpoint = SelectedProvider switch
            {
                AIProvider.OpenAI => "https://api.openai.com/v1/chat/completions",
                AIProvider.QianWen => "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
                AIProvider.DouBao => "https://ark.cn-beijing.volces.com/api/v3/chat/completions",
                AIProvider.DeepSeek => "https://api.deepseek.com/v1/chat/completions",
                _ => string.Empty
            };
        }
    }
}
