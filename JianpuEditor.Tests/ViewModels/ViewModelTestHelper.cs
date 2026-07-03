using CommunityToolkit.Mvvm.Messaging;
using JianpuEditor.Core.Abstractions;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Services;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Tests.ViewModels
{
    internal static class ViewModelTestHelper
    {
        public static AppMessenger CreateMessenger()
        {
            return new AppMessenger(new WeakReferenceMessenger());
        }

        public static EditCommandHistory CreateHistory(IAppMessenger messenger = null)
        {
            return new EditCommandHistory(messenger ?? CreateMessenger());
        }

        public static ScoreDocumentViewModel CreateDocument(
            IAppMessenger messenger = null,
            IEditCommandHistory history = null)
        {
            messenger = messenger ?? CreateMessenger();
            history = history ?? CreateHistory(messenger);
            return new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), messenger, history);
        }

        public static (ScoreDocumentViewModel document, ScoreSelectionViewModel selection, IAppMessenger messenger, IEditCommandHistory history)
            CreateDocumentWithSelection()
        {
            var messenger = CreateMessenger();
            var history = CreateHistory(messenger);
            var document = new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), messenger, history);
            var selection = new ScoreSelectionViewModel(document);
            return (document, selection, messenger, history);
        }

        public static MeasureNavigationViewModel CreateMeasureNavigation(
            ScoreDocumentViewModel document,
            ScoreSelectionViewModel selection,
            IAppMessenger messenger,
            IEditCommandHistory history = null)
        {
            return new MeasureNavigationViewModel(
                document,
                selection,
                messenger,
                history ?? CreateHistory(messenger));
        }

        public static NoteEditorViewModel CreateNoteEditor(
            ScoreDocumentViewModel document,
            ScoreSelectionViewModel selection,
            IAppMessenger messenger,
            IEditCommandHistory history = null,
            MeasureNavigationViewModel navigation = null)
        {
            navigation = navigation ?? CreateMeasureNavigation(document, selection, messenger, history);
            return new NoteEditorViewModel(
                document,
                selection,
                navigation,
                messenger,
                history ?? CreateHistory(messenger));
        }

        public static ChordEditorViewModel CreateChordEditor(
            ScoreDocumentViewModel document,
            ScoreSelectionViewModel selection,
            MeasureNavigationViewModel navigation,
            IAppMessenger messenger,
            IChordTransposeService transposeService = null,
            IEditCommandHistory history = null)
        {
            return new ChordEditorViewModel(
                document,
                selection,
                navigation,
                transposeService ?? new ChordTransposeServiceAdapter(),
                history ?? CreateHistory(messenger),
                messenger);
        }

        public static TieEditorViewModel CreateTieEditor(
            ScoreDocumentViewModel document,
            MeasureNavigationViewModel navigation,
            IAppMessenger messenger,
            IEditCommandHistory history = null)
        {
            return new TieEditorViewModel(
                document,
                navigation,
                messenger,
                history ?? CreateHistory(messenger));
        }

        public static ScoreEditorViewModel CreateScoreEditor(
            ScoreDocumentViewModel document,
            ScoreSelectionViewModel selection,
            MeasureNavigationViewModel navigation,
            ChordEditorViewModel chordEditor,
            IAppMessenger messenger,
            IEditCommandHistory history = null)
        {
            return new ScoreEditorViewModel(
                document,
                selection,
                navigation,
                chordEditor,
                messenger,
                history ?? CreateHistory(messenger));
        }

        public static MainViewModel CreateMainViewModel()
        {
            var messenger = CreateMessenger();
            var history = CreateHistory(messenger);
            var document = new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), messenger, history);
            var selection = new ScoreSelectionViewModel(document);
            var measureNavigation = new MeasureNavigationViewModel(document, selection, messenger, history);
            var noteEditor = new NoteEditorViewModel(document, selection, measureNavigation, messenger, history);
            var tieEditor = new TieEditorViewModel(document, measureNavigation, messenger, history);
            var measureContent = new MeasureContentViewModel(document, measureNavigation, messenger, history);
            var chordEditor = new ChordEditorViewModel(
                document,
                selection,
                measureNavigation,
                new ChordTransposeServiceAdapter(),
                history,
                messenger);
            var scoreEditor = new ScoreEditorViewModel(
                document,
                selection,
                measureNavigation,
                chordEditor,
                messenger,
                history);
            var playback = new PlaybackViewModel(document, new FakePlaybackService(), messenger);
            var sampleLibrary = new SampleLibraryViewModel(document, new SampleLibraryServiceAdapter(), messenger);
            return new MainViewModel(
                document,
                selection,
                noteEditor,
                tieEditor,
                measureNavigation,
                measureContent,
                chordEditor,
                scoreEditor,
                playback,
                sampleLibrary,
                new FakePdfExportService(),
                new FakeMidiExportService(),
                messenger);
        }

        public static MainViewModel CreateMainViewModel(
            FakePdfExportService pdfExport,
            FakeMidiExportService midiExport)
        {
            var messenger = CreateMessenger();
            var history = CreateHistory(messenger);
            var document = new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), messenger, history);
            var selection = new ScoreSelectionViewModel(document);
            var measureNavigation = new MeasureNavigationViewModel(document, selection, messenger, history);
            var noteEditor = new NoteEditorViewModel(document, selection, measureNavigation, messenger, history);
            var tieEditor = new TieEditorViewModel(document, measureNavigation, messenger, history);
            var measureContent = new MeasureContentViewModel(document, measureNavigation, messenger, history);
            var chordEditor = new ChordEditorViewModel(
                document,
                selection,
                measureNavigation,
                new ChordTransposeServiceAdapter(),
                history,
                messenger);
            var scoreEditor = new ScoreEditorViewModel(
                document,
                selection,
                measureNavigation,
                chordEditor,
                messenger,
                history);
            var playback = new PlaybackViewModel(document, new FakePlaybackService(), messenger);
            var sampleLibrary = new SampleLibraryViewModel(document, new SampleLibraryServiceAdapter(), messenger);
            return new MainViewModel(
                document,
                selection,
                noteEditor,
                tieEditor,
                measureNavigation,
                measureContent,
                chordEditor,
                scoreEditor,
                playback,
                sampleLibrary,
                pdfExport,
                midiExport,
                messenger);
        }
    }
}
