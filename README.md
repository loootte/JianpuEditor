# 简谱编辑器 (JianpuEditor)

基于 C# WinForms 的简谱编辑工具，支持主旋律编辑、副旋律与歌词、连音线，以及 JSON 保存、PDF 与 MIDI 导出。

## 功能

- **主旋律编辑**：输入音符 1–7、休止符 0，点击音符间隙插入
- **时值修饰**
  - 增时线：延一拍 → 延两拍 → 延三拍 → 不延长
  - 减时线：四分 → 八分 → 十六分
  - 高音点 / 低音点 / 附点
- **连音线**：选择起始与结束音符，跨音符绘制连音弧线
- **三行排版**：主旋律、副旋律（左对齐）、歌词（左对齐，随小节宽度等比缩放字号）
- **小节操作**：新建小节、复制小节范围、多小节选择（Ctrl / Shift + 点击）
- **文本编辑**：点击副旋律行或歌词行直接编辑；工具栏也可输入当前小节文本
- **文件**：JSON 格式保存 / 打开
- **PDF 导出**：A4 纵向，每行 4 小节，标题居中，调号/速度/BPM/作曲左对齐，内容溢出时自动缩放
- **MIDI 导出**
  - 主旋律按简谱时值与调号导出
  - 速度由 BPM 参数控制（与谱面「速度」文字独立）
  - 副旋律支持和弦标记，以柱式和弦导出（如 `D`、`Bm7`、`G/D`、`Cmaj7`）
  - 每小节最多 2 个和弦，用空格分隔；1 个和弦占满整小节，2 个和弦各弹半小节

## 环境要求

- Windows
- [.NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/net472) 或更高
- .NET SDK（用于编译）

## 构建与运行

```powershell
dotnet build JianpuEditor.sln -c Debug
.\JianpuEditor\bin\Debug\net472\JianpuEditor.exe
```

Release 构建：

```powershell
dotnet build JianpuEditor.sln -c Release
.\JianpuEditor\bin\Release\net472\JianpuEditor.exe
```

## 安装包

使用 Inno Setup 构建 Windows 安装程序：

```powershell
.\scripts\build-installer.ps1
```

输出文件：`installer/output/JianpuEditor-Setup-1.0.0.exe`

## 基本操作

| 操作 | 说明 |
|------|------|
| 点击音符 | 选中并修改时值、八度、附点等 |
| 点击音符间隙 | 在该位置插入新音符 |
| 点击副旋律 / 歌词行 | 行内编辑文字 |
| Ctrl + 点击小节 | 多选小节 |
| Shift + 点击小节 | 范围选择小节 |
| 连音线 | 点「连音线」→ 选起始音符 → 选结束音符；Esc 取消 |
| 导出 MIDI | 菜单或工具栏「导出 MIDI」，按 BPM 与和弦标记生成可播放文件 |

启动后自动加载《欢乐颂》示例曲谱，也可点工具栏「示例」重新载入。

## 副旋律和弦标记

副旋律行可填写和弦符号，导出 MIDI 时自动解析并弹奏。示例：

```
D
D    Bm7
G/D  Cmaj7
```

规则：

- 每小节最多 2 个和弦，用空格分隔（多个空格亦可）
- 若一行中混有非和弦文字（如「主题 A」），该小节不导出和弦
- 支持和弦类型：大三、小三、`7`、`maj7`、`m7`、转位（如 `G/D`）等常见写法

## 曲谱文件格式

曲谱保存为 JSON（`.json` / `.jianpu`），主要字段：

- `Title`、`KeySignature`、`Tempo`、`Bpm`、`Composer`
- `Measures[]`：每小节含 `MelodyNotes`、`SecondaryText`、`LyricText`
- `Ties[]`：连音线（起始/结束小节与音符索引）

其中 `Tempo` 为谱面显示用语（如「中速」），`Bpm` 为 MIDI 导出使用的每分钟拍数（默认 120）。

## 项目结构

```
JianpuEditor/
  MainForm.cs              # 主界面、工具栏、菜单
  Controls/ScoreCanvas.cs  # 画布、选择、行内编辑
  Models/                  # 曲谱、小节、音符、连音线
  Rendering/               # 布局、绘制、命中测试
  Services/                # JSON 读写、PDF/MIDI 导出、和弦解析、小节复制
  installer/               # Inno Setup 安装脚本
  scripts/                 # 构建与测试脚本
```

## 依赖

- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) 13.0.3
- [PDFsharp](https://www.nuget.org/packages/PDFsharp) 6.2.0

MIDI 导出为自研实现，无第三方 MIDI 库。

## 许可证

未指定许可证，使用前请与仓库维护者确认。