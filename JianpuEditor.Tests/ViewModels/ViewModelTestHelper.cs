using JianpuEditor.Core.Messaging;
using JianpuEditor.Services;
using JianpuEditor.ViewModels;

namespace JianpuEditor.Tests.ViewModels
{
    internal static class ViewModelTestHelper
    {
        public static ScoreDocumentViewModel CreateDocument()
        {
            return new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), new AppMessenger());
        }

        public static (ScoreDocumentViewModel document, ScoreSelectionViewModel selection, IAppMessenger messenger)
            CreateDocumentWithSelection()
        {
            var messenger = new AppMessenger();
            var document = new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), messenger);
            var selection = new ScoreSelectionViewModel(document);
            return (document, selection, messenger);
        }

        public static MainViewModel CreateMainViewModel()
        {
            var messenger = new AppMessenger();
            var document = new ScoreDocumentViewModel(new ScoreFileServiceAdapter(), messenger);
            var selection = new ScoreSelectionViewModel(document);
            var noteEditor = new NoteEditorViewModel(document, selection, messenger);
            var tieEditor = new TieEditorViewModel(document, messenger);
            var measureNavigation = new MeasureNavigationViewModel(document, selection, messenger);
            var measureContent = new MeasureContentViewModel(document, measureNavigation, messenger);
            var chordEditor = new ChordEditorViewModel(document, selection, messenger);
            var scoreEditor = new ScoreEditorViewModel(document, selection, measureNavigation, chordEditor, messenger);
            var playback = new PlaybackViewModel(document, new FakePlaybackService(), messenger);
            var sampleLibrary = new SampleLibraryViewModel(document, messenger);
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
                messenger);
        }
    }
}
