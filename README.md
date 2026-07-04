# 简谱编辑器 (JianpuEditor)

基于 C# WinForms 的简谱编辑工具，支持主旋律编辑、装饰音、和弦标识、歌词、连音线，以及 JSON 保存、PDF/MIDI 导出与谱面播放。

![简谱编辑器界面截图](Screen%20Sample.png)

## 功能

- **主旋律编辑**：输入音符 1–7、休止符 0，点击音符间隙插入
- **多选音符**
  - **Ctrl + 左键**：增选 / 减选单个音符
  - **Shift + 左键**：从锚点到当前音符范围批量增选 / 减选（可跨小节）
  - 跨小节多选时，工具栏「从 / 到」小节范围自动同步高亮
  - 多选时工具栏修饰、删除等操作批量生效
- **时值修饰**
  - **增时+ / 减时-** 六档循环：1/16 → 1/8 → 1/4 → 延 1 拍 → 延 2 拍 → 延 3 拍
  - 高音点 / 低音点 / 附点
- **音高调整**
  - **升 key / 降 key**：在当前调内按音级升降选中音符（1–7 及八度点）
- **拆分 / 合并**
  - **拆分**：1/4 音符拆为 2 个 1/8；长于 1/4 的音符拆成等长 1/4 段；1/16 不可拆
  - **合并**：小节内按音符序号两两配对 `(0,1)、(2,3)…` 合并；须谱面相邻、时值比 ≤ 2；音高取前一个；落单不处理
- **撤回 / 重做**
  - 菜单 **编辑 → 撤回**（**Ctrl+Z**）/ **重做**（**Ctrl+Y**），逐步恢复或重放谱面编辑
  - 覆盖音符编辑、删除、小节、转调、谱头 / 歌词 / 和弦内联编辑、连音线、装饰音、批量歌词编辑等；新建 / 打开 / 加载谱面后清空撤回 / 重做栈
- **装饰音**
  - 工具栏「装饰」区：**倚音** / **颤音** / **回音** / **延长**；菜单 **编辑 → 装饰音** 提供相同入口
  - 先选中一个或多个音符，再点击装饰按钮；多选时批量添加
  - 再次点击同类型按钮可取消该音符上的同类型装饰（其他类型保留）
  - Delete / 「删除」优先移除选中音符上的装饰音
  - 画布与 PDF 导出在音符上方绘制占位符号（倚 / tr / 回 / 延）
  - 谱面播放与 MIDI 导出会展开倚音、颤音、回音、波音与延长时值
- **连音线**
  - 点「连音线」→ 选起始音符 → 选结束音符；Esc 取消
  - 点击弧线可选中（蓝色高亮）
  - Delete / 「删除」可移除选中的连音线
  - 删除连音线首/尾音符时，连音线自动清除
- **和弦标识（副旋律行）**
  - 每小节最多 4 个和弦标识，与四分拍位置对齐
  - 点击空白拍位新增；点击标识可内联编辑
  - 拖动 `::` 改变拍位；拖到另一标识拍位可交换顺序
  - 点击 `x` 或 Delete 删除；工具栏「和弦」框可同步编辑选中项
  - 仅识别为和弦符号的文本会参与播放与 MIDI 导出
- **谱头编辑**：点击谱面标题、调号、速度、BPM、作曲者直接内联编辑（工具栏已精简）
- **和弦转调**
  - 菜单「编辑 → 和弦转调...」
  - 输入目标调号后，自动转调副旋律中所有和弦符号（如 `C` → `G`、`Bm7` → `F#m7`、`G/D` → `D/A`）
  - 同步更新曲谱调号字段；主旋律数字简谱不转调
  - 支持调号格式：`C`、`1=G`、`F#`、`Bb`、`D大调` 等
- **歌词**
  - 点击歌词行内联编辑整行 `LyricText`
  - **批量编辑歌词**：菜单 **编辑 → 批量编辑歌词...**，按小节范围列出各行歌词，可一次修改多小节
  - 可选 **重新对齐当前范围**：将歌词按音节映射到主旋律音符（跳过休止符与连音线延续音），写入 `LyricSyllables`
  - 对齐后画布逐字显示在对应音符下方；未对齐时仍显示整行歌词
  - 批量修改合并为单条撤回记录（**Ctrl+Z** 一步恢复）
- **三行排版**：主旋律、和弦标识、歌词
- **小节操作**
  - 新建小节；**编辑 → 新增小节（含占位符）**（**Ctrl+Shift+N**）可预填 4 个四分音符占位
  - **视图 → 新增小节默认填充占位符** 可让普通「新增小节」也带占位符
  - 工具栏设置「从 / 到」小节号后复制小节范围
- **文件**：JSON 格式保存 / 打开
- **PDF 导出**：A4 纵向，每行 4 小节；和弦仅输出文字，无编辑边框
- **MIDI 导出**
  - 主旋律按简谱时值与调号导出；装饰音展开为额外 MIDI 音符或延长时值
  - 和弦按拍位起止时间导出柱式和弦（如 `D`、`Bm7`、`G/D`）
  - 速度由 BPM 控制（与谱面「速度」文字独立）
- **谱面播放**
  - 工具栏「播放 / 停止」，按 BPM 实时播放主旋律与和弦
  - 可拖动蓝色进度条跳转；演奏逻辑与 MIDI 导出共用调度（含装饰音展开）

## 环境要求

- Windows
- [.NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/net472) 或更高
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（用于单元测试；仓库根目录 `global.json` 指定版本）
- Rider 运行测试需安装 **.NET 8 x86 运行时**（32 位 ReSharper Test Runner 使用 `Program Files (x86)\dotnet`）；升级 TFM 后请 **Build → Rebuild Solution** 并清除旧的 `bin/Debug/net6.0` 缓存
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

输出文件：`installer/output/JianpuEditor-Setup-1.2.0.exe`

## CI/CD

GitHub Actions 工作流位于 `.github/workflows/`：

| 工作流 | 触发 | 说明 |
|--------|------|------|
| **CI** | `main` 分支 push / PR | `dotnet format` 检查 + Roslyn 分析器 + Release 构建 + 单元测试 + MIDI 冒烟测试（含 NuGet / .NET 缓存） |
| **Release** | 推送标签 `v*` 或手动运行 | 构建安装包（`.exe` + `.zip`），标签发布时自动创建 GitHub Release |

### 发布新版本

```powershell
git tag v1.2.0
git push origin v1.2.0
```

也可在 GitHub **Actions → Release → Run workflow** 中手动指定版本号，仅生成安装包 artifact（不创建 Release）。

## 基本操作

| 操作 | 说明 |
|------|------|
| 点击音符 | 选中并修改时值、八度、附点等 |
| Ctrl / Shift + 点击音符 | 多选音符；跨小节时同步高亮小节范围 |
| 点击音符间隙 | 在该位置插入新音符 |
| 点击谱头字段 | 内联编辑标题、调号、速度、BPM、作曲者 |
| 点击和弦标识 | 选中并内联编辑；工具栏「和弦」框同步 |
| 点击副旋律空白拍位 | 在该拍位添加和弦标识 |
| 点击歌词行 | 行内编辑整行歌词 |
| 批量编辑歌词 | **编辑 → 批量编辑歌词...**；可勾选重新对齐，对齐后逐字显示在音符下 |
| 增时+ / 减时- | 六档时值循环（1/16 至延 3 拍） |
| 升 key / 降 key | 在当前调内升降选中音符音高 |
| 拆分 / 合并 | 拆分或合并选中音符时值 |
| 工具栏「从 / 到」+ 复制小节 | 复制指定范围的小节 |
| 连音线 | 点「连音线」→ 选起始/结束音符；点击弧线选中，Delete 删除 |
| 装饰音 | 选中音符后点工具栏「倚音 / 颤音 / 回音 / 延长」；再次点击同按钮取消 |
| 撤回 / 重做 | **编辑 → 撤回 / 重做** 或 **Ctrl+Z** / **Ctrl+Y** |
| 播放 / 停止 | 按 BPM 播放谱面；拖动蓝色进度条可跳转 |
| 转调 | 菜单「编辑 → 和弦转调...」；仅转调和弦标识 |
| 导出 PDF / MIDI | 菜单「文件」导出 |
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

## 歌词与对齐

每小节歌词有两种表示：

- **`LyricText`**：整行文本，点击歌词行可直接编辑
- **`LyricSyllables`**：音节列表，每个音节绑定一个主旋律音符索引

**重新对齐**规则：

- 中文按字、英文按空格分词，依次映射到可对齐的旋律音符
- 休止符、连音线延续音（tie 终点）不参与对齐
- 歌词字数与音符数不一致时，对齐尽可能多的音节并在状态中提示超出 / 剩余

**批量编辑歌词**对话框支持指定「从第 N 到第 M 小节」，初始范围取自当前选中小节；修改后可选重新对齐，所有变更作为一条操作可撤回。

## 曲谱文件格式

曲谱保存为 JSON（`.json` / `.jianpu`），主要字段：

- `Title`、`KeySignature`、`Tempo`、`Bpm`、`Composer`
- `Measures[]`：每小节含 `MelodyNotes`、`ChordMarkers`、`LyricText`、`LyricSyllables`、`Ornaments`
- `Ties[]`：连音线（起始/结束小节与音符索引）

小节字段说明：

| 字段 | 说明 |
|------|------|
| `MelodyNotes[]` | 主旋律音符 |
| `ChordMarkers[]` | `{ "Text": "C", "BeatPosition": 0 }` |
| `LyricText` | 歌词整行文本 |
| `LyricSyllables[]` | 逐音节歌词，`{ "Text": "你", "NoteIndex": 0, "BeatPosition": 0 }`；有数据时画布按音符逐字绘制 |
| `Ornaments[]` | 装饰音，`{ "Type": "Trill", "NoteIndex": 0, "BeatPosition": 0 }`；`Type` 为枚举名（如 `GraceNote`、`Trill`、`Turn`、`Fermata`） |

打开旧谱面时，若仅有 `LyricText` 而无 `LyricSyllables`，仍按整行显示；执行重新对齐或批量编辑并勾选对齐后，会生成音节数据。无 `Ornaments` 字段的旧文件可正常打开。

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
| **View** | `MainForm.cs`、`Views/`、`Controls/` | 菜单、工具栏、对话框；`ILayoutService` / `WinFormsLayoutService` 管理布局与 DPI；瘦 View 层仅处理 WinForms 与文件选择 |
| **Glue** | `Glue/` | `MainFormViewBinder`（控件 ↔ ViewModel 双向绑定）、`ScoreCanvasGlue`（画布刷新与选择同步）、`ScoreSelectionMapper` |
| **ViewModel** | `ViewModels/` | 编辑命令、谱面状态、选择协调；通过 `IAppMessenger` 发布 `ScoreEditedMessage` 等 |
| **Model** | `Models/` | 纯 POCO：`JianpuScore`、小节、音符、和弦标识 |
| **Core** | `Core/Abstractions/`、`Core/Messaging/` | 服务接口（`IScoreFileService`、`IScoreUndoService`、`IPdfExportService` 等）与消息总线 |
| **Services** | `Services/` | 静态业务实现 + DI 适配器；含 `EditCommandHistory`（撤回 / 重做）、`OrnamentService`、`OrnamentPlaybackService`、`LyricAlignmentService`、`BulkLyricEditService`、`NoteSplitMergeService`、`JianpuPitchService` 等 |
| **Rendering** | `Rendering/` | 布局、绘制、`AppTheme`（浅色/深色主题） |

**数据流**：用户操作 → `MainForm` 调用 ViewModel 方法 → 返回 `ScoreEditResult` → `ScoreCanvasGlue` 更新画布 → `MainFormViewBinder` 同步控件。

**依赖注入**（`AppBootstrapper.cs`）：所有 ViewModel 与 Service 接口注册为 Singleton，`MainForm` 为 Transient。

## 项目结构

```
JianpuEditor/
  Program.cs               # 启动、DI 容器、主题加载
  AppBootstrapper.cs       # 服务与 ViewModel 注册
  MainForm.cs              # 主界面（瘦 View 层）
  Views/                   # ILayoutService、布局上下文
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
JianpuEditor.Tests/        # xUnit 单元测试（199 个；服务、ViewModel、Glue、绘制）
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