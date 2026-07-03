using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Services;

namespace JianpuEditor.ViewModels
{
    public sealed class SampleLibraryViewModel : ObservableObject
    {
        private readonly ScoreDocumentViewModel _document;
        private readonly IAppMessenger _messenger;
        private IReadOnlyList<string> _samples = Array.Empty<string>();

        public SampleLibraryViewModel(ScoreDocumentViewModel document, IAppMessenger messenger)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            RefreshSamplesCommand = new RelayCommand(RefreshSamples);
            LoadDemoScoreCommand = new RelayCommand(() => LoadDemoScore());
            LoadSampleCommand = new RelayCommand<string>(
                path => LoadSample(path),
                path => !string.IsNullOrWhiteSpace(path));
            RefreshSamples();
        }

        public IReadOnlyList<string> Samples
        {
            get { return _samples; }
            private set { SetProperty(ref _samples, value); }
        }

        public bool HasSamples
        {
            get { return Samples != null && Samples.Count > 0; }
        }

        public RelayCommand RefreshSamplesCommand { get; }

        public RelayCommand LoadDemoScoreCommand { get; }

        public RelayCommand<string> LoadSampleCommand { get; }

        public void RefreshSamples()
        {
            Samples = SampleLibraryService.ListSampleFiles();
            OnPropertyChanged(nameof(HasSamples));
        }

        public ScoreEditResult LoadDemoScore()
        {
            _document.LoadDemoScore();
            var message = "已加载示例谱面《欢乐颂》";
            _messenger.Send(new ScoreEditedMessage(message, markDirty: false));
            _messenger.Send(new ScoreLoadedMessage(_document.Score, null));
            return new ScoreEditResult
            {
                Changed = true,
                Message = message,
                SelectMeasureIndex = 0,
                RequiresScoreRefresh = true
            };
        }

        public ScoreEditResult LoadSample(string path)
        {
            try
            {
                _document.LoadSample(path);
                var displayName = SampleLibraryService.GetDisplayName(path);
                var message = "已加载示例曲谱：" + _document.Score.Title;
                _messenger.Send(new ScoreEditedMessage(message, markDirty: false));
                _messenger.Send(new ScoreLoadedMessage(_document.Score, path));
                return new ScoreEditResult
                {
                    Changed = true,
                    Message = message,
                    SelectMeasureIndex = 0,
                    RequiresScoreRefresh = true
                };
            }
            catch (Exception ex)
            {
                AppLog.Exception("加载示例曲谱失败: " + path, ex);
                throw;
            }
        }

        public string GetDisplayName(string path)
        {
            return SampleLibraryService.GetDisplayName(path);
        }

        public string BuildWindowTitle(string samplePath)
        {
            if (!string.IsNullOrWhiteSpace(_document.CurrentFilePath))
            {
                return "简谱编辑器 - " + Path.GetFileName(_document.CurrentFilePath);
            }

            if (!string.IsNullOrWhiteSpace(samplePath))
            {
                return "简谱编辑器 - " + SampleLibraryService.GetDisplayName(samplePath);
            }

            return "简谱编辑器";
        }
    }
}
