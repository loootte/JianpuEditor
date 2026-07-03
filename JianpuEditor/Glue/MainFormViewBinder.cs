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
        private readonly TextBox _chordBox;
        private readonly NumericUpDown _measureSelector;
        private readonly NumericUpDown _measureRangeFrom;
        private readonly NumericUpDown _measureRangeTo;
        private readonly Label _statusLabel;
        private readonly Button _tieButton;
        private readonly Button _playButton;
        private readonly Button _stopButton;
        private bool _suppressMeasureTextSync;
        private bool _suppressMeasureRangeSync;
        private bool _suppressMeasureSelectorSync;

        public MainFormViewBinder(
            MainViewModel viewModel,
            Form form,
            TextBox chordBox,
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
            _chordBox = chordBox;
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
            _form.Text = _viewModel.Document.WindowTitle;
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
            if (e.PropertyName == nameof(ScoreDocumentViewModel.WindowTitle)
                || e.PropertyName == nameof(ScoreDocumentViewModel.Title))
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
