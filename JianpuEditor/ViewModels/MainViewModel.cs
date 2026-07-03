using System;
using JianpuEditor.Presentation;

namespace JianpuEditor.ViewModels
{
    public sealed class MainViewModel : NotifyPropertyChangedBase
    {
        private string _statusMessage = "就绪";

        public MainViewModel()
        {
            Document = new ScoreDocumentViewModel();
            NewScoreCommand = new RelayCommand(NewScore);
            OpenScoreCommand = new RelayCommand(() => RequestOpenScore?.Invoke(this, EventArgs.Empty));
            SaveScoreCommand = new RelayCommand(() => RequestSaveScore?.Invoke(this, EventArgs.Empty), () => Document.IsDirty || !string.IsNullOrEmpty(Document.CurrentFilePath));
        }

        public ScoreDocumentViewModel Document { get; }

        public string StatusMessage
        {
            get { return _statusMessage; }
            set { SetProperty(ref _statusMessage, value); }
        }

        public RelayCommand NewScoreCommand { get; }

        public RelayCommand OpenScoreCommand { get; }

        public RelayCommand SaveScoreCommand { get; }

        public event EventHandler RequestOpenScore;

        public event EventHandler RequestSaveScore;

        public void NewScore()
        {
            Document.ResetAsNew();
            StatusMessage = "已新建谱面";
            SaveScoreCommand.RaiseCanExecuteChanged();
        }

        public void SetStatus(string message)
        {
            StatusMessage = message ?? string.Empty;
        }
    }
}