using System.Collections.ObjectModel;
using System.Windows.Input;
using PowerShellHelper.Models;
using PowerShellHelper.Services;

namespace PowerShellHelper.ViewModels;

/// <summary>
/// 历史记录窗口ViewModel
/// </summary>
public class HistoryWindowViewModel : ViewModelBase
{
    private readonly HistoryService _historyService;
    private string _searchKeyword = string.Empty;
    private CommandHistoryRecord? _selectedRecord;

    public ObservableCollection<CommandHistoryRecord> HistoryRecords { get; }

    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value))
            {
                SearchRecords();
            }
        }
    }

    public CommandHistoryRecord? SelectedRecord
    {
        get => _selectedRecord;
        set => SetProperty(ref _selectedRecord, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ReExecuteCommand { get; }

    public event EventHandler<PowerShellCommand>? ReExecuteRequested;

    public HistoryWindowViewModel()
    {
        _historyService = HistoryService.Instance;
        HistoryRecords = new ObservableCollection<CommandHistoryRecord>();

        RefreshCommand = new RelayCommand(LoadRecords);
        DeleteCommand = new RelayCommand<CommandHistoryRecord>(DeleteRecord);
        ClearAllCommand = new RelayCommand(ClearAll);
        ExportCommand = new RelayCommand(Export);
        ReExecuteCommand = new RelayCommand<CommandHistoryRecord>(ReExecute);

        LoadRecords();
    }

    private void LoadRecords()
    {
        HistoryRecords.Clear();
        var records = _historyService.GetRecentRecords(100);
        
        foreach (var record in records)
        {
            HistoryRecords.Add(record);
        }
    }

    private void SearchRecords()
    {
        HistoryRecords.Clear();
        
        if (string.IsNullOrWhiteSpace(SearchKeyword))
        {
            LoadRecords();
            return;
        }

        var results = _historyService.SearchRecords(SearchKeyword);
        foreach (var record in results)
        {
            HistoryRecords.Add(record);
        }
    }

    private void DeleteRecord(CommandHistoryRecord? record)
    {
        if (record == null)
            return;

        var result = System.Windows.MessageBox.Show(
            "确定要删除这条历史记录吗?",
            "确认删除",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question
        );

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            _historyService.DeleteRecord(record.Id);
            HistoryRecords.Remove(record);
        }
    }

    private void ClearAll()
    {
        var result = System.Windows.MessageBox.Show(
            "确定要清空所有历史记录吗?此操作不可恢复!",
            "确认清空",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning
        );

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            _historyService.ClearAllRecords();
            HistoryRecords.Clear();
        }
    }

    private void Export()
    {
        try
        {
            var json = _historyService.ExportToJson();
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON文件|*.json",
                FileName = $"历史记录_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(saveDialog.FileName, json);
                System.Windows.MessageBox.Show(
                    "导出成功!",
                    "提示",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information
                );
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"导出失败: {ex.Message}",
                "错误",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error
            );
        }
    }

    private void ReExecute(CommandHistoryRecord? record)
    {
        if (record == null)
            return;

        var command = new PowerShellCommand
        {
            CommandText = record.GeneratedCommand,
            RiskLevel = record.RiskLevel
        };

        ReExecuteRequested?.Invoke(this, command);
    }
}
