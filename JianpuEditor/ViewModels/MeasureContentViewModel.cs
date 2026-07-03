using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JianpuEditor.Core.Abstractions;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Services;
using JianpuEditor.Services.EditCommands;

namespace JianpuEditor.ViewModels
{
    public sealed class MeasureContentViewModel : ObservableObject
    {
        private readonly ScoreDocumentViewModel _document;
        private readonly MeasureNavigationViewModel _navigation;
        private readonly IAppMessenger _messenger;
        private readonly IEditCommandHistory _history;
        private string _currentLyricText = string.Empty;
        private bool _suppressSync;

        public MeasureContentViewModel(
            ScoreDocumentViewModel document,
            MeasureNavigationViewModel navigation,
            IAppMessenger messenger,
            IEditCommandHistory history)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            AlignLyricsCommand = new RelayCommand(() => AlignLyricsToNotes());
        }

        public RelayCommand AlignLyricsCommand { get; }

        public string CurrentLyricText
        {
            get { return _currentLyricText; }
            set
            {
                if (_suppressSync)
                {
                    SetProperty(ref _currentLyricText, value);
                    return;
                }

                if (!SetProperty(ref _currentLyricText, value))
                {
                    return;
                }

                ApplyLyricText(value);
            }
        }

        public void LoadFromMeasure(int measureIndex)
        {
            _document.EnsureMeasures();
            measureIndex = Math.Max(0, Math.Min(measureIndex, _document.Score.Measures.Count - 1));
            _suppressSync = true;
            CurrentLyricText = _document.Score.Measures[measureIndex].LyricText ?? string.Empty;
            _suppressSync = false;
        }

        public void ApplyLyricText(string text)
        {
            _document.EnsureMeasures();
            var measureIndex = Math.Max(0, _navigation.CurrentMeasureIndex);
            if (measureIndex >= _document.Score.Measures.Count)
            {
                return;
            }

            var newText = text ?? string.Empty;
            var currentText = _document.Score.Measures[measureIndex].LyricText ?? string.Empty;
            if (string.Equals(currentText, newText, StringComparison.Ordinal))
            {
                return;
            }

            _history.Execute(new ModifyLyricTextCommand(
                _document.Score,
                _messenger,
                measureIndex,
                currentText,
                newText));
        }

        public ScoreEditResult NotifyInlineLyricEdited(int measureIndex)
        {
            LoadFromMeasure(measureIndex);
            return ScoreEditResult.WithMessage("已更新小节文字");
        }

        public ScoreEditResult AlignLyricsToNotes()
        {
            _document.EnsureMeasures();
            var measureIndex = Math.Max(0, Math.Min(_navigation.CurrentMeasureIndex, _document.Score.Measures.Count - 1));
            var measure = _document.Score.Measures[measureIndex];
            var lyricText = measure.LyricText ?? string.Empty;
            if (string.IsNullOrWhiteSpace(lyricText))
            {
                lyricText = _currentLyricText ?? string.Empty;
            }

            if (!LyricAlignmentService.TryBuildAlignment(
                    measure,
                    lyricText,
                    _document.Score.Ties,
                    measureIndex,
                    out var syllables,
                    out var message))
            {
                _messenger.Send(new StatusChangedMessage(message));
                return ScoreEditResult.Unchanged;
            }

            var command = new AlignLyricSyllablesCommand(
                _document.Score,
                _messenger,
                measureIndex,
                measure.LyricSyllables,
                measure.LyricText,
                syllables,
                message);
            _history.Execute(command);
            return command.Result ?? ScoreEditResult.Unchanged;
        }
    }
}
