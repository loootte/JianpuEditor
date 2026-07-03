# 简谱编辑器 (JianpuEditor)

基于 C# WinForms 的简谱编辑工具，支持主旋律编辑、和弦标识、歌词、连音线，以及 JSON 保存、PDF/MIDI 导出与谱面播放。

![简谱编辑器界面截图](Screen%20Sample.png)

## 功能

- **主旋律编辑**：输入音符 1–7、休止符 0，点击音符间隙插入
- **时值修饰**
  - 增时线：延一拍 → 延两拍 → 延三拍 → 不延长
  - 减时线：四分 → 八分 → 十六分
  - 高音点 / 低音点 / 附点
- **连音线**
  - 点「连音线」→ 选起始音符 → 选结束音符；Esc 取消
  - 点击弧线可选中（蓝色高亮）
  - Delete / 「删除」可移除选中的连音线
  - 删除连音线首/尾音符时，连音线自动清除
- **和弦标识（副旋律行）**
  - 每小节最多 4 个和弦标识，与四分拍位置对齐
  - 点击空白拍位或工具栏「添加和弦」新增；点击标识可内联编辑
  - 拖动 `::` 改变拍位；拖到另一标识拍位可交换顺序
  - 点击 `x` 或 Delete 删除；工具栏「和弦」框可同步编辑选中项
  - 仅识别为和弦符号的文本会参与播放与 MIDI 导出
- **和弦转调**
  - 工具栏「转调」或菜单「编辑 → 和弦转调...」
  - 输入目标调号后，自动转调副旋律中所有和弦符号（如 `C` → `G`、`Bm7` → `F#m7`、`G/D` → `D/A`）
  - 同步更新曲谱调号字段；主旋律数字简谱不转调
  - 支持调号格式：`C`、`1=G`、`F#`、`Bb`、`D大调` 等
- **歌词行**：点击歌词行内联编辑；工具栏也可输入当前小节歌词
- **三行排版**：主旋律、和弦标识、歌词
- **小节操作**：新建小节；工具栏设置「从 / 到」小节号后复制小节范围
- **文件**：JSON 格式保存 / 打开
- **PDF 导出**：A4 纵向，每行 4 小节；和弦仅输出文字，无编辑边框
- **MIDI 导出**
  - 主旋律按简谱时值与调号导出
  - 和弦按拍位起止时间导出柱式和弦（如 `D`、`Bm7`、`G/D`）
  - 速度由 BPM 控制（与谱面「速度」文字独立）
- **谱面播放**
  - 工具栏「播放 / 停止」，按 BPM 实时播放主旋律与和弦
  - 可拖动蓝色进度条跳转；演奏逻辑与 MIDI 导出共用调度

## 环境要求

- Windows
- [.NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/net472) 或更高
- .NET SDK（用于编译）
- 播放功能需要系统可用的 MIDI 合成器（如 Microsoft GS Wavetable Synth）

## 构建与运行

```powershell
dotnet build JianpuEditor.sln -c Debug
dotnet test JianpuEditor.sln -c Debug   # 需要 .NET 8 SDK
.\scripts\check-format.ps1             # 代码格式检查（CI 同款）
dotnet format JianpuEditor/JianpuEditor.csproj   # 自动修复格式

构建时会自动运行 Roslyn 分析器（`Microsoft.CodeAnalysis.NetAnalyzers`，Recommended 规则集），规则见根目录 `Directory.Build.props` 与 `.editorconfig`。
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

输出文件：`installer/output/JianpuEditor-Setup-1.1.0.exe`

## CI/CD

GitHub Actions 工作流位于 `.github/workflows/`：

| 工作流 | 触发 | 说明 |
|--------|------|------|
| **CI** | `main` 分支 push / PR | `dotnet format` 检查 + Roslyn 分析器 + Release 构建 + 单元测试 + MIDI 冒烟测试（含 NuGet / .NET 缓存） |
| **Release** | 推送标签 `v*` 或手动运行 | 构建安装包（`.exe` + `.zip`），标签发布时自动创建 GitHub Release |

### 发布新版本

```powershell
git tag v1.0.1
git push origin v1.0.1
```

也可在 GitHub **Actions → Release → Run workflow** 中手动指定版本号，仅生成安装包 artifact（不创建 Release）。

## 基本操作

| 操作 | 说明 |
|------|------|
| 点击音符 | 选中并修改时值、八度、附点等 |
| 点击音符间隙 | 在该位置插入新音符 |
| 点击和弦标识 | 选中并内联编辑；工具栏「和弦」框同步 |
| 点击副旋律空白拍位 | 在该拍位添加和弦标识 |
| 点击歌词行 | 行内编辑歌词 |
| 工具栏「从 / 到」+ 复制小节 | 复制指定范围的小节 |
| 连音线 | 点「连音线」→ 选起始/结束音符；点击弧线选中，Delete 删除 |
| 播放 / 停止 | 按 BPM 播放谱面；拖动蓝色进度条可跳转 |
| 转调 | 工具栏「转调」或菜单「和弦转调...」；仅转调和弦标识 |
| 导出 PDF / MIDI | 菜单或工具栏导出 |
| 深色模式 | 菜单 **视图 → 深色模式**（设置会保存到本地，PDF 导出仍为浅色纸面） |

启动后自动加载《欢乐颂》示例曲谱。`sample/` 目录提供更多示例（如《卡农》），可通过 **文件 → 示例曲库** 或工具栏 **曲库** 加载。

## 和弦标识

每小节最多 4 个标识，每个包含自由文本与拍位（`BeatPosition`，0 为第 1 拍）。

编辑界面示例（第 1 小节两个和弦）：

```
拍位: 0      2
      C      G
```

播放与 MIDI 导出时，仅解析合法和弦符号；非和弦文字保留显示但不发声。

支持和弦类型：大三、小三、`7`、`maj7`、`m7`、转位（如 `G/D`）等常见写法。

### 和弦转调

从当前调号（如 `1=C`）转到目标调号（如 `G` 或 `1=G`）时，遍历各小节 `ChordMarkers`，仅对 `ChordParser` 识别为和弦符号的文本做半音转调；自由文字（如「间奏」）保持不变。转调后调号字段更新为 `1=目标音名` 格式。

主旋律简谱数字（1–7）不参与转调；若需移调旋律，请手动编辑。

## 曲谱文件格式

曲谱保存为 JSON（`.json` / `.jianpu`），主要字段：

- `Title`、`KeySignature`、`Tempo`、`Bpm`、`Composer`
- `Measures[]`：每小节含 `MelodyNotes`、`ChordMarkers`、`LyricText`
- `Ties[]`：连音线（起始/结束小节与音符索引）

小节字段说明：

| 字段 | 说明 |
|------|------|
| `MelodyNotes[]` | 主旋律音符 |
| `ChordMarkers[]` | `{ "Text": "C", "BeatPosition": 0 }` |
| `LyricText` | 歌词 |

`Tempo` 为谱面显示用语（如「中速」），`Bpm` 为播放与 MIDI 使用的每分钟拍数（默认 120）。

## 日志

运行日志写入：

```
%LocalAppData%\JianpuEditor\logs\jianpu-editor.log
```

播放或 MIDI 相关出错时，错误弹窗会提示上述日志路径。

## 架构（MVVM）

本项目采用 **CommunityToolkit.Mvvm** + **Microsoft.Extensions.DependencyInjection**，将 WinForms 界面与编辑逻辑分离：

| 层次 | 目录 | 职责 |
|------|------|------|
| **View** | `MainForm.cs`、`Controls/` | 菜单、工具栏、对话框；仅处理 WinForms 与文件选择 |
| **Glue** | `Glue/` | `MainFormViewBinder`（控件 ↔ ViewModel 双向绑定）、`ScoreCanvasGlue`（画布刷新与选择同步）、`ScoreSelectionMapper` |
| **ViewModel** | `ViewModels/` | 编辑命令、谱面状态、选择协调；通过 `IAppMessenger` 发布 `ScoreEditedMessage` 等 |
| **Model** | `Models/` | 纯 POCO：`JianpuScore`、小节、音符、和弦标识 |
| **Core** | `Core/Abstractions/`、`Core/Messaging/` | 服务接口（`IScoreFileService`、`IPdfExportService` 等）与消息总线 |
| **Services** | `Services/` | 静态业务实现 + DI 适配器（`PdfExportServiceAdapter` 等） |
| **Rendering** | `Rendering/` | 布局、绘制、`AppTheme`（浅色/深色主题） |

**数据流**：用户操作 → `MainForm` 调用 ViewModel 方法 → 返回 `ScoreEditResult` → `ScoreCanvasGlue` 更新画布 → `MainFormViewBinder` 同步控件。

**依赖注入**（`AppBootstrapper.cs`）：所有 ViewModel 与 Service 接口注册为 Singleton，`MainForm` 为 Transient。

## 项目结构

```
JianpuEditor/
  Program.cs               # 启动、DI 容器、主题加载
  AppBootstrapper.cs       # 服务与 ViewModel 注册
  MainForm.cs              # 主界面（瘦 View 层）
  Glue/                    # View ↔ ViewModel 胶水层
  ViewModels/              # MVVM ViewModel（10 个）
  Core/                    # 接口抽象与消息
  Controls/ScoreCanvas.cs  # 画布、选择、播放进度条、和弦内联编辑
  Models/                  # 曲谱、小节、音符、连音线、和弦标识
  Rendering/               # 布局、绘制、AppTheme
  Services/                # JSON/PDF/MIDI、播放、和弦解析/转调、连音线维护
  installer/               # Inno Setup 安装脚本
  scripts/                 # 构建与测试脚本
  sample/                  # 示例曲库（.jianpu / .json）
JianpuEditor.Tests/        # xUnit 单元测试（服务、ViewModel、Glue）
```

## 依赖

- [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm) 8.4.0
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) 8.0.1
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) 13.0.3
- [PDFsharp](https://www.nuget.org/packages/PDFsharp) 6.2.0

MIDI 导出与谱面播放为自研实现，无第三方 MIDI 库。

## 贡献

欢迎通过 Issue 与 Pull Request 参与改进。提交代码前请阅读 [贡献者许可协议（CLA）](CLA.md)，并在首个 PR 中确认同意。

## 许可证

本项目采用 [Apache License 2.0](LICENSE) 开源协议。