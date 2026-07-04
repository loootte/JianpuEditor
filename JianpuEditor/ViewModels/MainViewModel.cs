using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JianpuEditor.Core.Abstractions;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;

namespace JianpuEditor.ViewModels
{
    public sealed class MainViewModel : ObservableObject, IDisposable
    {
        private readonly IPdfExportService _pdfExportService;
        private readonly IMidiExportService _midiExportService;
        private readonly IMidiImportService _midiImportService;
        private readonly IAppMessenger _messenger;
        private string _statusMessage = "就绪";

        public MainViewModel(
            ScoreDocumentViewModel document,
            ScoreSelectionViewModel selection,
            NoteEditorViewModel noteEditor,
            TieEditorViewModel tieEditor,
            MeasureNavigationViewModel measureNavigation,
            MeasureContentViewModel measureContent,
            ChordEditorViewModel chordEditor,
            OrnamentEditorViewModel ornamentEditor,
            ScoreEditorViewModel scoreEditor,
            PlaybackViewModel playback,
            SampleLibraryViewModel sampleLibrary,
            IPdfExportService pdfExportService,
            IMidiExportService midiExportService,
            IMidiImportService midiImportService,
            IAppMessenger messenger)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Selection = selection ?? throw new ArgumentNullException(nameof(selection));
            NoteEditor = noteEditor ?? throw new ArgumentNullException(nameof(noteEditor));
            TieEditor = tieEditor ?? throw new ArgumentNullException(nameof(tieEditor));
            MeasureNavigation = measureNavigation ?? throw new ArgumentNullException(nameof(measureNavigation));
            MeasureContent = measureContent ?? throw new ArgumentNullException(nameof(measureContent));
            ChordEditor = chordEditor ?? throw new ArgumentNullException(nameof(chordEditor));
            OrnamentEditor = ornamentEditor ?? throw new ArgumentNullException(nameof(ornamentEditor));
            ScoreEditor = scoreEditor ?? throw new ArgumentNullException(nameof(scoreEditor));
            Playback = playback ?? throw new ArgumentNullException(nameof(playback));
            SampleLibrary = sampleLibrary ?? throw new ArgumentNullException(nameof(sampleLibrary));
            _pdfExportService = pdfExportService ?? throw new ArgumentNullException(nameof(pdfExportService));
            _midiExportService = midiExportService ?? throw new ArgumentNullException(nameof(midiExportService));
            _midiImportService = midiImportService ?? throw new ArgumentNullException(nameof(midiImportService));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            NewScoreCommand = new RelayCommand(NewScore);
            OpenScoreCommand = new RelayCommand(() => RequestOpenScore?.Invoke(this, EventArgs.Empty));
            SaveScoreCommand = new RelayCommand(
                () => RequestSaveScore?.Invoke(this, EventArgs.Empty),
                () => Document.IsDirty || !string.IsNullOrEmpty(Document.CurrentFilePath));
            SaveAsScoreCommand = new RelayCommand(() => RequestSaveAsScore?.Invoke(this, EventArgs.Empty));
            ExportPdfCommand = new RelayCommand(() => RequestExportPdf?.Invoke(this, EventArgs.Empty));
            ExportMidiCommand = new RelayCommand(() => RequestExportMidi?.Invoke(this, EventArgs.Empty));
            ImportMidiCommand = new RelayCommand(() => RequestImportMidi?.Invoke(this, EventArgs.Empty));
            RequestTransposeDialogCommand = new RelayCommand(() => RequestTransposeDialog?.Invoke(this, EventArgs.Empty));

            _messenger.Register<MainViewModel, StatusChangedMessage>(this, OnStatusChanged);
            _messenger.Register<MainViewModel, ScoreEditedMessage>(this, OnScoreEdited);
            _messenger.Register<MainViewModel, ScoreLoadedMessage>(this, OnScoreLoaded);
        }

        public ScoreDocumentViewModel Document { get; }

        public ScoreSelectionViewModel Selection { get; }

        public NoteEditorViewModel NoteEditor { get; }

        public TieEditorViewModel TieEditor { get; }

        public MeasureNavigationViewModel MeasureNavigation { get; }

        public MeasureContentViewModel MeasureContent { get; }

        public ChordEditorViewModel ChordEditor { get; }

        public OrnamentEditorViewModel OrnamentEditor { get; }

        public ScoreEditorViewModel ScoreEditor { get; }

        public PlaybackViewModel Playback { get; }

        public SampleLibraryViewModel SampleLibrary { get; }

        public string StatusMessage
        {
            get { return _statusMessage; }
            set { SetProperty(ref _statusMessage, value); }
        }

        public RelayCommand NewScoreCommand { get; }

        public RelayCommand OpenScoreCommand { get; }

        public RelayCommand SaveScoreCommand { get; }

        public RelayCommand SaveAsScoreCommand { get; }

        public RelayCommand ExportPdfCommand { get; }

        public RelayCommand ExportMidiCommand { get; }

        public RelayCommand ImportMidiCommand { get; }

        public RelayCommand RequestTransposeDialogCommand { get; }

        public event EventHandler RequestOpenScore;

        public event EventHandler RequestSaveScore;

        public event EventHandler RequestSaveAsScore;

        public event EventHandler RequestExportPdf;

        public event EventHandler RequestExportMidi;

        public event EventHandler RequestImportMidi;

        public event EventHandler RequestTransposeDialog;

        public void NewScore()
        {
            Playback.Stop();
            TieEditor.CancelTieMode();
            Document.ResetAsNew();
            MeasureNavigation.SyncCurrentMeasureIndex(0);
            Playback.ResetHead();
            StatusMessage = "已新建谱面";
            SaveScoreCommand.NotifyCanExecuteChanged();
        }

        public void HandleSelectionChanged(ScoreSelectionInfo info)
        {
            if (info == null || info.MeasureIndex < 0)
            {
                return;
            }

            Selection.UpdateFrom(info);
            MeasureNavigation.SyncCurrentMeasureIndex(info.MeasureIndex);
            MeasureContent.LoadFromMeasure(info.MeasureIndex);
            ChordEditor.SyncFromSelection();

            if (info.HasNoteSelected && TieEditor.IsTieModeActive)
            {
                TieEditor.TryCompleteTie(info.MeasureIndex, info.NoteIndex);
                return;
            }

            var description = Selection.BuildSelectionDescription();
            if (!string.IsNullOrEmpty(description))
            {
                SetStatus(description);
            }
        }

        public void NotifyScoreEdited(string message, bool markDirty = true)
        {
            if (markDirty)
            {
                Document.MarkDirty();
            }

            SaveScoreCommand.NotifyCanExecuteChanged();
            _messenger.Send(new ScoreEditedMessage(message, markDirty: markDirty));
        }

        public void SetStatus(string message)
        {
            StatusMessage = message ?? string.Empty;
        }

        public ScoreEditResult ExportPdf(string filePath, int renderWidth)
        {
            try
            {
                _pdfExportService.Export(Document.Score, filePath, renderWidth);
                SetStatus("PDF 已导出: " + filePath);
                return ScoreEditResult.WithMessage("PDF 已导出: " + filePath);
            }
            catch (Exception ex)
            {
                SetStatus("PDF 导出失败");
                throw new InvalidOperationException("PDF 导出失败: " + ex.Message, ex);
            }
        }

        public ScoreEditResult ExportMidi(string filePath)
        {
            try
            {
                _midiExportService.Export(Document.Score, filePath);
                SetStatus("MIDI 已导出: " + filePath);
                return ScoreEditResult.WithMessage("MIDI 已导出: " + filePath);
            }
            catch (Exception ex)
            {
                SetStatus("MIDI 导出失败");
                throw new InvalidOperationException("MIDI 导出失败: " + ex.Message, ex);
            }
        }

        public ScoreEditResult ImportMidi(string filePath)
        {
            try
            {
                Playback.Stop();
                var score = _midiImportService.Import(filePath);
                Document.LoadFromMidi(score);
                SetStatus("MIDI 已导入: " + filePath);
                return ScoreEditResult.WithMessage("MIDI 已导入: " + filePath);
            }
            catch (Exception ex)
            {
                SetStatus("MIDI 导入失败");
                throw new InvalidOperationException("MIDI 导入失败: " + ex.Message, ex);
            }
        }

        public void Dispose()
        {
            _messenger.UnregisterAll<MainViewModel>(this);
            Playback.Dispose();
        }

        private void OnStatusChanged(MainViewModel recipient, StatusChangedMessage message)
        {
            recipient.SetStatus(message.Message);
        }

        private void OnScoreEdited(MainViewModel recipient, ScoreEditedMessage message)
        {
            if (message.MarkDirty)
            {
                recipient.Document.MarkDirty();
            }

            recipient.SaveScoreCommand.NotifyCanExecuteChanged();
            if (!string.IsNullOrEmpty(message.Message))
            {
                recipient.SetStatus(message.Message);
            }
        }

        private void OnScoreLoaded(MainViewModel recipient, ScoreLoadedMessage message)
        {
            recipient.SaveScoreCommand.NotifyCanExecuteChanged();
            recipient.OnPropertyChanged(nameof(recipient.Document));
        }
    }
}
