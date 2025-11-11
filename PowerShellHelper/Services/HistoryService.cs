using Microsoft.Data.Sqlite;
using System.IO;
using PowerShellHelper.Models;

namespace PowerShellHelper.Services;

/// <summary>
/// 历史记录服务
/// </summary>
public class HistoryService
{
    private static readonly string DatabasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PowerShellHelper",
        "history.db"
    );

    private static HistoryService? _instance;
    private static readonly object _lock = new object();

    public static HistoryService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new HistoryService();
                }
            }
            return _instance;
        }
    }

    private HistoryService()
    {
        InitializeDatabase();
    }

    /// <summary>
    /// 初始化数据库
    /// </summary>
    private void InitializeDatabase()
    {
        var directory = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();

        var createTableCmd = connection.CreateCommand();
        createTableCmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS CommandHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT NOT NULL,
                UserInput TEXT NOT NULL,
                GeneratedCommand TEXT NOT NULL,
                Status INTEGER NOT NULL,
                ExecutionResult TEXT,
                RiskLevel INTEGER NOT NULL
            );
            
            CREATE INDEX IF NOT EXISTS idx_timestamp ON CommandHistory(Timestamp DESC);
        ";
        createTableCmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 添加历史记录
    /// </summary>
    public void AddRecord(CommandHistoryRecord record)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            connection.Open();

            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO CommandHistory (Timestamp, UserInput, GeneratedCommand, Status, ExecutionResult, RiskLevel)
                VALUES (@timestamp, @userInput, @command, @status, @result, @risk)
            ";

            insertCmd.Parameters.AddWithValue("@timestamp", record.Timestamp.ToString("o"));
            insertCmd.Parameters.AddWithValue("@userInput", record.UserInput);
            insertCmd.Parameters.AddWithValue("@command", SanitizeCommand(record.GeneratedCommand));
            insertCmd.Parameters.AddWithValue("@status", (int)record.Status);
            insertCmd.Parameters.AddWithValue("@result", record.ExecutionResult ?? string.Empty);
            insertCmd.Parameters.AddWithValue("@risk", (int)record.RiskLevel);

            insertCmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存历史记录失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取最近的历史记录
    /// </summary>
    public List<CommandHistoryRecord> GetRecentRecords(int limit = 50)
    {
        var records = new List<CommandHistoryRecord>();

        try
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            connection.Open();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"
                SELECT Id, Timestamp, UserInput, GeneratedCommand, Status, ExecutionResult, RiskLevel
                FROM CommandHistory
                ORDER BY Timestamp DESC
                LIMIT @limit
            ";
            selectCmd.Parameters.AddWithValue("@limit", limit);

            using var reader = selectCmd.ExecuteReader();
            while (reader.Read())
            {
                records.Add(new CommandHistoryRecord
                {
                    Id = reader.GetInt32(0),
                    Timestamp = DateTime.Parse(reader.GetString(1)),
                    UserInput = reader.GetString(2),
                    GeneratedCommand = reader.GetString(3),
                    Status = (ExecutionStatus)reader.GetInt32(4),
                    ExecutionResult = reader.IsDBNull(5) ? null : reader.GetString(5),
                    RiskLevel = (RiskLevel)reader.GetInt32(6)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"读取历史记录失败: {ex.Message}");
        }

        return records;
    }

    /// <summary>
    /// 搜索历史记录
    /// </summary>
    public List<CommandHistoryRecord> SearchRecords(string keyword, DateTime? startDate = null, DateTime? endDate = null)
    {
        var records = new List<CommandHistoryRecord>();

        try
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            connection.Open();

            var selectCmd = connection.CreateCommand();
            var whereConditions = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                whereConditions.Add("(UserInput LIKE @keyword OR GeneratedCommand LIKE @keyword)");
            }

            if (startDate.HasValue)
            {
                whereConditions.Add("Timestamp >= @startDate");
            }

            if (endDate.HasValue)
            {
                whereConditions.Add("Timestamp <= @endDate");
            }

            var whereClause = whereConditions.Count > 0 
                ? "WHERE " + string.Join(" AND ", whereConditions)
                : "";

            selectCmd.CommandText = $@"
                SELECT Id, Timestamp, UserInput, GeneratedCommand, Status, ExecutionResult, RiskLevel
                FROM CommandHistory
                {whereClause}
                ORDER BY Timestamp DESC
                LIMIT 100
            ";

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                selectCmd.Parameters.AddWithValue("@keyword", $"%{keyword}%");
            }

            if (startDate.HasValue)
            {
                selectCmd.Parameters.AddWithValue("@startDate", startDate.Value.ToString("o"));
            }

            if (endDate.HasValue)
            {
                selectCmd.Parameters.AddWithValue("@endDate", endDate.Value.ToString("o"));
            }

            using var reader = selectCmd.ExecuteReader();
            while (reader.Read())
            {
                records.Add(new CommandHistoryRecord
                {
                    Id = reader.GetInt32(0),
                    Timestamp = DateTime.Parse(reader.GetString(1)),
                    UserInput = reader.GetString(2),
                    GeneratedCommand = reader.GetString(3),
                    Status = (ExecutionStatus)reader.GetInt32(4),
                    ExecutionResult = reader.IsDBNull(5) ? null : reader.GetString(5),
                    RiskLevel = (RiskLevel)reader.GetInt32(6)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"搜索历史记录失败: {ex.Message}");
        }

        return records;
    }

    /// <summary>
    /// 删除指定记录
    /// </summary>
    public void DeleteRecord(int id)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            connection.Open();

            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM CommandHistory WHERE Id = @id";
            deleteCmd.Parameters.AddWithValue("@id", id);
            deleteCmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"删除历史记录失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 清理过期记录
    /// </summary>
    public void CleanupOldRecords(int retentionDays)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            connection.Open();

            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM CommandHistory WHERE Timestamp < @cutoffDate";
            deleteCmd.Parameters.AddWithValue("@cutoffDate", cutoffDate.ToString("o"));
            
            var deletedCount = deleteCmd.ExecuteNonQuery();
            System.Diagnostics.Debug.WriteLine($"清理了 {deletedCount} 条过期记录");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"清理历史记录失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 清空所有记录
    /// </summary>
    public void ClearAllRecords()
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            connection.Open();

            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM CommandHistory";
            deleteCmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"清空历史记录失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 脱敏命令(移除敏感信息)
    /// </summary>
    private string SanitizeCommand(string command)
    {
        // 简单的敏感信息检测和脱敏
        var sensitivePatterns = new[]
        {
            @"-Password\s+[^\s]+",
            @"-ApiKey\s+[^\s]+",
            @"-Secret\s+[^\s]+",
            @"-Token\s+[^\s]+"
        };

        var sanitized = command;
        foreach (var pattern in sensitivePatterns)
        {
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized, 
                pattern, 
                match => match.Value.Split(' ')[0] + " ***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }

        return sanitized;
    }

    /// <summary>
    /// 导出历史记录为JSON
    /// </summary>
    public string ExportToJson()
    {
        var records = GetRecentRecords(1000);
        return System.Text.Json.JsonSerializer.Serialize(records, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }
}
