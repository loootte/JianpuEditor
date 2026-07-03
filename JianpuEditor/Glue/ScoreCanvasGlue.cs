using System;
using System.ComponentModel;
using JianpuEditor.Controls;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Glue
{
    internal sealed class ScoreCanvasGlue : IDisposable
    {
        private readonly MainViewModel _viewModel;
        private readonly ScoreCanvas _canvas;
        private readonly IAppMessenger _messenger;

        public ScoreCanvasGlue(MainViewModel viewModel, ScoreCanvas canvas, IAppMessenger messenger)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            _canvas.Score = _viewModel.Document.Score;
            _viewModel.Document.PropertyChanged += OnDocumentPropertyChanged;
            _viewModel.Playback.PositionChanged += OnPlaybackPositionChanged;
            _viewModel.Playback.PlaybackFinished += OnPlaybackFinished;
            _viewModel.Playback.PlaybackError += OnPlaybackError;
            _messenger.Register<ScoreCanvasGlue, ScoreEditedMessage>(this, OnScoreEdited);
            _messenger.Register<ScoreCanvasGlue, ScoreLoadedMessage>(this, OnScoreLoaded);
        }

        public void ApplyEditResult(ScoreEditResult result)
        {
            if (result == null || !result.Changed)
            {
                return;
            }

            if (result.ClearMelodySelection)
            {
                _canvas.ClearMelodySelection();
            }

            if (result.ClearTieSelection)
            {
                _canvas.ClearTieSelection();
            }

            if (result.ClearChordSelection)
            {
                _canvas.ClearChordSelection();
            }

            if (result.SetSelectedMeasureIndices != null && result.SetPrimaryMeasureIndex.HasValue)
            {
                _canvas.SetSelectedMeasures(result.SetSelectedMeasureIndices, result.SetPrimaryMeasureIndex.Value);
            }
            else if (result.SelectNoteMeasureIndex.HasValue && result.SelectNoteIndex.HasValue)
            {
                _canvas.SelectNote(result.SelectNoteMeasureIndex.Value, result.SelectNoteIndex.Value);
            }
            else if (result.SelectChordMeasureIndex.HasValue && result.SelectChordMarkerIndex.HasValue)
            {
                _canvas.SelectChordMarker(
                    result.SelectChordMeasureIndex.Value,
                    result.SelectChordMarkerIndex.Value);
            }
            else if (result.SelectMeasureIndex.HasValue)
            {
                _canvas.SelectMeasure(result.SelectMeasureIndex.Value);
            }

            if (result.RequiresScoreRefresh)
            {
                RefreshCanvas();
            }
        }

        public void AttachDocumentScore()
        {
            _canvas.Score = _viewModel.Document.Score;
            RefreshCanvas();
        }

        public void RefreshCanvas()
        {
            _canvas.RefreshScore();
        }

        public void ResetPlaybackHead()
        {
            _viewModel.Playback.ResetHead();
            _canvas.SetPlaybackPosition(0, showHead: true, ensureVisible: false);
        }

        public void Dispose()
        {
            _viewModel.Document.PropertyChanged -= OnDocumentPropertyChanged;
            _viewModel.Playback.PositionChanged -= OnPlaybackPositionChanged;
            _viewModel.Playback.PlaybackFinished -= OnPlaybackFinished;
            _viewModel.Playback.PlaybackError -= OnPlaybackError;
            _messenger.UnregisterAll<ScoreCanvasGlue>(this);
        }

        private void OnDocumentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScoreDocumentViewModel.Score))
            {
                AttachDocumentScore();
                return;
            }

            if (e.PropertyName == nameof(ScoreDocumentViewModel.Title)
                || e.PropertyName == nameof(ScoreDocumentViewModel.KeySignature)
                || e.PropertyName == nameof(ScoreDocumentViewModel.Tempo)
                || e.PropertyName == nameof(ScoreDocumentViewModel.Bpm)
                || e.PropertyName == nameof(ScoreDocumentViewModel.Composer))
            {
                RefreshCanvas();
            }
        }

        private void OnPlaybackPositionChanged(double quarterBeat)
        {
            _canvas.SetPlaybackPosition(quarterBeat, showHead: true, ensureVisible: false);
        }

        private void OnPlaybackFinished()
        {
            // Button state handled by view binder.
        }

        private void OnPlaybackError(Exception ex)
        {
            ShowPlaybackError("播放中断", ex);
        }

        private void OnScoreEdited(ScoreCanvasGlue recipient, ScoreEditedMessage message)
        {
            recipient.RefreshCanvas();
        }

        private void OnScoreLoaded(ScoreCanvasGlue recipient, ScoreLoadedMessage message)
        {
            recipient.AttachDocumentScore();
        }

        private static void ShowPlaybackError(string title, Exception ex)
        {
            var msg = ex == null
                ? title
                : title + Environment.NewLine + Environment.NewLine +
                  ex.Message + Environment.NewLine + Environment.NewLine +
                  "详细日志:" + Environment.NewLine + Services.AppLog.LogFilePath;
            System.Windows.Forms.MessageBox.Show(msg, "错误", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        }
    }
}
