using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace JianpuEditor.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        private string _statusMessage = "就绪";

        public MainViewModel(ScoreDocumentViewModel document)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            NewScoreCommand = new RelayCommand(NewScore);
            OpenScoreCommand = new RelayCommand(() => RequestOpenScore?.Invoke(this, EventArgs.Empty));
            SaveScoreCommand = new RelayCommand(
                () => RequestSaveScore?.Invoke(this, EventArgs.Empty),
                () => Document.IsDirty || !string.IsNullOrEmpty(Document.CurrentFilePath));
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
            SaveScoreCommand.NotifyCanExecuteChanged();
        }

        public void SetStatus(string message)
        {
            StatusMessage = message ?? string.Empty;
        }
    }
}
