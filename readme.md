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

### 三级安全策略详解

#### 🔴 严格模式 (Strict)
**最严格的安全防护，适合新手或生产环境**

- **允许执行**：仅允许只读查询命令（`CommandType.Query`）
- **拦截规则**：
  - ❌ 所有非查询命令都会被拦截
  - ❌ 所有非低风险命令都会被拦截
  - 只允许 `Get-*`、`Test-*`、`Select-*`、`Measure-*`、`Show-*`、`Write-*` 等查询类命令

**示例：**
- ✅ 允许：`Get-Process`、`Get-ChildItem`、`Test-Path`
- ❌ 拒绝：`New-Item`、`Set-Content`、`Remove-Item`

---

#### 🟡 标准模式 (Standard) - 默认推荐
**平衡安全性和实用性，适合日常使用**

- **允许执行**：低风险和中风险命令
- **拦截规则**：
  - ❌ 高风险命令（评分 ≥ 70）直接拦截
  - ⚠️ 中风险命令（评分 40-69）需要用户确认
  - ✅ 低风险命令（评分 < 40）直接执行

**示例：**
- ✅ 直接执行：`Get-Process`、`Test-Path`
- ⚠️ 需要确认：`New-Item`、`Set-Content`、`Restart-Service`
- ❌ 拒绝：`Remove-Item C:\Windows\*`、`Format-Volume`

---

#### 🟢 宽松模式 (Relaxed)
**最小限制，适合高级用户**

- **允许执行**：几乎所有命令
- **拦截规则**：
  - ❌ 仅拦截极度危险的命令（高风险 + 评分 ≥ 80）
  - ⚠️ 中风险及以上命令需要确认
  - ✅ 其他命令允许执行

**示例：**
- ✅ 直接执行：所有低风险命令
- ⚠️ 需要确认：大部分中、高风险命令
- ❌ 拒绝：`Format-Volume`、`Remove-Item C:\Windows\* -Force -Recurse`

---

### 风险评分算法

命令的风险评分由以下因素决定（总分 0-100）：

| 因素 | 权重 | 说明 |
|------|------|------|
| **命令动词** | 40% | `Remove`、`Delete`、`Format` = 高风险<br>`Set`、`New`、`Update` = 中风险<br>`Get`、`Test`、`Select` = 低风险 |
| **路径风险** | 30% | 系统目录（C:\Windows）= 高风险<br>用户目录 = 中风险<br>相对路径 = 低风险 |
| **参数风险** | 20% | `-Force`、`-Confirm:$false` 等危险参数 |
| **影响范围** | 10% | `-Recurse`、通配符 `*`、管道操作 |

**风险等级划分：**
- 🟢 **低风险**：评分 < 40
- 🟡 **中风险**：评分 40-69
- 🔴 **高风险**：评分 ≥ 70

---

### 策略对比表

| 场景 | 严格模式 | 标准模式 | 宽松模式 |
|------|---------|---------|----------|
| 查询 Python 版本 | ✅ 允许 | ✅ 允许 | ✅ 允许 |
| 创建新文件 | ❌ 拒绝 | ⚠️ 需确认 | ⚠️ 需确认 |
| 删除文件 | ❌ 拒绝 | ❌ 拒绝 | ⚠️ 需确认 |
| 删除系统目录文件 | ❌ 拒绝 | ❌ 拒绝 | ❌ 拒绝 |

**推荐使用：**
- 🔰 新手/学习环境 → **严格模式**
- 👔 日常办公/开发 → **标准模式**（默认）
- 🚀 高级用户/运维 → **宽松模式**

## 数据存储位置

- 配置文件: `%LOCALAPPDATA%\PowerShellHelper\config.json`

## 已知限制

1. 系统托盘功能未完全实现(需要Hardcodet.NotifyIcon.Wpf包)
2. 全局快捷键功能待实现
3. 应用图标使用默认图标

## 后续支持功能

1. 支持Mac系统（待有Mac开发环境之后）
2. 添加系统托盘支持
2. 实现全局快捷键唤醒
3. 添加更多AI提供商支持
4. 优化UI交互体验
5. 添加命令模板库功能

## 演示
# powershell命令
![alt text](PowerShellHelper\img\image.png)
# 生成文件
![alt text](PowerShellHelper\img\image-1.png)
# 支持的模型
![alt text](PowerShellHelper\img\image-2.png)
# 安全策略
![alt text](PowerShellHelper\img\image-3.png)
