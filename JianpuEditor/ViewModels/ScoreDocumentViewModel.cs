using JianpuEditor.Models;
using JianpuEditor.Presentation;
using JianpuEditor.Services;

namespace JianpuEditor.ViewModels
{
    public sealed class ScoreDocumentViewModel : NotifyPropertyChangedBase
    {
        private JianpuScore _score = new JianpuScore();
        private string _currentFilePath;
        private bool _isDirty;

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
                ChordMarkerService.NormalizeScore(_score);
                OnPropertyChanged(nameof(Score));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(KeySignature));
                OnPropertyChanged(nameof(Tempo));
                OnPropertyChanged(nameof(Bpm));
                OnPropertyChanged(nameof(Composer));
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
            private set { SetProperty(ref _currentFilePath, value); }
        }

        public bool IsDirty
        {
            get { return _isDirty; }
            private set { SetProperty(ref _isDirty, value); }
        }

        public void LoadFromFile(string path)
        {
            Score = ScoreFileService.Load(path);
            CurrentFilePath = path;
            IsDirty = false;
        }

        public void SaveToFile(string path)
        {
            ScoreFileService.Save(_score, path);
            CurrentFilePath = path;
            IsDirty = false;
        }

        public void ResetAsNew()
        {
            Score = new JianpuScore();
            CurrentFilePath = null;
            IsDirty = false;
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }
    }
}