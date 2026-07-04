# MIDI 导入 Spike (v1.3) — Issue #51

## 目标

从 MIDI 文件生成可编辑的 `.jianpu` 简谱，降低示例曲库与制谱成本（关联 #21 / #22）。

## 实现方案

| 模块 | 说明 |
|------|------|
| `MidiImportService` | 自研 MIDI 解析（与 `MidiExportService` 对称，无第三方库） |
| 旋律轨选择 | Format 0 直接用；Format 1 取非鼓轨音符最多的一轨 |
| 时值量化 | 十六分网格（0.25 拍） |
| 音高映射 | 逆运算 `ScoreMidiSchedule.ToMelodyMidiNote`，自动检测 `1=C`…`1=B` |
| 小节拆分 | 默认 4/4（每小节 4 拍），间隙自动填休止符 |
| UI | **文件 → 导入 MIDI... (Spike)** |

## 验收（Spike）

- [x] 支持单旋律 / 简单多轨 MIDI（旋律通道 0）
- [x] 自动转为简谱数字 1–7 + 时值 + 八度点
- [x] 输出可在编辑器中编辑、播放、导出
- [x] 导出 → 导入往返测试（`MidiImportServiceTests`）

## 命令行试用

```powershell
# 先导出一份 MIDI
.\JianpuEditor\bin\Debug\net472\JianpuEditor.exe

# 或脚本：将 MIDI 转为 .jianpu JSON
.\scripts\import-midi.ps1 -InputMidi "path\to\file.mid" -OutputJianpu "out.jianpu"
```

## 已知限制

- **不导入**：连音线、装饰音、歌词、和弦（仅主旋律通道）
- **量化**：仅支持 1/16、1/8、附点、1/4–全音等离散时值；复杂连音可能近似
- **升降号**：半音音高映射到最近调内音级（与现有播放模型一致）
- **格式**：不支持 SMPTE 时间码；多轨合并未做声部分离

## 后续计划

1. 菜单去掉 “(Spike)” 标记，与打开/保存流程统一
2. 导入对话框可选调号、量化精度、每小节拍数
3. 第二轨导入为和弦标识（#22）
4. 往返导出对比测试扩展至《欢乐颂》示例