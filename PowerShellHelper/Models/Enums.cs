namespace PowerShellHelper.Models;

/// <summary>
/// AI模型提供商枚举
/// </summary>
public enum AIProvider
{
    OpenAI,
    AzureOpenAI,
    QianWen,      // 阿里云通义千问
    DouBao,       // 豆包
    DeepSeek
}

/// <summary>
/// 安全策略级别
/// </summary>
public enum SecurityLevel
{
    Strict,    // 严格模式
    Standard,  // 标准模式
    Relaxed    // 宽松模式
}

/// <summary>
/// 命令风险等级
/// </summary>
public enum RiskLevel
{
    Low,      // 绿色 - 低风险
    Medium,   // 黄色 - 中风险
    High      // 红色 - 高风险
}

/// <summary>
/// 命令执行状态
/// </summary>
public enum ExecutionStatus
{
    NotExecuted,  // 未执行
    Success,      // 成功
    Failed,       // 失败
    Cancelled     // 已取消
}

/// <summary>
/// 命令类型分类
/// </summary>
public enum CommandType
{
    Query,        // 只读查询
    FileOperation, // 文件操作
    SystemConfig,  // 系统配置
    NetworkOperation, // 网络操作
    Dangerous     // 危险操作
}
