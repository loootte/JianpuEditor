using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using JianpuEditor.Controls;
using JianpuEditor.Models;
using JianpuEditor.Services;

namespace JianpuEditor
{
    public sealed class MainForm : Form
    {
        private readonly ScoreCanvas _canvas = new ScoreCanvas();
        private readonly TextBox _titleBox = new TextBox();
        private readonly TextBox _keyBox = new TextBox();
        private readonly TextBox _tempoBox = new TextBox();
        private readonly TextBox _composerBox = new TextBox();
        private readonly TextBox _secondaryBox = new TextBox();
        private readonly TextBox _lyricBox = new TextBox();
        private readonly NumericUpDown _measureSelector = new NumericUpDown();
        private readonly NumericUpDown _measureRangeFrom = new NumericUpDown();
        private readonly NumericUpDown _measureRangeTo = new NumericUpDown();
        private readonly Label _statusLabel = new Label();

        private JianpuNote _pendingNote = CreateDefaultNote();
        private string _currentFilePath;
        private bool _suppressMeasureTextSync;
        private bool _suppressMeasureRangeSync;
        private bool _tieModeActive;
        private int _tieStartMeasureIndex = -1;
        private int _tieStartNoteIndex = -1;
        private Button _tieButton;

        public MainForm()
        {
            Text = "简谱编辑器";
            Width = 1280;
            Height = 820;
            MinimumSize = new Size(960, 640);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei", 9f);
            KeyPreview = true;
            KeyDown += OnFormKeyDown;

            BuildMenu();
            BuildCanvas();
            BuildFooter();
            BuildToolbar();
            BuildHeader();

            _canvas.SelectionChanged += OnCanvasSelectionChanged;
            _canvas.MeasureTextEdited += OnCanvasMeasureTextEdited;
            _canvas.Score = new JianpuScore();
            LoadDemoScore();
            UpdateStatus("就绪 - 点击音符修改，点击音符间隙插入，点击副旋律/歌词行编辑文字");
        }

        private static JianpuNote CreateDefaultNote()
        {
            return new JianpuNote
            {
                Type = NoteType.Note,
                Pitch = 1,
                Octave = 0,
                Underlines = 0,
                Dashes = 0,
                Dotted = false
            };
        }

        private JianpuMeasure CurrentMeasure
        {
            get
            {
                EnsureMeasures();
                return _canvas.Score.Measures[_canvas.SelectedMeasureIndex];
            }
        }

        private JianpuNote SelectedNote
        {
            get
            {
                if (_canvas.SelectedNoteIndex < 0)
                {
                    return null;
                }

                var notes = CurrentMeasure.MelodyNotes;
                if (_canvas.SelectedNoteIndex >= notes.Count)
                {
                    return null;
                }

                return notes[_canvas.SelectedNoteIndex];
            }
        }

        private void BuildMenu()
        {
            var menu = new MenuStrip();

            var fileMenu = new ToolStripMenuItem("文件");
            fileMenu.DropDownItems.Add(CreateMenuItem("新建", Keys.Control | Keys.N, OnNewScore));
            fileMenu.DropDownItems.Add(CreateMenuItem("打开...", Keys.Control | Keys.O, OnOpenScore));
            fileMenu.DropDownItems.Add(CreateMenuItem("保存", Keys.Control | Keys.S, OnSaveScore));
            fileMenu.DropDownItems.Add(CreateMenuItem("另存为...", Keys.Control | Keys.Shift | Keys.S, OnSaveScoreAs));
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(CreateMenuItem("导出 PDF...", Keys.Control | Keys.P, OnExportPdf));
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(CreateMenuItem("退出", Keys.None, (s, e) => Close()));

            var editMenu = new ToolStripMenuItem("编辑");
            editMenu.DropDownItems.Add(CreateMenuItem("撤销最后一个音符", Keys.Control | Keys.Z, (s, e) => DeleteLast()));
            editMenu.DropDownItems.Add(CreateMenuItem("新增小节", Keys.None, (s, e) => AddMeasure()));
            editMenu.DropDownItems.Add(CreateMenuItem("复制小节", Keys.None, (s, e) => DuplicateMeasures()));
            editMenu.DropDownItems.Add(CreateMenuItem("清空谱面", Keys.None, OnClearScore));

            menu.Items.Add(fileMenu);
            menu.Items.Add(editMenu);
            MainMenuStrip = menu;
        }

        private void BuildHeader()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 88,
                Padding = new Padding(12, 8, 12, 8),
                ColumnCount = 8
            };

            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            _titleBox.TextChanged += (s, e) => _canvas.Score.Title = _titleBox.Text;
            _keyBox.TextChanged += (s, e) => _canvas.Score.KeySignature = _keyBox.Text;
            _tempoBox.TextChanged += (s, e) => _canvas.Score.Tempo = _tempoBox.Text;
            _composerBox.TextChanged += (s, e) => _canvas.Score.Composer = _composerBox.Text;

            panel.Controls.Add(new Label { Text = "标题", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            panel.Controls.Add(_titleBox, 1, 0);
            panel.Controls.Add(new Label { Text = "调号", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 2, 0);
            panel.Controls.Add(_keyBox, 3, 0);
            panel.Controls.Add(new Label { Text = "速度", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 4, 0);
            panel.Controls.Add(_tempoBox, 5, 0);
            panel.Controls.Add(new Label { Text = "作曲", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 6, 0);
            panel.Controls.Add(_composerBox, 7, 0);

            Controls.Add(panel);
        }

        private void BuildToolbar()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 120,
                Padding = new Padding(12, 8, 12, 8),
                WrapContents = true,
                AutoScroll = true
            };

            panel.Controls.Add(CreateToolButton("打开", () => OnOpenScore(null, EventArgs.Empty)));
            panel.Controls.Add(CreateToolButton("保存", () => OnSaveScore(null, EventArgs.Empty)));
            panel.Controls.Add(CreateToolButton("导出PDF", () => OnExportPdf(null, EventArgs.Empty)));
            panel.Controls.Add(CreateSeparator());

            panel.Controls.Add(new Label { Text = "音符:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            for (var pitch = 1; pitch <= 7; pitch++)
            {
                var p = pitch;
                panel.Controls.Add(CreateToolButton(p.ToString(), () => AddNote(p)));
            }

            panel.Controls.Add(CreateToolButton("0", AddRest));
            panel.Controls.Add(CreateToolButton("新小节", AddMeasure));
            panel.Controls.Add(CreateSeparator());

            panel.Controls.Add(new Label { Text = "修饰:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            panel.Controls.Add(CreateToolButton("高音·", () => SetOctave(1)));
            panel.Controls.Add(CreateToolButton("低音·", () => SetOctave(-1)));
            panel.Controls.Add(CreateToolButton("附点", ToggleSelectedDotted));
            panel.Controls.Add(CreateToolButton("增时线", CycleSelectedExtension));
            panel.Controls.Add(CreateToolButton("减时线", CycleSelectedDuration));
            _tieButton = CreateToolButton("连音线", ToggleTieMode);
            panel.Controls.Add(_tieButton);
            panel.Controls.Add(CreateSeparator());

            panel.Controls.Add(new Label { Text = "当前小节:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            _measureSelector.Minimum = 1;
            _measureSelector.Maximum = 1;
            _measureSelector.Width = 56;
            _measureSelector.ValueChanged += (s, e) => SelectMeasure((int)_measureSelector.Value - 1);
            panel.Controls.Add(_measureSelector);

            panel.Controls.Add(new Label { Text = "从:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            _measureRangeFrom.Minimum = 1;
            _measureRangeFrom.Maximum = 1;
            _measureRangeFrom.Width = 56;
            _measureRangeFrom.ValueChanged += OnMeasureRangeChanged;
            panel.Controls.Add(_measureRangeFrom);

            panel.Controls.Add(new Label { Text = "到:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            _measureRangeTo.Minimum = 1;
            _measureRangeTo.Maximum = 1;
            _measureRangeTo.Width = 56;
            _measureRangeTo.ValueChanged += OnMeasureRangeChanged;
            panel.Controls.Add(_measureRangeTo);

            panel.Controls.Add(CreateToolButton("复制小节", DuplicateMeasures));
            panel.Controls.Add(CreateSeparator());

            panel.Controls.Add(new Label { Text = "副旋律:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            _secondaryBox.Width = 160;
            _secondaryBox.TextChanged += (s, e) => ApplyMeasureText();
            panel.Controls.Add(_secondaryBox);

            panel.Controls.Add(new Label { Text = "歌词:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            _lyricBox.Width = 160;
            _lyricBox.TextChanged += (s, e) => ApplyMeasureText();
            panel.Controls.Add(_lyricBox);

            panel.Controls.Add(CreateSeparator());
            panel.Controls.Add(CreateToolButton("删除", DeleteLast));
            panel.Controls.Add(CreateToolButton("清空", () => OnClearScore(null, EventArgs.Empty)));
            panel.Controls.Add(CreateToolButton("示例", LoadDemoScore));

            Controls.Add(panel);
        }

        private void BuildCanvas()
        {
            _canvas.Dock = DockStyle.Fill;
            _canvas.MinimumSize = new Size(200, 200);
            Controls.Add(_canvas);
        }

        private void BuildFooter()
        {
            _statusLabel.Dock = DockStyle.Bottom;
            _statusLabel.Height = 28;
            _statusLabel.Padding = new Padding(12, 6, 0, 0);
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(_statusLabel);
        }

        private Button CreateToolButton(string text, Action onClick)
        {
            var button = new Button
            {
                Text = text,
                Width = text.Length > 2 ? 72 : 42,
                Height = 34,
                Margin = new Padding(4, 4, 4, 4)
            };
            button.Click += (s, e) => onClick();
            return button;
        }

        private static Panel CreateSeparator()
        {
            return new Panel
            {
                Width = 2,
                Height = 30,
                BackColor = Color.LightGray,
                Margin = new Padding(8, 8, 8, 8)
            };
        }

        private static ToolStripMenuItem CreateMenuItem(string text, Keys shortcut, EventHandler handler)
        {
            var item = new ToolStripMenuItem(text, null, handler);
            if (shortcut != Keys.None && IsValidMenuShortcut(shortcut))
            {
                item.ShortcutKeys = shortcut;
                item.ShowShortcutKeys = true;
            }

            return item;
        }

        private static bool IsValidMenuShortcut(Keys shortcut)
        {
            return (shortcut & Keys.Modifiers) != Keys.None;
        }

        private void OnFormKeyDown(object sender, KeyEventArgs e)
        {
            if (IsTextInputFocused())
            {
                return;
            }

            if (e.KeyCode == Keys.Escape && _tieModeActive)
            {
                CancelTieMode();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Back)
            {
                DeleteLast();
                e.Handled = true;
            }
        }

        private bool IsTextInputFocused()
        {
            var active = ActiveControl;
            return active is TextBox || active is NumericUpDown;
        }

        private void EnsureMeasures()
        {
            if (_canvas.Score.Measures == null || _canvas.Score.Measures.Count == 0)
            {
                _canvas.Score.Measures = new System.Collections.Generic.List<JianpuMeasure> { new JianpuMeasure() };
            }
        }

        private void AddNote(int pitch)
        {
            var selected = SelectedNote;
            if (selected != null)
            {
                selected.Type = NoteType.Note;
                selected.Pitch = pitch;
                RefreshAfterEdit("已修改选中音符为 " + pitch);
                return;
            }

            var note = ClonePendingNote();
            note.Type = NoteType.Note;
            note.Pitch = pitch;
            InsertMelodyNote(note);
            RefreshAfterEdit("已插入音符 " + pitch);
        }

        private void AddRest()
        {
            var selected = SelectedNote;
            if (selected != null)
            {
                selected.Type = NoteType.Rest;
                selected.Pitch = 0;
                RefreshAfterEdit("已修改选中音符为休止符");
                return;
            }

            var note = ClonePendingNote();
            note.Type = NoteType.Rest;
            note.Pitch = 0;
            InsertMelodyNote(note);
            RefreshAfterEdit("已插入休止符");
        }

        private void InsertMelodyNote(JianpuNote note)
        {
            var measureIndex = _canvas.SelectedMeasureIndex;
            var measure = _canvas.Score.Measures[measureIndex];
            int insertIndex;

            if (_canvas.SelectedInsertIndex >= 0)
            {
                insertIndex = _canvas.SelectedInsertIndex;
            }
            else
            {
                insertIndex = measure.MelodyNotes.Count;
            }

            measure.MelodyNotes.Insert(insertIndex, note);
            ResetPendingModifiers();
            _canvas.SelectNote(measureIndex, insertIndex);
        }

        private void AddMeasure()
        {
            EnsureMeasures();
            _canvas.Score.Measures.Add(new JianpuMeasure());
            SelectMeasure(_canvas.Score.Measures.Count - 1);
            RefreshAfterEdit("已新增第 " + _canvas.Score.Measures.Count + " 小节");
        }

        private JianpuNote ClonePendingNote()
        {
            return new JianpuNote
            {
                Type = _pendingNote.Type,
                Pitch = _pendingNote.Pitch,
                Octave = _pendingNote.Octave,
                Underlines = _pendingNote.Underlines,
                Dashes = _pendingNote.Dashes,
                Dotted = _pendingNote.Dotted
            };
        }

        private void ResetPendingModifiers()
        {
            _pendingNote = CreateDefaultNote();
        }

        private void SetOctave(int octave)
        {
            var selected = SelectedNote;
            if (selected != null)
            {
                selected.Octave = selected.Octave == octave ? 0 : octave;
                RefreshAfterEdit("已修改选中音符八度");
                return;
            }

            _pendingNote.Octave = _pendingNote.Octave == octave ? 0 : octave;
            UpdateStatus("当前八度: " + (_pendingNote.Octave > 0 ? "高音" : _pendingNote.Octave < 0 ? "低音" : "中音"));
        }

        private void ToggleSelectedDotted()
        {
            var selected = SelectedNote;
            if (selected != null)
            {
                selected.Dotted = !selected.Dotted;
                RefreshAfterEdit(selected.Dotted ? "已为选中音符添加附点" : "已移除选中音符附点");
                return;
            }

            _pendingNote.Dotted = !_pendingNote.Dotted;
            UpdateStatus(_pendingNote.Dotted ? "下一音符将带附点" : "已取消附点");
        }

        private void CycleSelectedDuration()
        {
            var selected = SelectedNote;
            if (selected != null)
            {
                selected.Underlines = (selected.Underlines + 1) % 3;
                selected.Dashes = 0;
                RefreshAfterEdit("时值: " + GetDurationLabel(selected.Underlines));
                return;
            }

            _pendingNote.Underlines = (_pendingNote.Underlines + 1) % 3;
            _pendingNote.Dashes = 0;
            UpdateStatus("下一音符时值: " + GetDurationLabel(_pendingNote.Underlines));
        }

        private void CycleSelectedExtension()
        {
            var selected = SelectedNote;
            if (selected != null)
            {
                selected.Dashes = (selected.Dashes + 1) % 4;
                if (selected.Dashes > 0)
                {
                    selected.Underlines = 0;
                }

                RefreshAfterEdit("延长: " + GetExtensionLabel(selected.Dashes));
                return;
            }

            _pendingNote.Dashes = (_pendingNote.Dashes + 1) % 4;
            if (_pendingNote.Dashes > 0)
            {
                _pendingNote.Underlines = 0;
            }
            UpdateStatus("下一音符延长: " + GetExtensionLabel(_pendingNote.Dashes));
        }

        private static string GetDurationLabel(int underlines)
        {
            switch (underlines)
            {
                case 1: return "八分音符";
                case 2: return "十六分音符";
                default: return "四分音符";
            }
        }

        private static string GetExtensionLabel(int dashes)
        {
            switch (dashes)
            {
                case 1: return "延一拍";
                case 2: return "延两拍";
                case 3: return "延三拍";
                default: return "不延长";
            }
        }

        private void ToggleTieMode()
        {
            if (_tieModeActive)
            {
                CancelTieMode();
                return;
            }

            _tieModeActive = true;
            _tieStartMeasureIndex = -1;
            _tieStartNoteIndex = -1;
            if (_tieButton != null)
            {
                _tieButton.BackColor = Color.FromArgb(255, 255, 200);
            }

            UpdateStatus("连音线：请选择起始音符");
        }

        private void CancelTieMode()
        {
            _tieModeActive = false;
            _tieStartMeasureIndex = -1;
            _tieStartNoteIndex = -1;
            if (_tieButton != null)
            {
                _tieButton.BackColor = SystemColors.Control;
            }

            UpdateStatus("已取消连音线");
        }

        private static bool IsNoteAfter(int measureA, int noteA, int measureB, int noteB)
        {
            if (measureA != measureB)
            {
                return measureB > measureA;
            }

            return noteB > noteA;
        }

        private void TryCompleteTie(int endMeasureIndex, int endNoteIndex)
        {
            if (!_tieModeActive)
            {
                return;
            }

            if (_tieStartMeasureIndex < 0)
            {
                _tieStartMeasureIndex = endMeasureIndex;
                _tieStartNoteIndex = endNoteIndex;
                UpdateStatus("连音线：请选择结束音符");
                return;
            }

            if (_tieStartMeasureIndex == endMeasureIndex && _tieStartNoteIndex == endNoteIndex)
            {
                UpdateStatus("连音线：结束音符不能与起始音符相同");
                return;
            }

            if (!IsNoteAfter(_tieStartMeasureIndex, _tieStartNoteIndex, endMeasureIndex, endNoteIndex))
            {
                _tieStartMeasureIndex = endMeasureIndex;
                _tieStartNoteIndex = endNoteIndex;
                UpdateStatus("连音线：结束音符须晚于起始音符，请重新选择结束音符");
                return;
            }

            if (_canvas.Score.Ties == null)
            {
                _canvas.Score.Ties = new List<JianpuTie>();
            }

            _canvas.Score.Ties.Add(new JianpuTie
            {
                StartMeasureIndex = _tieStartMeasureIndex,
                StartNoteIndex = _tieStartNoteIndex,
                EndMeasureIndex = endMeasureIndex,
                EndNoteIndex = endNoteIndex
            });

            CancelTieMode();
            RefreshAfterEdit("已添加连音线");
        }

        private void ApplyMeasureText()
        {
            if (_suppressMeasureTextSync)
            {
                return;
            }

            CurrentMeasure.SecondaryText = _secondaryBox.Text;
            CurrentMeasure.LyricText = _lyricBox.Text;
            _canvas.RefreshScore();
        }

        private void SelectMeasure(int index)
        {
            EnsureMeasures();
            index = Math.Max(0, Math.Min(index, _canvas.Score.Measures.Count - 1));
            _canvas.SelectMeasure(index);
            SyncMeasureTextBoxes(index);
            SyncMeasureRangeControls(index, index);
            _measureSelector.Maximum = _canvas.Score.Measures.Count;
            _measureSelector.Value = index + 1;
            UpdateStatus("当前编辑第 " + (index + 1) + " 小节");
        }

        private void OnMeasureRangeChanged(object sender, EventArgs e)
        {
            if (_suppressMeasureRangeSync)
            {
                return;
            }

            EnsureMeasures();
            var from = (int)_measureRangeFrom.Value - 1;
            var to = (int)_measureRangeTo.Value - 1;
            if (from > to)
            {
                if (sender == _measureRangeFrom)
                {
                    _suppressMeasureRangeSync = true;
                    _measureRangeTo.Value = from + 1;
                    _suppressMeasureRangeSync = false;
                    to = from;
                }
                else
                {
                    _suppressMeasureRangeSync = true;
                    _measureRangeFrom.Value = to + 1;
                    _suppressMeasureRangeSync = false;
                    from = to;
                }
            }

            _canvas.SelectMeasureRange(from, to);
            SyncMeasureTextBoxes(_canvas.SelectedMeasureIndex);
            _measureSelector.Value = _canvas.SelectedMeasureIndex + 1;
            UpdateStatus("已选择第 " + (from + 1) + " 到第 " + (to + 1) + " 小节");
        }

        private void SyncMeasureRangeControls(int fromIndex, int toIndex)
        {
            _suppressMeasureRangeSync = true;
            var measureCount = Math.Max(1, _canvas.Score.Measures.Count);
            _measureRangeFrom.Maximum = measureCount;
            _measureRangeTo.Maximum = measureCount;
            _measureRangeFrom.Value = Math.Max(1, fromIndex + 1);
            _measureRangeTo.Value = Math.Max(1, toIndex + 1);
            _suppressMeasureRangeSync = false;
        }

        private void SyncMeasureRangeFromSelection(IReadOnlyList<int> indices)
        {
            if (indices == null || indices.Count == 0)
            {
                SyncMeasureRangeControls(_canvas.SelectedMeasureIndex, _canvas.SelectedMeasureIndex);
                return;
            }

            SyncMeasureRangeControls(indices.Min(), indices.Max());
        }

        private void DuplicateMeasures()
        {
            EnsureMeasures();
            var indices = _canvas.GetSelectedMeasureIndices().ToList();
            if (indices.Count == 0)
            {
                indices.Add(_canvas.SelectedMeasureIndex);
            }

            var insertAt = indices[indices.Count - 1] + 1;
            var clones = indices.Select(index => MeasureCloneService.Clone(_canvas.Score.Measures[index])).ToList();
            for (var i = 0; i < clones.Count; i++)
            {
                _canvas.Score.Measures.Insert(insertAt + i, clones[i]);
            }

            var duplicatedStart = insertAt;
            var duplicatedIndices = Enumerable.Range(duplicatedStart, indices.Count).ToList();
            _canvas.SetSelectedMeasures(duplicatedIndices, duplicatedStart);
            SyncMeasureTextBoxes(duplicatedStart);
            SyncMeasureRangeControls(duplicatedIndices.Min(), duplicatedIndices.Max());
            _measureSelector.Maximum = _canvas.Score.Measures.Count;
            _measureSelector.Value = duplicatedStart + 1;
            RefreshAfterEdit("已复制 " + indices.Count + " 个小节到第 " + (duplicatedStart + 1) + " 小节后");
        }

        private void SyncMeasureTextBoxes(int index)
        {
            _suppressMeasureTextSync = true;
            _secondaryBox.Text = _canvas.Score.Measures[index].SecondaryText ?? string.Empty;
            _lyricBox.Text = _canvas.Score.Measures[index].LyricText ?? string.Empty;
            _suppressMeasureTextSync = false;
        }

        private void OnCanvasSelectionChanged(object sender, ScoreSelectionChangedEventArgs e)
        {
            if (e.MeasureIndex < 0)
            {
                return;
            }

            SyncMeasureTextBoxes(e.MeasureIndex);
            SyncMeasureRangeFromSelection(e.SelectedMeasureIndices);
            _measureSelector.Maximum = _canvas.Score.Measures.Count;
            _measureSelector.Value = e.MeasureIndex + 1;

            if (e.HasNoteSelected && _tieModeActive)
            {
                TryCompleteTie(e.MeasureIndex, e.NoteIndex);
                return;
            }

            if (e.SelectedMeasureIndices != null && e.SelectedMeasureIndices.Count > 1)
            {
                UpdateStatus("已选择第 " + (e.SelectedMeasureIndices.Min() + 1) + " 到第 " + (e.SelectedMeasureIndices.Max() + 1) + " 小节，可点击「复制小节」");
            }
            else if (e.HasNoteSelected)
            {
                UpdateStatus("已选中第 " + (e.MeasureIndex + 1) + " 小节第 " + (e.NoteIndex + 1) + " 个音符，可用上方按钮修改");
            }
            else if (e.HasGapSelected)
            {
                UpdateStatus("已选中第 " + (e.MeasureIndex + 1) + " 小节第 " + (e.InsertIndex + 1) + " 个插入位置，可用上方按钮插入音符");
            }
            else
            {
                UpdateStatus("当前编辑第 " + (e.MeasureIndex + 1) + " 小节，点击副旋律/歌词行可直接编辑文字");
            }
        }

        private void OnCanvasMeasureTextEdited(object sender, EventArgs e)
        {
            if (_canvas.SelectedMeasureIndex >= 0)
            {
                SyncMeasureTextBoxes(_canvas.SelectedMeasureIndex);
            }

            RefreshAfterEdit("已更新小节文字");
        }

        private void DeleteLast()
        {
            var measure = CurrentMeasure;
            if (_canvas.SelectedNoteIndex >= 0 && _canvas.SelectedNoteIndex < measure.MelodyNotes.Count)
            {
                measure.MelodyNotes.RemoveAt(_canvas.SelectedNoteIndex);
                _canvas.ClearMelodySelection();
                RefreshAfterEdit("已删除选中音符");
                return;
            }

            if (measure.MelodyNotes.Count > 0)
            {
                measure.MelodyNotes.RemoveAt(measure.MelodyNotes.Count - 1);
                _canvas.ClearMelodySelection();
                RefreshAfterEdit("已删除当前小节最后一个音符");
                return;
            }

            if (_canvas.Score.Measures.Count > 1)
            {
                _canvas.Score.Measures.RemoveAt(_canvas.SelectedMeasureIndex);
                SelectMeasure(Math.Max(0, _canvas.SelectedMeasureIndex - 1));
                RefreshAfterEdit("已删除空小节");
            }
        }

        private void RefreshAfterEdit(string message)
        {
            var measureCount = Math.Max(1, _canvas.Score.Measures.Count);
            _measureSelector.Maximum = measureCount;
            _measureRangeFrom.Maximum = measureCount;
            _measureRangeTo.Maximum = measureCount;
            _canvas.RefreshScore();
            UpdateStatus(message);
        }

        private void OnClearScore(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定清空当前谱面吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            _canvas.Score.Measures = new System.Collections.Generic.List<JianpuMeasure> { new JianpuMeasure() };
            SelectMeasure(0);
            RefreshAfterEdit("谱面已清空");
        }

        private void OnNewScore(object sender, EventArgs e)
        {
            CancelTieMode();
            _canvas.Score = new JianpuScore();
            _titleBox.Text = _canvas.Score.Title;
            _keyBox.Text = _canvas.Score.KeySignature;
            _tempoBox.Text = _canvas.Score.Tempo;
            _composerBox.Text = _canvas.Score.Composer;
            _currentFilePath = null;
            Text = "简谱编辑器";
            SelectMeasure(0);
            RefreshAfterEdit("已新建谱面");
        }

        private void OnOpenScore(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog
            {
                Filter = "简谱文件 (*.jianpu)|*.jianpu|JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*"
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                var score = ScoreFileService.Load(dialog.FileName);
                _canvas.Score = score;
                _titleBox.Text = score.Title;
                _keyBox.Text = score.KeySignature;
                _tempoBox.Text = score.Tempo;
                _composerBox.Text = score.Composer;
                _currentFilePath = dialog.FileName;
                Text = "简谱编辑器 - " + Path.GetFileName(dialog.FileName);
                SelectMeasure(0);
                RefreshAfterEdit("已打开: " + dialog.FileName);
            }
        }

        private void OnSaveScore(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentFilePath))
            {
                OnSaveScoreAs(sender, e);
                return;
            }

            ScoreFileService.Save(_canvas.Score, _currentFilePath);
            UpdateStatus("已保存: " + _currentFilePath);
        }

        private void OnSaveScoreAs(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog
            {
                Filter = "简谱文件 (*.jianpu)|*.jianpu|JSON 文件 (*.json)|*.json",
                FileName = string.IsNullOrWhiteSpace(_canvas.Score.Title) ? "新乐曲.jianpu" : _canvas.Score.Title + ".jianpu"
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                ScoreFileService.Save(_canvas.Score, dialog.FileName);
                _currentFilePath = dialog.FileName;
                Text = "简谱编辑器 - " + Path.GetFileName(dialog.FileName);
                UpdateStatus("已保存: " + dialog.FileName);
            }
        }

        private void OnExportPdf(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog
            {
                Filter = "PDF 文件 (*.pdf)|*.pdf",
                FileName = string.IsNullOrWhiteSpace(_canvas.Score.Title) ? "简谱.pdf" : _canvas.Score.Title + ".pdf"
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    PdfExportService.Export(_canvas.Score, dialog.FileName, Math.Max(Width - 40, 900));
                    UpdateStatus("PDF 已导出: " + dialog.FileName);
                    MessageBox.Show("PDF 导出成功。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("PDF 导出失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadDemoScore()
        {
            _canvas.Score = new JianpuScore
            {
                Title = "欢乐颂",
                KeySignature = "1=C",
                Tempo = "中速",
                Composer = "贝多芬",
                Measures = new System.Collections.Generic.List<JianpuMeasure>
                {
                    new JianpuMeasure
                    {
                        MelodyNotes = new System.Collections.Generic.List<JianpuNote>
                        {
                            new JianpuNote { Pitch = 3 }, new JianpuNote { Pitch = 3 },
                            new JianpuNote { Pitch = 4 }, new JianpuNote { Pitch = 5 }
                        },
                        SecondaryText = "主题 A",
                        LyricText = "欢乐女神"
                    },
                    new JianpuMeasure
                    {
                        MelodyNotes = new System.Collections.Generic.List<JianpuNote>
                        {
                            new JianpuNote { Pitch = 5 }, new JianpuNote { Pitch = 4 },
                            new JianpuNote { Pitch = 3 }, new JianpuNote { Pitch = 2 }
                        },
                        SecondaryText = "应答",
                        LyricText = "圣洁美丽"
                    },
                    new JianpuMeasure
                    {
                        MelodyNotes = new System.Collections.Generic.List<JianpuNote>
                        {
                            new JianpuNote { Pitch = 1, Dashes = 1 }
                        },
                        SecondaryText = "延长",
                        LyricText = "灿烂光芒"
                    },
                    new JianpuMeasure
                    {
                        MelodyNotes = new System.Collections.Generic.List<JianpuNote>
                        {
                            new JianpuNote { Pitch = 1 }, new JianpuNote { Pitch = 2 },
                            new JianpuNote { Pitch = 3 }, new JianpuNote { Pitch = 2, Dashes = 1 }
                        },
                        SecondaryText = "主题 B",
                        LyricText = "照大地"
                    },
                    new JianpuMeasure
                    {
                        MelodyNotes = new System.Collections.Generic.List<JianpuNote>
                        {
                            new JianpuNote { Pitch = 3, Dotted = true, Underlines = 1 },
                            new JianpuNote { Pitch = 3, Underlines = 1 }
                        },
                        SecondaryText = "收束",
                        LyricText = "我们欢聚"
                    }
                }
            };

            _titleBox.Text = _canvas.Score.Title;
            _keyBox.Text = _canvas.Score.KeySignature;
            _tempoBox.Text = _canvas.Score.Tempo;
            _composerBox.Text = _canvas.Score.Composer;
            SelectMeasure(0);
            RefreshAfterEdit("已加载示例谱面《欢乐颂》");
        }

        private void UpdateStatus(string message)
        {
            _statusLabel.Text = message;
        }
    }
}