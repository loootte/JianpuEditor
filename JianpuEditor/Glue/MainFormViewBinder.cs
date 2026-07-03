using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JianpuEditor.Rendering;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Glue
{
    internal sealed class MainFormViewBinder : IDisposable
    {
        private readonly MainViewModel _viewModel;
        private readonly Form _form;
        private readonly TextBox _titleBox;
        private readonly TextBox _keyBox;
        private readonly TextBox _tempoBox;
        private readonly NumericUpDown _bpmBox;
        private readonly TextBox _composerBox;
        private readonly TextBox _chordBox;
        private readonly TextBox _lyricBox;
        private readonly NumericUpDown _measureSelector;
        private readonly NumericUpDown _measureRangeFrom;
        private readonly NumericUpDown _measureRangeTo;
        private readonly Label _statusLabel;
        private readonly Button _tieButton;
        private readonly Button _playButton;
        private readonly Button _stopButton;
        private bool _suppressHeaderSync;
        private bool _suppressMeasureTextSync;
        private bool _suppressMeasureRangeSync;
        private bool _suppressMeasureSelectorSync;

        public MainFormViewBinder(
            MainViewModel viewModel,
            Form form,
            TextBox titleBox,
            TextBox keyBox,
            TextBox tempoBox,
            NumericUpDown bpmBox,
            TextBox composerBox,
            TextBox chordBox,
            TextBox lyricBox,
            NumericUpDown measureSelector,
            NumericUpDown measureRangeFrom,
            NumericUpDown measureRangeTo,
            Label statusLabel,
            Button tieButton,
            Button playButton,
            Button stopButton)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _titleBox = titleBox;
            _keyBox = keyBox;
            _tempoBox = tempoBox;
            _bpmBox = bpmBox;
            _composerBox = composerBox;
            _chordBox = chordBox;
            _lyricBox = lyricBox;
            _measureSelector = measureSelector;
            _measureRangeFrom = measureRangeFrom;
            _measureRangeTo = measureRangeTo;
            _statusLabel = statusLabel;
            _tieButton = tieButton;
            _playButton = playButton;
            _stopButton = stopButton;

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.Document.PropertyChanged += OnDocumentPropertyChanged;
            _viewModel.TieEditor.PropertyChanged += OnTieEditorPropertyChanged;
            _viewModel.Playback.PropertyChanged += OnPlaybackPropertyChanged;
            _viewModel.ChordEditor.PropertyChanged += OnChordEditorPropertyChanged;
            _viewModel.MeasureContent.PropertyChanged += OnMeasureContentPropertyChanged;

            WireHeaderInputs();
            SyncHeaderFromDocument();
            SyncFromViewModels();
            UpdatePlaybackButtons();
            UpdateTieButton();
        }

        public bool SuppressMeasureTextSync
        {
            get { return _suppressMeasureTextSync; }
        }

        public bool SuppressMeasureRangeSync
        {
            get { return _suppressMeasureRangeSync; }
        }

        public bool SuppressMeasureSelectorSync
        {
            get { return _suppressMeasureSelectorSync; }
        }

        public void SyncHeaderFromDocument()
        {
            var document = _viewModel.Document;
            _suppressHeaderSync = true;
            _titleBox.Text = document.Title ?? string.Empty;
            _keyBox.Text = document.KeySignature ?? string.Empty;
            _tempoBox.Text = document.Tempo ?? string.Empty;
            _bpmBox.Value = Math.Max(_bpmBox.Minimum, Math.Min(_bpmBox.Maximum, document.Bpm > 0 ? document.Bpm : 120));
            _composerBox.Text = document.Composer ?? string.Empty;
            _form.Text = document.WindowTitle;
            _suppressHeaderSync = false;
        }

        public void SyncFromViewModels()
        {
            _statusLabel.Text = _viewModel.StatusMessage ?? string.Empty;
            SyncMeasureControls();
            SyncMeasureTextBoxes();
            UpdatePlaybackButtons();
            UpdateTieButton();
        }

        public void SyncMeasureControls()
        {
            var measureCount = Math.Max(1, _viewModel.MeasureNavigation.MeasureCount);
            _suppressMeasureSelectorSync = true;
            _suppressMeasureRangeSync = true;
            _measureSelector.Maximum = measureCount;
            _measureRangeFrom.Maximum = measureCount;
            _measureRangeTo.Maximum = measureCount;

            var currentIndex = Math.Max(0, _viewModel.MeasureNavigation.CurrentMeasureIndex);
            _measureSelector.Value = Math.Max(1, currentIndex + 1);

            var range = _viewModel.Selection.GetMeasureRangeIndices();
            _measureRangeFrom.Value = Math.Max(1, range.fromIndex + 1);
            _measureRangeTo.Value = Math.Max(1, range.toIndex + 1);
            _suppressMeasureRangeSync = false;
            _suppressMeasureSelectorSync = false;
        }

        public void SyncMeasureTextBoxes()
        {
            _suppressMeasureTextSync = true;
            _lyricBox.Text = _viewModel.MeasureContent.CurrentLyricText ?? string.Empty;
            _chordBox.Text = _viewModel.ChordEditor.SelectedChordText ?? string.Empty;
            _chordBox.Enabled = _viewModel.ChordEditor.IsChordEditorEnabled;
            _suppressMeasureTextSync = false;
        }

        public void Dispose()
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.Document.PropertyChanged -= OnDocumentPropertyChanged;
            _viewModel.TieEditor.PropertyChanged -= OnTieEditorPropertyChanged;
            _viewModel.Playback.PropertyChanged -= OnPlaybackPropertyChanged;
            _viewModel.ChordEditor.PropertyChanged -= OnChordEditorPropertyChanged;
            _viewModel.MeasureContent.PropertyChanged -= OnMeasureContentPropertyChanged;
        }

        private void WireHeaderInputs()
        {
            _titleBox.TextChanged += (s, e) =>
            {
                if (_suppressHeaderSync)
                {
                    return;
                }

                _viewModel.Document.Title = _titleBox.Text;
            };
            _keyBox.TextChanged += (s, e) =>
            {
                if (_suppressHeaderSync)
                {
                    return;
                }

                _viewModel.Document.KeySignature = _keyBox.Text;
            };
            _tempoBox.TextChanged += (s, e) =>
            {
                if (_suppressHeaderSync)
                {
                    return;
                }

                _viewModel.Document.Tempo = _tempoBox.Text;
            };
            _bpmBox.ValueChanged += (s, e) =>
            {
                if (_suppressHeaderSync)
                {
                    return;
                }

                _viewModel.Document.Bpm = (int)_bpmBox.Value;
            };
            _composerBox.TextChanged += (s, e) =>
            {
                if (_suppressHeaderSync)
                {
                    return;
                }

                _viewModel.Document.Composer = _composerBox.Text;
            };
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.StatusMessage))
            {
                _statusLabel.Text = _viewModel.StatusMessage ?? string.Empty;
            }
        }

        private void OnDocumentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScoreDocumentViewModel.WindowTitle))
            {
                _form.Text = _viewModel.Document.WindowTitle;
            }
            else if (e.PropertyName == nameof(ScoreDocumentViewModel.Title)
                     || e.PropertyName == nameof(ScoreDocumentViewModel.KeySignature)
                     || e.PropertyName == nameof(ScoreDocumentViewModel.Tempo)
                     || e.PropertyName == nameof(ScoreDocumentViewModel.Bpm)
                     || e.PropertyName == nameof(ScoreDocumentViewModel.Composer))
            {
                SyncHeaderFromDocument();
            }
            else if (e.PropertyName == nameof(ScoreDocumentViewModel.Score))
            {
                SyncMeasureControls();
            }
        }

        private void OnTieEditorPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TieEditorViewModel.IsTieModeActive))
            {
                UpdateTieButton();
            }
        }

        private void OnPlaybackPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlaybackViewModel.IsPlaying))
            {
                UpdatePlaybackButtons();
            }
        }

        private void OnChordEditorPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChordEditorViewModel.SelectedChordText)
                || e.PropertyName == nameof(ChordEditorViewModel.IsChordEditorEnabled))
            {
                if (!_suppressMeasureTextSync)
                {
                    SyncMeasureTextBoxes();
                }
            }
        }

        private void OnMeasureContentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MeasureContentViewModel.CurrentLyricText))
            {
                if (!_suppressMeasureTextSync)
                {
                    _suppressMeasureTextSync = true;
                    _lyricBox.Text = _viewModel.MeasureContent.CurrentLyricText ?? string.Empty;
                    _suppressMeasureTextSync = false;
                }
            }
        }

        private void UpdateTieButton()
        {
            if (_tieButton == null)
            {
                return;
            }

            _tieButton.BackColor = _viewModel.TieEditor.IsTieModeActive
                ? AppTheme.TieModeButtonBackground
                : AppTheme.IsDarkMode ? Color.FromArgb(58, 58, 64) : SystemColors.Control;
        }

        private void UpdatePlaybackButtons()
        {
            if (_playButton == null || _stopButton == null)
            {
                return;
            }

            _playButton.Enabled = !_viewModel.Playback.IsPlaying;
            _stopButton.Enabled = _viewModel.Playback.IsPlaying;
        }
    }
}
