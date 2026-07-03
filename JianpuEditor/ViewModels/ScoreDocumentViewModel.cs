using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using JianpuEditor.Core.Abstractions;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.Services;

namespace JianpuEditor.ViewModels
{
    public sealed class ScoreDocumentViewModel : ObservableObject
    {
        private readonly IScoreFileService _fileService;
        private readonly IAppMessenger _messenger;
        private JianpuScore _score = new JianpuScore();
        private string _currentFilePath;
        private bool _isDirty;

        public ScoreDocumentViewModel(IScoreFileService fileService, IAppMessenger messenger)
        {
            _fileService = fileService ?? new ScoreFileServiceAdapter();
            _messenger = messenger;
        }

        public JianpuScore Score
        {
            get { return _score; }
            set
            {
                if (ReferenceEquals(_score, value))
                {
                    return;
                }

                _score = value ?? new JianpuScore();
                EnsureMeasures();
                ChordMarkerService.NormalizeScore(_score);
                OnPropertyChanged(nameof(Score));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(KeySignature));
                OnPropertyChanged(nameof(Tempo));
                OnPropertyChanged(nameof(Bpm));
                OnPropertyChanged(nameof(Composer));
                OnPropertyChanged(nameof(WindowTitle));
                MarkDirty();
            }
        }

        public string Title
        {
            get { return _score.Title; }
            set
            {
                if (_score.Title == value)
                {
                    return;
                }

                _score.Title = value ?? string.Empty;
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(WindowTitle));
                MarkDirty();
            }
        }

        public string KeySignature
        {
            get { return _score.KeySignature; }
            set
            {
                if (_score.KeySignature == value)
                {
                    return;
                }

                _score.KeySignature = value ?? string.Empty;
                OnPropertyChanged(nameof(KeySignature));
                MarkDirty();
            }
        }

        public string Tempo
        {
            get { return _score.Tempo; }
            set
            {
                if (_score.Tempo == value)
                {
                    return;
                }

                _score.Tempo = value ?? string.Empty;
                OnPropertyChanged(nameof(Tempo));
                MarkDirty();
            }
        }

        public int Bpm
        {
            get { return _score.Bpm; }
            set
            {
                if (_score.Bpm == value)
                {
                    return;
                }

                _score.Bpm = value;
                OnPropertyChanged(nameof(Bpm));
                MarkDirty();
            }
        }

        public string Composer
        {
            get { return _score.Composer; }
            set
            {
                if (_score.Composer == value)
                {
                    return;
                }

                _score.Composer = value ?? string.Empty;
                OnPropertyChanged(nameof(Composer));
                MarkDirty();
            }
        }

        public string CurrentFilePath
        {
            get { return _currentFilePath; }
            private set
            {
                if (SetProperty(ref _currentFilePath, value))
                {
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public string WindowTitle
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_currentFilePath))
                {
                    return "简谱编辑器 - " + Path.GetFileName(_currentFilePath);
                }

                return "简谱编辑器";
            }
        }

        public bool IsDirty
        {
            get { return _isDirty; }
            private set { SetProperty(ref _isDirty, value); }
        }

        public void EnsureMeasures()
        {
            if (_score.Measures == null || _score.Measures.Count == 0)
            {
                _score.Measures = new List<JianpuMeasure> { new JianpuMeasure() };
            }
        }

        public void LoadFromFile(string path)
        {
            Score = _fileService.Load(path);
            CurrentFilePath = path;
            IsDirty = false;
            _messenger?.Send(new ScoreLoadedMessage(_score, path));
        }

        public void SaveToFile(string path)
        {
            _fileService.Save(_score, path);
            CurrentFilePath = path;
            IsDirty = false;
        }

        public void ResetAsNew()
        {
            _score = new JianpuScore();
            EnsureMeasures();
            ChordMarkerService.NormalizeScore(_score);
            CurrentFilePath = null;
            IsDirty = false;
            OnPropertyChanged(nameof(Score));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(KeySignature));
            OnPropertyChanged(nameof(Tempo));
            OnPropertyChanged(nameof(Bpm));
            OnPropertyChanged(nameof(Composer));
            OnPropertyChanged(nameof(WindowTitle));
            _messenger?.Send(new ScoreLoadedMessage(_score, null));
        }

        public void LoadDemoScore()
        {
            _score = DemoScoreFactory.CreateOdeToJoy();
            CurrentFilePath = null;
            IsDirty = false;
            OnPropertyChanged(nameof(Score));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(KeySignature));
            OnPropertyChanged(nameof(Tempo));
            OnPropertyChanged(nameof(Bpm));
            OnPropertyChanged(nameof(Composer));
            OnPropertyChanged(nameof(WindowTitle));
        }

        public void LoadSample(string path)
        {
            _score = _fileService.Load(path);
            ChordMarkerService.NormalizeScore(_score);
            CurrentFilePath = null;
            IsDirty = false;
            OnPropertyChanged(nameof(Score));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(KeySignature));
            OnPropertyChanged(nameof(Tempo));
            OnPropertyChanged(nameof(Bpm));
            OnPropertyChanged(nameof(Composer));
            OnPropertyChanged(nameof(WindowTitle));
        }

        public void ClearMeasures()
        {
            EnsureMeasures();
            _score.Measures = new List<JianpuMeasure> { new JianpuMeasure() };
            MarkDirty();
            OnPropertyChanged(nameof(Score));
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }

        public void MarkClean()
        {
            IsDirty = false;
        }
    }
}
