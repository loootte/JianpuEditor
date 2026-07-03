using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using JianpuEditor.Controls;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Glue;
using JianpuEditor.Rendering;
using JianpuEditor.Services;
using JianpuEditor.ViewModels;

namespace JianpuEditor
{
    public sealed class MainForm : Form
    {
        private readonly MainViewModel _viewModel;
        private readonly ScoreCanvasGlue _glue;
        private readonly MainFormViewBinder _binder;
        private readonly ScoreCanvas _canvas = new ScoreCanvas();
        private readonly TextBox _titleBox = new TextBox();
        private readonly TextBox _keyBox = new TextBox();
        private readonly TextBox _tempoBox = new TextBox();
        private readonly NumericUpDown _bpmBox = new NumericUpDown();
        private readonly TextBox _composerBox = new TextBox();
        private readonly TextBox _chordBox = new TextBox();
        private readonly TextBox _lyricBox = new TextBox();
        private readonly NumericUpDown _measureSelector = new NumericUpDown();
        private readonly NumericUpDown _measureRangeFrom = new NumericUpDown();
        private readonly NumericUpDown _measureRangeTo = new NumericUpDown();
        private readonly Label _statusLabel = new Label();
        private Button _tieButton;
        private Button _playButton;
        private Button _stopButton;
        private const int HeaderPanelHeight = 88;
        private const int DefaultToolbarHeight = 160;

        private TableLayoutPanel _topChrome;
        private MenuStrip _menuStrip;
        private ContextMenuStrip _sampleLibraryMenu;
        private ToolStripMenuItem _darkModeMenuItem;

        public MainForm(MainViewModel viewModel, IAppMessenger messenger)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            Text = "简谱编辑器";
            Width = 1280;
            Height = 820;
            MinimumSize = new Size(960, 640);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei", 9f);
            KeyPreview = true;
            KeyDown += OnFormKeyDown;

            SuspendLayout();
            BuildFooter();
            BuildMenuStrip();
            BuildTopChrome();
            BuildCanvas();
            ResumeLayout(true);
            Load += OnFormLoad;
            Shown += OnFormShown;
            Resize += OnFormResize;

            _binder = new MainFormViewBinder(
                _viewModel,
                this,
                _titleBox,
                _keyBox,
                _tempoBox,
                _bpmBox,
                _composerBox,
                _chordBox,
                _lyricBox,
                _measureSelector,
                _measureRangeFrom,
                _measureRangeTo,
                _statusLabel,
                _tieButton,
                _playButton,
                _stopButton);

            _glue = new ScoreCanvasGlue(_viewModel, _canvas, messenger);

            _canvas.SelectionChanged += OnCanvasSelectionChanged;
            _canvas.MeasureTextEdited += OnCanvasMeasureTextEdited;
            _canvas.ChordMarkersChanged += OnCanvasChordMarkersChanged;
            _canvas.PlaybackSeeked += OnCanvasPlaybackSeeked;

            _viewModel.RequestOpenScore += (s, e) => OnOpenScore(s, e);
            _viewModel.RequestSaveScore += (s, e) => OnSaveScore(s, e);
            _viewModel.RequestSaveAsScore += (s, e) => OnSaveScoreAs(s, e);
            _viewModel.RequestExportPdf += (s, e) => OnExportPdf(s, e);
            _viewModel.RequestExportMidi += (s, e) => OnExportMidi(s, e);
            _viewModel.RequestTransposeDialog += (s, e) => ShowTransposeDialog();

            FormClosed += OnFormClosed;
            AppLog.Info("简谱编辑器启动");

            var demoResult = _viewModel.SampleLibrary.LoadDemoScore();
            _glue.ApplyEditResult(demoResult);
            _glue.ResetPlaybackHead();
            _binder.SyncHeaderFromDocument();
            _binder.SyncFromViewModels();
            _viewModel.SetStatus("就绪 - 点击音符修改，副旋律行可添加/拖动和弦标识，点击歌词行编辑文字");
            WinFormsThemeApplier.Apply(this, _canvas);
            EnsureMenuStripVisible();
        }

        private void BuildMenuStrip()
        {
            _menuStrip = new MenuStrip();
            PopulateMenuStrip(_menuStrip);
            MainMenuStrip = _menuStrip;
        }

        private void BuildTopChrome()
        {
            var headerPanel = CreateHeaderPanel();
            var toolbarPanel = CreateToolbarPanel();

            _topChrome = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = false,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            _topChrome.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _topChrome.RowStyles.Add(new RowStyle(SizeType.Absolute, HeaderPanelHeight));
            _topChrome.RowStyles.Add(new RowStyle(SizeType.Absolute, DefaultToolbarHeight));
            _topChrome.Controls.Add(headerPanel, 0, 0);
            _topChrome.Controls.Add(toolbarPanel, 0, 1);
            Controls.Add(_topChrome);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            AdjustTopChromeHeight();
        }

        private void OnFormShown(object sender, EventArgs e)
        {
            EnsureMenuStripVisible();
            AdjustTopChromeHeight();
            _canvas.ResetViewport();
        }

        private void EnsureMenuStripVisible()
        {
            if (_menuStrip == null)
            {
                return;
            }

            _menuStrip.Visible = true;
            if (MainMenuStrip != _menuStrip)
            {
                MainMenuStrip = _menuStrip;
            }

            WinFormsThemeApplier.ApplyMenuStrip(_menuStrip);
        }

        private void OnFormResize(object sender, EventArgs e)
        {
            AdjustTopChromeHeight();
        }

        private void AdjustTopChromeHeight()
        {
            if (_topChrome == null)
            {
                return;
            }

            var toolbar = _topChrome.GetControlFromPosition(0, 1) as FlowLayoutPanel;
            var width = Math.Max(ClientSize.Width, 400);
            var toolbarHeight = MeasureToolbarHeight(toolbar, width);
            _topChrome.RowStyles[1] = new RowStyle(SizeType.Absolute, toolbarHeight);

            var chromeHeight = HeaderPanelHeight + toolbarHeight + _topChrome.Padding.Vertical;
            if (_topChrome.Height != chromeHeight)
            {
                _topChrome.Height = chromeHeight;
            }

            _topChrome.MinimumSize = new Size(0, chromeHeight);
            PerformLayout();
        }

        private static int MeasureToolbarHeight(FlowLayoutPanel toolbar, int width)
        {
            if (toolbar == null)
            {
                return DefaultToolbarHeight;
            }

            toolbar.MaximumSize = new Size(width, 0);
            toolbar.Width = width;
            toolbar.PerformLayout();
            var height = toolbar.GetPreferredSize(new Size(width, 0)).Height;
            return Math.Max(height + 4, 80);
        }

        private void PopulateMenuStrip(MenuStrip menu)
        {
            var fileMenu = new ToolStripMenuItem("文件");
            fileMenu.DropDownItems.Add(CreateMenuItem("新建", Keys.Control | Keys.N, (s, e) => OnNewScore(s, e)));
            fileMenu.DropDownItems.Add(CreateMenuItem("打开...", Keys.Control | Keys.O, OnOpenScore));
            fileMenu.DropDownItems.Add(CreateMenuItem("保存", Keys.Control | Keys.S, OnSaveScore));
            fileMenu.DropDownItems.Add(CreateMenuItem("另存为...", Keys.Control | Keys.Shift | Keys.S, OnSaveScoreAs));
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(CreateMenuItem("导出 PDF...", Keys.Control | Keys.P, OnExportPdf));
            fileMenu.DropDownItems.Add(CreateMenuItem("导出 MIDI...", Keys.None, OnExportMidi));
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            var sampleMenu = new ToolStripMenuItem("示例曲库");
            sampleMenu.DropDownOpening += (s, e) => PopulateSampleLibraryMenu(sampleMenu.DropDownItems);
            PopulateSampleLibraryMenu(sampleMenu.DropDownItems);
            fileMenu.DropDownItems.Add(sampleMenu);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(CreateMenuItem("退出", Keys.None, (s, e) => Close()));

            var editMenu = new ToolStripMenuItem("编辑");
            editMenu.DropDownItems.Add(CreateMenuItem("撤销最后一个音符", Keys.Control | Keys.Z, (s, e) => ExecuteDelete()));
            editMenu.DropDownItems.Add(CreateMenuItem("新增小节", Keys.None, (s, e) => ExecuteAddMeasure()));
            editMenu.DropDownItems.Add(CreateMenuItem("复制小节", Keys.None, (s, e) => ExecuteDuplicateMeasures()));
            editMenu.DropDownItems.Add(CreateMenuItem("和弦转调...", Keys.None, (s, e) => ShowTransposeDialog()));
            editMenu.DropDownItems.Add(CreateMenuItem("清空谱面", Keys.None, OnClearScore));

            var viewMenu = new ToolStripMenuItem("视图");
            _darkModeMenuItem = new ToolStripMenuItem("深色模式")
            {
                CheckOnClick = true,
                Checked = AppTheme.IsDarkMode
            };
            _darkModeMenuItem.CheckedChanged += OnDarkModeToggled;
            viewMenu.DropDownItems.Add(_darkModeMenuItem);

            menu.Items.Add(fileMenu);
            menu.Items.Add(editMenu);
            menu.Items.Add(viewMenu);
        }

        private TableLayoutPanel CreateHeaderPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = HeaderPanelHeight,
                Padding = new Padding(12, 8, 12, 8),
                ColumnCount = 10
            };

            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _titleBox.Dock = DockStyle.Fill;
            _keyBox.Dock = DockStyle.Fill;
            _tempoBox.Dock = DockStyle.Fill;
            _composerBox.Dock = DockStyle.Fill;
            _bpmBox.Dock = DockStyle.Fill;
            _bpmBox.Minimum = 30;
            _bpmBox.Maximum = 300;
            _bpmBox.Value = 120;

            panel.Controls.Add(new Label { Text = "标题", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            panel.Controls.Add(_titleBox, 1, 0);
            panel.Controls.Add(new Label { Text = "调号", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 2, 0);
            panel.Controls.Add(_keyBox, 3, 0);
            panel.Controls.Add(new Label { Text = "速度", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 4, 0);
            panel.Controls.Add(_tempoBox, 5, 0);
            panel.Controls.Add(new Label { Text = "BPM", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 6, 0);
            panel.Controls.Add(_bpmBox, 7, 0);
            panel.Controls.Add(new Label { Text = "作曲", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 8, 0);
            panel.Controls.Add(_composerBox, 9, 0);

            return panel;
        }

        private FlowLayoutPanel CreateToolbarPanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 8, 12, 8),
                WrapContents = true,
                AutoScroll = false
            };

            panel.Controls.Add(CreateToolButton("打开", () => _viewModel.OpenScoreCommand.Execute(null)));
            panel.Controls.Add(CreateToolButton("保存", () => _viewModel.SaveScoreCommand.Execute(null)));
            panel.Controls.Add(CreateToolButton("导出PDF", () => _viewModel.ExportPdfCommand.Execute(null)));
            panel.Controls.Add(CreateToolButton("导出MIDI", () => _viewModel.ExportMidiCommand.Execute(null)));
            panel.Controls.Add(CreateSeparator());

            _playButton = CreateToolButton("播放", OnPlayScore);
            _stopButton = CreateToolButton("停止", OnStopPlayback);
            _stopButton.Enabled = false;
            panel.Controls.Add(_playButton);
            panel.Controls.Add(_stopButton);
            panel.Controls.Add(CreateSeparator());

            panel.Controls.Add(new Label { Text = "音符:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            for (var pitch = 1; pitch <= 7; pitch++)
            {
                var p = pitch;
                panel.Controls.Add(CreateToolButton(p.ToString(), () => ExecuteEdit(() => _viewModel.NoteEditor.AddNote(p))));
            }

            panel.Controls.Add(CreateToolButton("0", () => ExecuteEdit(() => _viewModel.NoteEditor.AddRest())));
            panel.Controls.Add(CreateToolButton("新小节", ExecuteAddMeasure));
            panel.Controls.Add(CreateSeparator());

            panel.Controls.Add(new Label { Text = "修饰:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            panel.Controls.Add(CreateToolButton("高音·", () => _viewModel.NoteEditor.SetOctaveUpCommand.Execute(null)));
            panel.Controls.Add(CreateToolButton("低音·", () => _viewModel.NoteEditor.SetOctaveDownCommand.Execute(null)));
            panel.Controls.Add(CreateToolButton("附点", () => ExecuteEdit(() => _viewModel.NoteEditor.ToggleDotted())));
            panel.Controls.Add(CreateToolButton("增时线", () => ExecuteEdit(() => _viewModel.NoteEditor.CycleExtension())));
            panel.Controls.Add(CreateToolButton("减时线", () => ExecuteEdit(() => _viewModel.NoteEditor.CycleDuration())));
            _tieButton = CreateToolButton("连音线", () => _viewModel.TieEditor.ToggleTieModeCommand.Execute(null));
            panel.Controls.Add(_tieButton);
            panel.Controls.Add(CreateSeparator());

            panel.Controls.Add(new Label { Text = "当前小节:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            _measureSelector.Minimum = 1;
            _measureSelector.Maximum = 1;
            _measureSelector.Width = 56;
            _measureSelector.ValueChanged += OnMeasureSelectorChanged;
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

            panel.Controls.Add(CreateToolButton("复制小节", ExecuteDuplicateMeasures));
            panel.Controls.Add(CreateSeparator());

            panel.Controls.Add(new Label { Text = "和弦:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            _chordBox.Width = 120;
            _chordBox.TextChanged += OnChordTextChanged;
            panel.Controls.Add(_chordBox);
            panel.Controls.Add(CreateToolButton("添加和弦", () => ExecuteEdit(() => _viewModel.ChordEditor.AddChordMarker())));
            panel.Controls.Add(CreateToolButton("转调", ShowTransposeDialog));

            panel.Controls.Add(new Label { Text = "歌词:", AutoSize = true, Margin = new Padding(0, 10, 6, 0) });
            _lyricBox.Width = 160;
            _lyricBox.TextChanged += OnLyricTextChanged;
            panel.Controls.Add(_lyricBox);

            panel.Controls.Add(CreateSeparator());
            panel.Controls.Add(CreateToolButton("删除", ExecuteDelete));
            panel.Controls.Add(CreateToolButton("清空", () => OnClearScore(null, EventArgs.Empty)));
            _sampleLibraryMenu = new ContextMenuStrip();
            _sampleLibraryMenu.Opening += (s, e) => PopulateSampleLibraryMenu(_sampleLibraryMenu.Items);
            var sampleButton = CreateToolButton("曲库", () => { });
            sampleButton.Click += (s, e) => _sampleLibraryMenu.Show(sampleButton, new Point(0, sampleButton.Height));
            panel.Controls.Add(sampleButton);

            return panel;
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
                BackColor = AppTheme.Separator,
                Margin = new Padding(8, 8, 8, 8)
            };
        }

        private void OnDarkModeToggled(object sender, EventArgs e)
        {
            AppTheme.SetDarkMode(_darkModeMenuItem.Checked);
            WinFormsThemeApplier.Apply(this, _canvas);
            _binder.SyncFromViewModels();
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

        private void ExecuteEdit(Func<ScoreEditResult> action)
        {
            var result = action();
            _glue.ApplyEditResult(result);
            _binder.SyncFromViewModels();
        }

        private void ExecuteAddMeasure()
        {
            var result = _viewModel.MeasureNavigation.AddMeasure();
            _glue.ApplyEditResult(result);
            _binder.SyncFromViewModels();
        }

        private void ExecuteDuplicateMeasures()
        {
            var result = _viewModel.MeasureNavigation.DuplicateMeasures();
            _glue.ApplyEditResult(result);
            _binder.SyncFromViewModels();
        }

        private void ExecuteDelete()
        {
            var result = _viewModel.ScoreEditor.Delete();
            _glue.ApplyEditResult(result);
            _binder.SyncFromViewModels();
        }

        private void OnFormKeyDown(object sender, KeyEventArgs e)
        {
            if (IsTextInputFocused())
            {
                return;
            }

            if (e.KeyCode == Keys.Escape && _viewModel.TieEditor.IsTieModeActive)
            {
                _viewModel.TieEditor.CancelTieModeCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
            {
                ExecuteDelete();
                e.Handled = true;
            }
        }

        private bool IsTextInputFocused()
        {
            var active = ActiveControl;
            return active is TextBox || active is NumericUpDown;
        }

        private void OnMeasureSelectorChanged(object sender, EventArgs e)
        {
            if (_binder.SuppressMeasureSelectorSync)
            {
                return;
            }

            var result = _viewModel.MeasureNavigation.SelectMeasure((int)_measureSelector.Value - 1);
            _glue.ApplyEditResult(result);
            _binder.SyncFromViewModels();
        }

        private void OnMeasureRangeChanged(object sender, EventArgs e)
        {
            if (_binder.SuppressMeasureRangeSync)
            {
                return;
            }

            var normalized = _viewModel.MeasureNavigation.NormalizeMeasureRange(
                (int)_measureRangeFrom.Value,
                (int)_measureRangeTo.Value,
                sender == _measureRangeFrom);
            var result = _viewModel.MeasureNavigation.ApplyMeasureRange(normalized.fromIndex, normalized.toIndex);
            _glue.ApplyEditResult(result);
            _binder.SyncFromViewModels();
        }

        private void OnChordTextChanged(object sender, EventArgs e)
        {
            if (_binder.SuppressMeasureTextSync)
            {
                return;
            }

            _viewModel.ChordEditor.SelectedChordText = _chordBox.Text;
            _canvas.UpdateSelectedChordText(_chordBox.Text);
        }

        private void OnLyricTextChanged(object sender, EventArgs e)
        {
            if (_binder.SuppressMeasureTextSync)
            {
                return;
            }

            _viewModel.MeasureContent.CurrentLyricText = _lyricBox.Text;
        }

        private void OnCanvasSelectionChanged(object sender, ScoreSelectionChangedEventArgs e)
        {
            if (e.MeasureIndex < 0)
            {
                return;
            }

            _viewModel.HandleSelectionChanged(ScoreSelectionMapper.FromCanvas(e));
            _binder.SyncFromViewModels();
        }

        private void OnCanvasChordMarkersChanged(object sender, EventArgs e)
        {
            _viewModel.NotifyScoreEdited("已更新和弦标识", markDirty: true);
            _viewModel.ChordEditor.SyncFromSelection();
            _binder.SyncMeasureTextBoxes();
        }

        private void OnCanvasMeasureTextEdited(object sender, EventArgs e)
        {
            if (_canvas.SelectedMeasureIndex >= 0)
            {
                var result = _viewModel.MeasureContent.NotifyInlineLyricEdited(_canvas.SelectedMeasureIndex);
                _glue.ApplyEditResult(result);
                _binder.SyncFromViewModels();
            }
        }

        private void OnClearScore(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定清空当前谱面吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            _viewModel.Playback.Stop();
            _viewModel.TieEditor.CancelTieMode();
            var result = _viewModel.ScoreEditor.ClearScore();
            _glue.ApplyEditResult(result);
            _glue.ResetPlaybackHead();
            _binder.SyncFromViewModels();
        }

        private void OnNewScore(object sender, EventArgs e)
        {
            _viewModel.NewScoreCommand.Execute(null);
            _glue.ApplyEditResult(new ScoreEditResult { Changed = true, SelectMeasureIndex = 0 });
            _glue.ResetPlaybackHead();
            _binder.SyncHeaderFromDocument();
            _binder.SyncFromViewModels();
        }

        private void OnOpenScore(object sender, EventArgs e)
        {
            _viewModel.Playback.Stop();
            _viewModel.TieEditor.CancelTieMode();
            using (var dialog = new OpenFileDialog
            {
                Filter = "简谱文件 (*.jianpu)|*.jianpu|JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*"
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                _viewModel.Document.LoadFromFile(dialog.FileName);
                _glue.ApplyEditResult(new ScoreEditResult { Changed = true, SelectMeasureIndex = 0 });
                _glue.ResetPlaybackHead();
                _binder.SyncHeaderFromDocument();
                _binder.SyncFromViewModels();
                _viewModel.SetStatus("已打开: " + dialog.FileName);
            }
        }

        private void OnSaveScore(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_viewModel.Document.CurrentFilePath))
            {
                OnSaveScoreAs(sender, e);
                return;
            }

            _viewModel.Document.SaveToFile(_viewModel.Document.CurrentFilePath);
            _viewModel.SetStatus("已保存: " + _viewModel.Document.CurrentFilePath);
        }

        private void OnSaveScoreAs(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog
            {
                Filter = "简谱文件 (*.jianpu)|*.jianpu|JSON 文件 (*.json)|*.json",
                FileName = string.IsNullOrWhiteSpace(_viewModel.Document.Title)
                    ? "新乐曲.jianpu"
                    : _viewModel.Document.Title + ".jianpu"
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                _viewModel.Document.SaveToFile(dialog.FileName);
                _binder.SyncHeaderFromDocument();
                _viewModel.SetStatus("已保存: " + dialog.FileName);
            }
        }

        private void OnExportPdf(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog
            {
                Filter = "PDF 文件 (*.pdf)|*.pdf",
                FileName = string.IsNullOrWhiteSpace(_viewModel.Document.Title)
                    ? "简谱.pdf"
                    : _viewModel.Document.Title + ".pdf"
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    _viewModel.ExportPdf(dialog.FileName, Math.Max(Width - 40, 900));
                    MessageBox.Show("PDF 导出成功。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnExportMidi(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog
            {
                Filter = "MIDI 文件 (*.mid)|*.mid",
                FileName = string.IsNullOrWhiteSpace(_viewModel.Document.Title)
                    ? "简谱.mid"
                    : _viewModel.Document.Title + ".mid"
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    _viewModel.ExportMidi(dialog.FileName);
                    MessageBox.Show("MIDI 导出成功。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void PopulateSampleLibraryMenu(ToolStripItemCollection items)
        {
            items.Clear();
            items.Add(CreateMenuItem("欢乐颂（内置）", Keys.None, (s, e) => LoadDemoScore()));
            items.Add(new ToolStripSeparator());

            _viewModel.SampleLibrary.RefreshSamples();
            if (!_viewModel.SampleLibrary.HasSamples)
            {
                items.Add(new ToolStripMenuItem("(sample 目录暂无文件)") { Enabled = false });
                return;
            }

            foreach (var sampleFile in _viewModel.SampleLibrary.Samples)
            {
                var path = sampleFile;
                var label = _viewModel.SampleLibrary.GetDisplayName(sampleFile);
                items.Add(CreateMenuItem(label, Keys.None, (s, e) => LoadSampleScore(path)));
            }
        }

        private void LoadSampleScore(string path)
        {
            try
            {
                _viewModel.Playback.Stop();
                _viewModel.TieEditor.CancelTieMode();
                var result = _viewModel.SampleLibrary.LoadSample(path);
                Text = _viewModel.SampleLibrary.BuildWindowTitle(path);
                _glue.ApplyEditResult(result);
                _glue.ResetPlaybackHead();
                _binder.SyncHeaderFromDocument();
                _binder.SyncFromViewModels();
            }
            catch (Exception ex)
            {
                AppLog.Exception("加载示例曲谱失败: " + path, ex);
                MessageBox.Show("加载示例曲谱失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDemoScore()
        {
            _viewModel.Playback.Stop();
            _viewModel.TieEditor.CancelTieMode();
            var result = _viewModel.SampleLibrary.LoadDemoScore();
            _glue.ApplyEditResult(result);
            _glue.ResetPlaybackHead();
            _binder.SyncHeaderFromDocument();
            _binder.SyncFromViewModels();
        }

        private void OnPlayScore()
        {
            try
            {
                var startQuarter = _canvas.PlaybackPositionQuarter;
                _viewModel.Playback.Play(startQuarter);
                _canvas.SetPlaybackPosition(startQuarter, showHead: true, ensureVisible: true);
            }
            catch (Exception ex)
            {
                AppLog.Exception("用户点击播放失败", ex);
                ShowPlaybackError("播放失败", ex);
            }
        }

        private void OnStopPlayback()
        {
            _viewModel.Playback.StopCommand.Execute(null);
        }

        private void OnCanvasPlaybackSeeked(double quarterBeat)
        {
            try
            {
                _viewModel.Playback.Seek(quarterBeat);
                if (!_viewModel.Playback.IsPlaying)
                {
                    _canvas.SetPlaybackPosition(quarterBeat, showHead: true, ensureVisible: true);
                }
            }
            catch (Exception ex)
            {
                AppLog.Exception("拖动播放进度失败", ex);
                ShowPlaybackError("播放跳转失败", ex);
            }
        }

        private static void ShowPlaybackError(string title, Exception ex)
        {
            var message = ex == null
                ? title
                : title + Environment.NewLine + Environment.NewLine +
                  ex.Message + Environment.NewLine + Environment.NewLine +
                  "详细日志:" + Environment.NewLine + AppLog.LogFilePath;
            MessageBox.Show(message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            AppLog.Info("简谱编辑器退出");
            _glue.Dispose();
            _binder.Dispose();
            _viewModel.Dispose();
        }

        private void ShowTransposeDialog()
        {
            using (var dialog = new Form())
            {
                dialog.Text = "和弦转调";
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.ClientSize = new Size(380, 156);
                dialog.Font = Font;

                var sourceLabel = new Label { Text = "当前调号：", Location = new Point(16, 18), AutoSize = true };
                var sourceValue = new Label
                {
                    Text = _viewModel.Document.KeySignature ?? "1=C",
                    Location = new Point(108, 18),
                    AutoSize = true
                };
                var targetLabel = new Label { Text = "目标调号：", Location = new Point(16, 54), AutoSize = true };
                var targetBox = new TextBox
                {
                    Location = new Point(108, 50),
                    Width = 240,
                    Text = _viewModel.Document.KeySignature ?? "1=C"
                };
                var hintLabel = new Label
                {
                    Text = "支持格式：G、1=G、F#、Bb、D大调。仅转调副旋律中的和弦标识。",
                    Location = new Point(16, 84),
                    Size = new Size(348, 32),
                    ForeColor = Color.DimGray
                };
                var okButton = new Button { Text = "转换", DialogResult = DialogResult.OK, Location = new Point(192, 118), Width = 76 };
                var cancelButton = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(276, 118), Width = 76 };

                dialog.Controls.Add(sourceLabel);
                dialog.Controls.Add(sourceValue);
                dialog.Controls.Add(targetLabel);
                dialog.Controls.Add(targetBox);
                dialog.Controls.Add(hintLabel);
                dialog.Controls.Add(okButton);
                dialog.Controls.Add(cancelButton);
                dialog.AcceptButton = okButton;
                dialog.CancelButton = cancelButton;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                var targetKey = targetBox.Text?.Trim();
                if (string.IsNullOrEmpty(targetKey))
                {
                    MessageBox.Show("请输入目标调号。", "转调", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var result = _viewModel.ChordEditor.TransposeChords(targetKey);
                if (!result.Changed && !string.IsNullOrEmpty(result.Message))
                {
                    MessageBox.Show(result.Message, "转调失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _glue.ApplyEditResult(result);
                _binder.SyncHeaderFromDocument();
                _binder.SyncFromViewModels();
            }
        }
    }
}
