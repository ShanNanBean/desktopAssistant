# PowerShell 桌面助手 - 快速开始指南

## 🚀 快速开始

### 步骤 1: 安装 .NET 8 SDK

1. 访问 https://dotnet.microsoft.com/download/dotnet/8.0
2. 下载并安装 .NET 8 SDK (Windows x64)
3. 验证安装:
   ```bash
   dotnet --version
   ```

### 步骤 2: 编译项目

打开命令提示符或PowerShell,执行:

```bash
# 进入项目目录
cd f:\Dev\qoder\desktopAssistant

# 还原NuGet包
dotnet restore PowerShellHelper/PowerShellHelper.csproj

# 编译项目
dotnet build PowerShellHelper/PowerShellHelper.csproj -c Release
```

### 步骤 3: 运行应用

```bash
dotnet run --project PowerShellHelper/PowerShellHelper.csproj
```

或者直接运行编译后的exe文件:
```bash
.\PowerShellHelper\bin\Release\net8.0-windows\PowerShellHelper.exe
```

### 步骤 4: 首次配置

1. 应用启动后,点击右上角 "⚙ 设置" 按钮
2. 选择AI提供商(推荐选择您有API密钥的服务)
3. 输入API密钥
4. 选择安全级别(建议首次使用选择"标准模式")
5. 点击"保存"

### 步骤 5: 开始使用

在输入框中输入您的需求,例如:
- "查看当前目录的文件列表"
- "显示系统进程"
- "创建一个test文件夹"

AI会为您生成对应的PowerShell命令,确认后即可执行!

## 🔑 获取AI API密钥

### OpenAI
- 注册: https://platform.openai.com/signup
- 获取密钥: https://platform.openai.com/api-keys

### 通义千问 (阿里云)
- 注册: https://dashscope.aliyun.com/
- 获取密钥: 控制台 > API-KEY管理

### DeepSeek
- 注册: https://platform.deepseek.com/
- 获取密钥: 平台控制台

### 豆包
- 注册: https://www.volcengine.com/
- 获取密钥: 火山引擎控制台

## ⚠️ 注意事项

1. **API密钥安全**: 请妥善保管您的API密钥,不要分享给他人
2. **命令确认**: 执行重要命令前请仔细确认
3. **安全级别**: 新手用户建议使用"严格模式"或"标准模式"
4. **网络连接**: 需要稳定的网络连接访问AI服务

## 📝 常见问题

### Q1: 提示"API密钥验证失败"?
**A**: 请检查:
- API密钥是否正确
- 选择的AI提供商是否匹配
- 网络连接是否正常
- API密钥是否有余额

### Q2: 命令被拦截无法执行?
**A**: 这是安全机制在保护您:
- 检查命令是否真的安全
- 可以在设置中调整安全级别
- 严格模式下仅允许只读命令

### Q3: 如何清除历史记录?
**A**: 
- 点击"📋 历史"按钮
- 点击右下角"🗑️ 清空全部"
- 或在设置中调整历史保留天数

### Q4: 程序无法启动?
**A**: 请确保:
- 已安装 .NET 8 SDK
- 系统为 Windows 11
- 以管理员身份运行(如需要)

## 🎯 使用技巧

1. **描述要清晰**: 越详细的描述,AI生成的命令越准确
2. **查看说明**: AI会解释命令的作用,执行前请阅读
3. **善用历史**: 常用命令可以从历史中快速重新执行
4. **分步操作**: 复杂任务建议分多次执行
5. **学习命令**: 通过生成的命令学习PowerShell语法

## 📚 相关文档

- 详细使用说明: `USAGE.md`
- 项目完成总结: `PROJECT_SUMMARY.md`
- 系统设计文档: `.qoder/quests/power-shell-desktop-helper.md`

## 💡 示例场景

### 场景1: 文件管理
```
用户输入: "列出D盘根目录下所有大于100MB的文件"
生成命令: Get-ChildItem -Path D:\ -File | Where-Object {$_.Length -gt 100MB} | Select-Object Name, Length
```

### 场景2: 系统信息
```
用户输入: "查看内存使用情况"
生成命令: Get-CimInstance Win32_OperatingSystem | Select-Object TotalVisibleMemorySize, FreePhysicalMemory
```

### 场景3: 进程管理
```
用户输入: "显示占用CPU最高的5个进程"
生成命令: Get-Process | Sort-Object CPU -Descending | Select-Object -First 5
```

## 🆘 获取帮助

如果遇到问题:
1. 查看 `USAGE.md` 详细文档
2. 检查 `PROJECT_SUMMARY.md` 中的功能说明
3. 查看应用内"关于"页面

---

**祝使用愉快! 🎉**
