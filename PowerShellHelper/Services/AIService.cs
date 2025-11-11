using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PowerShellHelper.Models;

namespace PowerShellHelper.Services;

/// <summary>
/// AI服务接口
/// </summary>
public interface IAIService
{
    Task<AIResponse> GenerateCommandAsync(string userInput, List<ChatMessage> context);
}

/// <summary>
/// AI服务实现
/// </summary>
public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly ConfigManager _configManager;
    
    private const string SystemPrompt = @"你是一个专业的PowerShell助手。你的任务是根据用户的自然语言需求,生成安全、准确的PowerShell命令。

重要规则:
1. 只生成PowerShell命令,不要生成CMD或批处理命令
2. 严禁生成以下危险操作:
   - 格式化磁盘 (Format-Volume, diskpart等)
   - 删除系统文件或系统目录
   - 删除或修改关键注册表项
   - 绕过执行策略
   - 终止系统关键进程
3. 对于文件删除、批量操作等敏感操作,务必谨慎
4. 使用规范的PowerShell语法和cmdlet

输出格式(严格遵守):
```powershell
<你生成的PowerShell命令>
```
说明: <简短说明命令的作用>
风险: <低/中/高>
影响: <命令可能影响的文件、目录或系统范围>";

    public AIService()
    {
        _httpClient = new HttpClient();
        _configManager = ConfigManager.Instance;
    }

    public async Task<AIResponse> GenerateCommandAsync(string userInput, List<ChatMessage> context)
    {
        try
        {
            var config = _configManager.Config.AIConfig;
            
            if (string.IsNullOrEmpty(config.ApiKey))
            {
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = "未配置API密钥,请在设置中配置"
                };
            }

            string endpoint = GetEndpoint(config);
            var requestBody = BuildRequestBody(config, userInput, context);
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", config.ApiKey);
            _httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = $"API请求失败: {response.StatusCode}, {errorContent}"
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return ParseResponse(responseContent, config.Provider);
        }
        catch (TaskCanceledException)
        {
            return new AIResponse
            {
                Success = false,
                ErrorMessage = "请求超时,请检查网络连接"
            };
        }
        catch (Exception ex)
        {
            return new AIResponse
            {
                Success = false,
                ErrorMessage = $"发生错误: {ex.Message}"
            };
        }
    }

    private string GetEndpoint(AIModelConfig config)
    {
        if (!string.IsNullOrEmpty(config.ApiEndpoint))
            return config.ApiEndpoint;

        return config.Provider switch
        {
            AIProvider.OpenAI => "https://api.openai.com/v1/chat/completions",
            AIProvider.AzureOpenAI => config.ApiEndpoint ?? throw new InvalidOperationException("Azure OpenAI需要配置端点"),
            AIProvider.QianWen => "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
            AIProvider.DouBao => "https://ark.cn-beijing.volces.com/api/v3/chat/completions",
            AIProvider.DeepSeek => "https://api.deepseek.com/v1/chat/completions",
            _ => throw new NotSupportedException($"不支持的AI提供商: {config.Provider}")
        };
    }

    private object BuildRequestBody(AIModelConfig config, string userInput, List<ChatMessage> context)
    {
        var messages = new List<object>
        {
            new { role = "system", content = SystemPrompt }
        };

        // 添加最近的对话上下文(最多10轮)
        var recentContext = context.TakeLast(10).ToList();
        foreach (var msg in recentContext)
        {
            messages.Add(new
            {
                role = msg.IsUser ? "user" : "assistant",
                content = msg.Content
            });
        }

        // 添加当前用户输入
        messages.Add(new { role = "user", content = userInput });

        string modelName = config.ModelName ?? GetDefaultModel(config.Provider);

        return new
        {
            model = modelName,
            messages = messages,
            max_tokens = config.MaxTokens,
            temperature = 0.7
        };
    }

    private string GetDefaultModel(AIProvider provider)
    {
        return provider switch
        {
            AIProvider.OpenAI => "gpt-4o-mini",
            AIProvider.AzureOpenAI => "gpt-4",
            AIProvider.QianWen => "qwen-plus",
            AIProvider.DouBao => "doubao-pro-32k",
            AIProvider.DeepSeek => "deepseek-chat",
            _ => "gpt-3.5-turbo"
        };
    }

    private AIResponse ParseResponse(string responseJson, AIProvider provider)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            string? content = null;

            // 解析响应内容
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message))
                {
                    if (message.TryGetProperty("content", out var contentProp))
                    {
                        content = contentProp.GetString();
                    }
                }
            }

            if (string.IsNullOrEmpty(content))
            {
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = "AI返回内容为空"
                };
            }

            // 解析命令和说明
            var command = ParseCommandFromContent(content);

            return new AIResponse
            {
                Success = true,
                Content = content,
                Command = command
            };
        }
        catch (Exception ex)
        {
            return new AIResponse
            {
                Success = false,
                ErrorMessage = $"解析响应失败: {ex.Message}"
            };
        }
    }

    private PowerShellCommand ParseCommandFromContent(string content)
    {
        var command = new PowerShellCommand();

        // 提取PowerShell代码块
        var codeMatch = Regex.Match(content, @"```(?:powershell)?\s*\n(.*?)\n```", RegexOptions.Singleline);
        if (codeMatch.Success)
        {
            command.CommandText = codeMatch.Groups[1].Value.Trim();
        }
        else
        {
            // 如果没有代码块,尝试提取整行命令
            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Get-") || trimmed.StartsWith("Set-") || 
                    trimmed.StartsWith("New-") || trimmed.StartsWith("Remove-") ||
                    trimmed.StartsWith("Test-") || trimmed.StartsWith("Start-") ||
                    trimmed.StartsWith("Stop-"))
                {
                    command.CommandText = trimmed;
                    break;
                }
            }
        }

        // 提取说明
        var descMatch = Regex.Match(content, @"说明[:：]\s*(.+?)(?:\n|$)", RegexOptions.Singleline);
        if (descMatch.Success)
        {
            command.Description = descMatch.Groups[1].Value.Trim();
        }

        // 提取风险等级
        var riskMatch = Regex.Match(content, @"风险[:：]\s*([低中高])", RegexOptions.Singleline);
        if (riskMatch.Success)
        {
            command.RiskLevel = riskMatch.Groups[1].Value switch
            {
                "低" => RiskLevel.Low,
                "中" => RiskLevel.Medium,
                "高" => RiskLevel.High,
                _ => RiskLevel.Medium
            };
        }
        else
        {
            command.RiskLevel = RiskLevel.Medium;
        }

        return command;
    }
}
