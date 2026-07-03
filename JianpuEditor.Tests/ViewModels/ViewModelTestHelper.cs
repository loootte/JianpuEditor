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

        public static ScoreDocumentViewModel CreateDocument()
        {
            return new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), CreateMessenger());
        }

        public static (ScoreDocumentViewModel document, ScoreSelectionViewModel selection, IAppMessenger messenger)
            CreateDocumentWithSelection()
        {
            var messenger = CreateMessenger();
            var document = new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), messenger);
            var selection = new ScoreSelectionViewModel(document);
            return (document, selection, messenger);
        }

        public static ChordEditorViewModel CreateChordEditor(
            ScoreDocumentViewModel document,
            ScoreSelectionViewModel selection,
            IAppMessenger messenger,
            IChordTransposeService transposeService = null)
        {
            return new ChordEditorViewModel(
                document,
                selection,
                transposeService ?? new ChordTransposeServiceAdapter(),
                messenger);
        }

        public static MainViewModel CreateMainViewModel()
        {
            var messenger = CreateMessenger();
            var document = new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), messenger);
            var selection = new ScoreSelectionViewModel(document);
            var noteEditor = new NoteEditorViewModel(document, selection, messenger);
            var tieEditor = new TieEditorViewModel(document, messenger);
            var measureNavigation = new MeasureNavigationViewModel(document, selection, messenger);
            var measureContent = new MeasureContentViewModel(document, measureNavigation, messenger);
            var chordEditor = CreateChordEditor(document, selection, messenger);
            var scoreEditor = new ScoreEditorViewModel(document, selection, measureNavigation, chordEditor, messenger);
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
            var document = new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), messenger);
            var selection = new ScoreSelectionViewModel(document);
            var noteEditor = new NoteEditorViewModel(document, selection, messenger);
            var tieEditor = new TieEditorViewModel(document, messenger);
            var measureNavigation = new MeasureNavigationViewModel(document, selection, messenger);
            var measureContent = new MeasureContentViewModel(document, measureNavigation, messenger);
            var chordEditor = CreateChordEditor(document, selection, messenger);
            var scoreEditor = new ScoreEditorViewModel(document, selection, measureNavigation, chordEditor, messenger);
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
