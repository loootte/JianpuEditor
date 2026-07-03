using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JianpuEditor.Core.Abstractions;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Services;

namespace JianpuEditor.ViewModels
{
    public sealed class PlaybackViewModel : ObservableObject, IDisposable
    {
        private readonly ScoreDocumentViewModel _document;
        private readonly IScorePlaybackService _playbackService;
        private readonly IAppMessenger _messenger;
        private bool _isPlaying;
        private double _playbackPositionQuarter;

        public PlaybackViewModel(
            ScoreDocumentViewModel document,
            IScorePlaybackService playbackService,
            IAppMessenger messenger)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _playbackService = playbackService ?? throw new ArgumentNullException(nameof(playbackService));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            PlayCommand = new RelayCommand(() => Play(), () => !IsPlaying);
            StopCommand = new RelayCommand(Stop, () => IsPlaying);

            _playbackService.PositionChanged += OnPlaybackPositionChanged;
            _playbackService.PlaybackFinished += OnPlaybackFinished;
            _playbackService.PlaybackError += OnPlaybackError;
            _messenger.Register<PlaybackViewModel, ScoreEditedMessage>(this, OnScoreEdited);
        }

        public bool IsPlaying
        {
            get { return _isPlaying; }
            private set
            {
                if (SetProperty(ref _isPlaying, value))
                {
                    PlayCommand.NotifyCanExecuteChanged();
                    StopCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public double PlaybackPositionQuarter
        {
            get { return _playbackPositionQuarter; }
            private set { SetProperty(ref _playbackPositionQuarter, value); }
        }

        public double TotalQuarterLength
        {
            get { return _playbackService.TotalQuarterLength; }
        }

        public RelayCommand PlayCommand { get; }

        public RelayCommand StopCommand { get; }

        public event Action<double> PositionChanged;

        public event Action PlaybackFinished;

        public event Action<Exception> PlaybackError;

        public void Play(double startQuarter = -1)
        {
            try
            {
                var quarter = startQuarter >= 0 ? startQuarter : PlaybackPositionQuarter;
                AppLog.Info(
                    "播放请求: bpm=" + _document.Bpm +
                    ", startQuarter=" + quarter.ToString("0.###") +
                    ", measures=" + (_document.Score.Measures?.Count ?? 0));
                _playbackService.Play(_document.Score, _document.Bpm, quarter);
                IsPlaying = true;
                PlaybackPositionQuarter = quarter;
                _messenger.Send(new StatusChangedMessage("正在播放..."));
            }
            catch (Exception ex)
            {
                AppLog.Exception("播放失败", ex);
                IsPlaying = false;
                PlaybackError?.Invoke(ex);
            }
        }

        public void Stop()
        {
            _playbackService.Stop();
            IsPlaying = false;
            _messenger.Send(new StatusChangedMessage("播放已停止"));
        }

        public void Seek(double quarterBeat)
        {
            try
            {
                _playbackService.Seek(quarterBeat);
                if (!IsPlaying)
                {
                    PlaybackPositionQuarter = quarterBeat;
                    PositionChanged?.Invoke(quarterBeat);
                }
            }
            catch (Exception ex)
            {
                AppLog.Exception("播放跳转失败", ex);
                PlaybackError?.Invoke(ex);
            }
        }

        public void ResetHead()
        {
            _playbackService.Prepare(_document.Score, 0);
            PlaybackPositionQuarter = 0;
            IsPlaying = false;
            PositionChanged?.Invoke(0);
        }

        public void Dispose()
        {
            _playbackService.PositionChanged -= OnPlaybackPositionChanged;
            _playbackService.PlaybackFinished -= OnPlaybackFinished;
            _playbackService.PlaybackError -= OnPlaybackError;
            _messenger.UnregisterAll<PlaybackViewModel>(this);
            _playbackService.Dispose();
        }

        private void OnPlaybackPositionChanged(double quarterBeat)
        {
            PlaybackPositionQuarter = quarterBeat;
            PositionChanged?.Invoke(quarterBeat);
        }

        private void OnPlaybackFinished()
        {
            IsPlaying = false;
            _messenger.Send(new StatusChangedMessage("播放完成"));
            PlaybackFinished?.Invoke();
        }

        private void OnPlaybackError(Exception ex)
        {
            IsPlaying = false;
            PlaybackError?.Invoke(ex);
        }

        private void OnScoreEdited(PlaybackViewModel recipient, ScoreEditedMessage message)
        {
            if (message.StopPlayback && recipient.IsPlaying)
            {
                recipient.Stop();
            }
        }
    }
}
