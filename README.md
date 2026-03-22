# textdiff

一个原生 Windows 桌面端的文本对比工具，使用 WPF + AvalonEdit 构建。

## 当前功能

- 左右双栏文本编辑
- 支持从文件加载或从剪贴板粘贴
- 自动比较与手动比较
- 按行差异高亮
- 左右编辑区滚动同步
- 底部状态栏显示差异统计和光标位置

## 运行方式

```powershell
dotnet run --project .\TextDiff.Desktop\TextDiff.Desktop.csproj
```

## 生成安装包

先安装 `Inno Setup 6`，然后在项目根目录执行：

```powershell
.\scripts\build-installer.ps1
```

这个脚本会先发布自包含的单文件程序，再调用 Inno Setup 生成安装包。

默认输出位置：

- 发布目录：`TextDiff.Desktop\bin\Release\net10.0-windows\win-x64\publish\`
- 安装包目录：`installer\Output\`

## 界面说明

- 左侧和右侧分别输入或载入文本
- 工具栏可快速打开文件、粘贴内容、交换左右和清空
- 黄色表示修改行
- 红色表示仅左侧存在
- 绿色表示仅右侧存在

## 技术栈

- .NET 10
- WPF
- AvalonEdit
