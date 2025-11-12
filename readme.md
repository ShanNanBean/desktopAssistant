# PowerShell 桌面助手 - 使用说明

## 项目结构说明

本项目已完成核心功能的实现,包括:

### 已实现的功能模块

1. **核心数据模型** (`Models/`)
   - 枚举类型定义 (AIProvider, SecurityLevel, RiskLevel等)
   - 应用配置模型
   - 数据传输对象

2. **服务层** (`Services/`)
   - `ConfigManager`: 配置管理(支持DPAPI加密)
   - `AIService`: AI模型集成(支持OpenAI、Azure、通义千问、豆包、DeepSeek)
   - `SafetyCheckerService`: 命令安全检查(黑白名单、风险评分)
   - `PowerShellExecutor`: PowerShell命令执行
   - `FileHelperService`: 文件操作辅助

3. **UI界面** (`Views/` 和 `ViewModels/`)
   - 主对话窗口
   - 设置窗口

## 编译和运行要求

### 前置条件
1. 安装 .NET 8 SDK
2. Windows 11 操作系统

### 编译步骤
由于当前环境无.NET SDK,需要手动编译:

1. 安装.NET 8 SDK
   - 下载地址: https://dotnet.microsoft.com/download/dotnet/8.0
   
2. 打开命令行,导航到项目目录:
   ```
   cd f:\Dev\qoder\desktopAssistant
   ```

3. 还原NuGet包:
   ```
   dotnet restore PowerShellHelper/PowerShellHelper.csproj
   ```

4. 编译项目:
   ```
   dotnet build PowerShellHelper/PowerShellHelper.csproj
   ```

5. 运行项目:
   ```
   dotnet run --project PowerShellHelper/PowerShellHelper.csproj
   ```

## 首次使用配置

1. 启动应用后,点击右上角的"⚙ 设置"按钮
2. 在"AI模型配置"标签页中:
   - 选择AI提供商(如OpenAI、通义千问等)
   - 输入API密钥
   - (可选)自定义API端点和模型名称
3. 在"安全策略"标签页中选择适合的安全级别
4. 点击"保存"

## 使用示例

### 基本操作
1. 在输入框中输入自然语言需求,例如:
   - "查看当前目录下的所有文件"
   - "列出系统中运行的所有进程"
   - "创建一个名为test.txt的文件"

2. AI会生成相应的PowerShell命令并显示
3. 根据风险等级,可能需要确认后才能执行
4. 执行结果会显示在对话窗口中

## 安全机制

### 三级安全策略
- **严格模式**: 仅允许只读查询命令
- **标准模式**: 允许文件操作但需确认,默认推荐
- **宽松模式**: 允许大部分操作,仅拦截极危险命令

### 风险评分算法
命令会根据以下维度评分:
- 命令动词风险 (40%权重)
- 操作路径风险 (30%权重)
- 参数风险 (20%权重)
- 影响范围 (10%权重)

## 数据存储位置

- 配置文件: `%LOCALAPPDATA%\PowerShellHelper\config.json`

## 已知限制

1. 系统托盘功能未完全实现(需要Hardcodet.NotifyIcon.Wpf包)
2. 全局快捷键功能待实现
3. 应用图标使用默认图标

## 后续改进建议

1. 添加系统托盘支持
2. 实现全局快捷键唤醒
3. 添加更多AI提供商支持
4. 优化UI交互体验
5. 添加命令模板库功能