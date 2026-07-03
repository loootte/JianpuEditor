using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using JianpuEditor.Controls;
using JianpuEditor.Core.Abstractions;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Glue;
using JianpuEditor.Rendering;
using JianpuEditor.Services;
using JianpuEditor.Models;
using JianpuEditor.ViewModels;
using JianpuEditor.Views;

namespace JianpuEditor
{
    public sealed partial class MainForm : Form, IView
    {
        private readonly MainViewModel _viewModel;
        private readonly IAppMessenger _messenger;
        private readonly ILayoutService _layoutService;
        private readonly IScoreUndoService _undoService;
        private ScoreCanvasGlue _glue;
        private MainFormViewBinder _binder;
        private MainFormLayoutContext _layoutContext;
        private TableLayoutPanel _mainLayout;
        private TableLayoutPanel _chromeLayout;
        private readonly ScoreCanvas _canvas = new ScoreCanvas();
        private readonly TextBox _chordBox = new TextBox();
        private readonly NumericUpDown _measureSelector = new NumericUpDown();
        private readonly NumericUpDown _measureRangeFrom = new NumericUpDown();
        private readonly NumericUpDown _measureRangeTo = new NumericUpDown();
        private readonly Label _statusLabel = new Label();
        private Button _tieButton;
        private Button _playButton;
        private Button _stopButton;
        private MenuStrip _menuStrip;
        private ContextMenuStrip _sampleLibraryMenu;
        private ToolStripMenuItem _darkModeMenuItem;
        private ToolStripMenuItem _undoMenuItem;

        public MainForm(
            MainViewModel viewModel,
            IAppMessenger messenger,
            ILayoutService layoutService,
            IScoreUndoService undoService)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _layoutService = layoutService ?? throw new ArgumentNullException(nameof(layoutService));
            _undoService = undoService ?? throw new ArgumentNullException(nameof(undoService));
            _undoService.StackChanged += (s, e) => UpdateUndoMenuState();

            InitializeComponent();
            SetupLayoutStructure();

            KeyDown += OnFormKeyDown;
            Load += OnFormLoad;
            Resize += OnFormResize;
            FormClosed += OnFormClosed;
        }

        public void InitializeBindings(object viewModel)
        {
            if (viewModel is not MainViewModel mainViewModel)
            {
                throw new ArgumentException("MainForm requires MainViewModel.", nameof(viewModel));
            }

            _binder = new MainFormViewBinder(
                mainViewModel,
                this,
                _chordBox,
                _measureSelector,
                _measureRangeFrom,
                _measureRangeTo,
                _statusLabel,
                _tieButton,
                _playButton,
                _stopButton);

            _glue = new ScoreCanvasGlue(mainViewModel, _canvas, _messenger);

            _canvas.SelectionChanged += OnCanvasSelectionChanged;
            _canvas.MeasureTextEdited += OnCanvasMeasureTextEdited;
            _canvas.HeaderEdited += OnCanvasHeaderEdited;
            _canvas.ChordMarkersChanged += OnCanvasChordMarkersChanged;
            _canvas.ScoreMutationStarting += OnCanvasScoreMutationStarting;
            _canvas.PlaybackSeeked += OnCanvasPlaybackSeeked;

            mainViewModel.RequestOpenScore += (s, e) => OnOpenScore(s, e);
            mainViewModel.RequestSaveScore += (s, e) => OnSaveScore(s, e);
            mainViewModel.RequestSaveAsScore += (s, e) => OnSaveScoreAs(s, e);
            mainViewModel.RequestExportPdf += (s, e) => OnExportPdf(s, e);
            mainViewModel.RequestExportMidi += (s, e) => OnExportMidi(s, e);
            mainViewModel.RequestTransposeDialog += (s, e) => ShowTransposeDialog();
        }

        public void RestoreLayout()
        {
            _layoutService.RestoreLayout();
        }

        public void ApplyTheme()
        {
            _layoutService.ApplyTheme();
            _binder?.SyncFromViewModels();
        }

        private void SetupLayoutStructure()
        {
            _menuStrip = BuildMenuStrip();
            var toolbarPanel = BuildToolbarPanel();
            ConfigureStatusLabel();

            _chromeLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = false,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            _chromeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _chromeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, MainFormLayoutContext.MinimumMenuHeight));
            _chromeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, MainFormLayoutContext.DefaultToolbarHeight));

            _menuStrip.Dock = DockStyle.Fill;
            toolbarPanel.Dock = DockStyle.Fill;

            _chromeLayout.Controls.Add(_menuStrip, 0, 0);
            _chromeLayout.Controls.Add(toolbarPanel, 0, 1);

            _mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _canvas.Dock = DockStyle.Fill;
            _canvas.MinimumSize = new Size(200, 200);
            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.MinimumSize = new Size(0, MainFormLayoutContext.StatusRowHeight);

            _mainLayout.Controls.Add(_chromeLayout, 0, 0);
            _mainLayout.Controls.Add(_canvas, 0, 1);
            _mainLayout.Controls.Add(_statusLabel, 0, 2);

            Controls.Clear();
            Controls.Add(_mainLayout);
            MainMenuStrip = _menuStrip;

            _layoutContext = new MainFormLayoutContext
            {
                Form = this,
                MainLayout = _mainLayout,
                ChromeLayout = _chromeLayout,
                MenuStrip = _menuStrip,
                ToolbarPanel = toolbarPanel,
                ScoreCanvas = _canvas,
                StatusLabel = _statusLabel
            };
            _layoutService.Attach(_layoutContext);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            InitializeBindings(_viewModel);
            RestoreLayout();
            ApplyDpiScaling();
            ApplyTheme();

            AppLog.Info("简谱编辑器启动");

            var demoResult = _viewModel.SampleLibrary.LoadDemoScore();
            _glue.ApplyEditResult(demoResult);
            _glue.ResetPlaybackHead();
            _binder.SyncHeaderFromDocument();
            _binder.SyncFromViewModels();
            _viewModel.SetStatus("就绪 - 点击谱面标题/调号/速度/BPM/作曲直接编辑，点击歌词行编辑文字");
        }

        private void OnFormResize(object sender, EventArgs e)
        {
            RestoreLayout();
        }

        private void ApplyDpiScaling()
        {
            _layoutService.ApplyDpiScaling();
        }

        private MenuStrip BuildMenuStrip()
        {
            var menu = new MenuStrip();

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
            _undoMenuItem = CreateMenuItem("撤回", Keys.Control | Keys.Z, (s, e) => ExecuteUndo());
            _undoMenuItem.Enabled = false;
            editMenu.DropDownItems.Add(_undoMenuItem);
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
            viewMenu.DropDownItems.Add(new ToolStripSeparator());
            viewMenu.DropDownItems.Add(CreateMenuItem("重置布局", Keys.None, (s, e) => RestoreLayout()));

            menu.Items.Add(fileMenu);
            menu.Items.Add(editMenu);
            menu.Items.Add(viewMenu);
            return menu;
        }

        private FlowLayoutPanel BuildToolbarPanel()
        {
            var panel = new FlowLayoutPanel
            {
                Padding = new Padding(12, 8, 12, 8),
                WrapContents = true,
                AutoScroll = false
            };

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
            panel.Controls.Add(CreateToolButton("高音·", () => ExecuteEdit(() => _viewModel.NoteEditor.SetOctave(1))));
            panel.Controls.Add(CreateToolButton("低音·", () => ExecuteEdit(() => _viewModel.NoteEditor.SetOctave(-1))));
            panel.Controls.Add(CreateToolButton("升key", () => ExecuteEdit(() => _viewModel.NoteEditor.TransposePitch(1))));
            panel.Controls.Add(CreateToolButton("降key", () => ExecuteEdit(() => _viewModel.NoteEditor.TransposePitch(-1))));
            panel.Controls.Add(CreateToolButton("拆分", () => ExecuteEdit(() => _viewModel.NoteEditor.SplitSelectedNotes())));
            panel.Controls.Add(CreateToolButton("合并", () => ExecuteEdit(() => _viewModel.NoteEditor.MergeSelectedNotes())));
            panel.Controls.Add(CreateToolButton("附点", () => ExecuteEdit(() => _viewModel.NoteEditor.ToggleDotted())));
            panel.Controls.Add(CreateToolButton("增时+", () => ExecuteEdit(() => _viewModel.NoteEditor.IncreaseDuration())));
            panel.Controls.Add(CreateToolButton("减时-", () => ExecuteEdit(() => _viewModel.NoteEditor.DecreaseDuration())));
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

            _chordBox.Width = 120;
            _chordBox.TextChanged += OnChordTextChanged;
            panel.Controls.Add(_chordBox);

            panel.Controls.Add(CreateSeparator());
            panel.Controls.Add(CreateToolButton("删除", ExecuteDelete));
            _sampleLibraryMenu = new ContextMenuStrip();
            _sampleLibraryMenu.Opening += (s, e) => PopulateSampleLibraryMenu(_sampleLibraryMenu.Items);
            var sampleButton = CreateToolButton("曲库", () => { });
            sampleButton.Click += (s, e) => _sampleLibraryMenu.Show(sampleButton, new Point(0, sampleButton.Height));
            panel.Controls.Add(sampleButton);

            return panel;
        }

        private void ConfigureStatusLabel()
        {
            _statusLabel.Padding = new Padding(12, 6, 0, 0);
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusLabel.Height = MainFormLayoutContext.StatusRowHeight;
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
            ApplyTheme();
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

        private void RecordUndoSnapshot()
        {
            if (_undoService.IsRestoring)
            {
                return;
            }

            _undoService.RecordSnapshot(_viewModel.Document.Score);
            UpdateUndoMenuState();
        }

        private void UpdateUndoMenuState()
        {
            if (_undoMenuItem != null)
            {
                _undoMenuItem.Enabled = _undoService.CanUndo;
            }
        }

        private void ExecuteEdit(Func<ScoreEditResult> action)
        {
            RecordUndoSnapshot();
            var result = action();
            if (!result.Changed)
            {
                _undoService.DiscardLastSnapshot();
                UpdateUndoMenuState();
            }

            _glue.ApplyEditResult(result);
            _binder.SyncFromViewModels();
        }

        private void ExecuteAddMeasure()
        {
            RecordUndoSnapshot();
            var result = _viewModel.MeasureNavigation.AddMeasure();
            if (!result.Changed)
            {
                _undoService.DiscardLastSnapshot();
                UpdateUndoMenuState();
            }

            _glue.ApplyEditResult(result);
            _binder.SyncFromViewModels();
        }

        private void ExecuteDuplicateMeasures()
        {
            RecordUndoSnapshot();
            var result = _viewModel.MeasureNavigation.DuplicateMeasures();
            if (!result.Changed)
            {
                _undoService.DiscardLastSnapshot();
                UpdateUndoMenuState();
            }

            _glue.ApplyEditResult(result);
            _binder.SyncFromViewModels();
        }

        private void ExecuteDelete()
        {
            RecordUndoSnapshot();
            var result = _viewModel.ScoreEditor.Delete();
            if (!result.Changed)
            {
                _undoService.DiscardLastSnapshot();
                UpdateUndoMenuState();
            }

            _glue.ApplyEditResult(result);
            _binder.SyncFromViewModels();
        }

        private void ExecuteUndo()
        {
            if (!_undoService.CanUndo)
            {
                return;
            }

            var snapshot = _undoService.PopSnapshot();
            if (snapshot == null)
            {
                UpdateUndoMenuState();
                return;
            }

            _undoService.EnterRestore();
            try
            {
                _viewModel.Playback.Stop();
                _viewModel.TieEditor.CancelTieMode();

                var measureIndex = _viewModel.MeasureNavigation.CurrentMeasureIndex;
                if (snapshot.Measures != null && snapshot.Measures.Count > 0)
                {
                    measureIndex = Math.Max(0, Math.Min(measureIndex, snapshot.Measures.Count - 1));
                }
                else
                {
                    measureIndex = 0;
                }

                _viewModel.Document.Score = snapshot;
                _viewModel.MeasureNavigation.SyncCurrentMeasureIndex(measureIndex);
                _viewModel.MeasureContent.LoadFromMeasure(measureIndex);
                _viewModel.ChordEditor.SyncFromSelection();

                _glue.ApplyEditResult(new ScoreEditResult
                {
                    Changed = true,
                    SelectMeasureIndex = measureIndex,
                    ClearMelodySelection = true,
                    ClearTieSelection = true,
                    ClearChordSelection = true
                });
                _glue.ResetPlaybackHead();
                _binder.SyncHeaderFromDocument();
                _binder.SyncFromViewModels();
                _viewModel.SetStatus("已撤回");
            }
            finally
            {
                _undoService.LeaveRestore();
                UpdateUndoMenuState();
            }
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

            if (e.Control && e.KeyCode == Keys.Z)
            {
                ExecuteUndo();
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

        private void OnCanvasHeaderEdited(object sender, ScoreHeaderEditedEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            RecordUndoSnapshot();
            var text = e.Text ?? string.Empty;
            switch (e.Field)
            {
                case ScoreHeaderField.Title:
                    _viewModel.Document.Title = text;
                    break;
                case ScoreHeaderField.KeySignature:
                    _viewModel.Document.KeySignature = text;
                    break;
                case ScoreHeaderField.Tempo:
                    _viewModel.Document.Tempo = text;
                    break;
                case ScoreHeaderField.Bpm:
                    if (int.TryParse(text.Trim(), out var bpm))
                    {
                        _viewModel.Document.Bpm = Math.Max(30, Math.Min(300, bpm));
                    }

                    break;
                case ScoreHeaderField.Composer:
                    _viewModel.Document.Composer = text;
                    break;
            }

            _binder.SyncHeaderFromDocument();
            _binder.SyncFromViewModels();
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

        private void OnCanvasScoreMutationStarting(object sender, EventArgs e)
        {
            RecordUndoSnapshot();
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
            RecordUndoSnapshot();
            var result = _viewModel.ScoreEditor.ClearScore();
            if (!result.Changed)
            {
                _undoService.DiscardLastSnapshot();
                UpdateUndoMenuState();
            }

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
            _glue?.Dispose();
            _binder?.Dispose();
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

                RecordUndoSnapshot();
                var result = _viewModel.ChordEditor.TransposeChords(targetKey);
                if (!result.Changed)
                {
                    _undoService.DiscardLastSnapshot();
                    UpdateUndoMenuState();
                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        MessageBox.Show(result.Message, "转调失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    return;
                }

                _glue.ApplyEditResult(result);
                _binder.SyncHeaderFromDocument();
                _binder.SyncFromViewModels();
            }
        }
    }
}
