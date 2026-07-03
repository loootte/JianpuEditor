using JianpuEditor.Models;
using JianpuEditor.Rendering;
using JianpuEditor.Services;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public class MeasureNavigationViewModelTests
    {
        [Fact]
        public void AddMeasure_IncreasesMeasureCount()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            document.EnsureMeasures();

            navigation.AddMeasure();

            Assert.Equal(2, navigation.MeasureCount);
        }

        [Fact]
        public void AddMeasure_WithPlaceholdersEnabled_FillsFourQuarterNotes()
        {
            using var scope = EditorPreferenceScope.EnableFillPlaceholders();
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            document.EnsureMeasures();

            var result = navigation.AddMeasure();

            Assert.Equal(4, document.Score.Measures[1].MelodyNotes.Count);
            Assert.Equal(1, result.SelectNoteMeasureIndex);
            Assert.Equal(0, result.SelectNoteIndex);
        }

        [Fact]
        public void AddMeasure_WithPlaceholdersDisabled_CreatesEmptyMeasure()
        {
            using var scope = EditorPreferenceScope.DisableFillPlaceholders();
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            document.EnsureMeasures();

            var result = navigation.AddMeasure();

            Assert.Empty(document.Score.Measures[1].MelodyNotes);
            Assert.Null(result.SelectNoteMeasureIndex);
            Assert.Null(result.SelectNoteIndex);
        }

        [Fact]
        public void AddMeasureWithPlaceholders_AlwaysFillsRegardlessOfSetting()
        {
            using var scope = EditorPreferenceScope.DisableFillPlaceholders();
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            document.EnsureMeasures();

            var result = navigation.AddMeasureWithPlaceholders();

            Assert.Equal(MeasurePlaceholderService.DefaultPlaceholderCount, document.Score.Measures[1].MelodyNotes.Count);
            Assert.Equal(1, result.SelectNoteMeasureIndex);
            Assert.Equal(0, result.SelectNoteIndex);
        }

        [Fact]
        public void AddMeasure_WithPlaceholders_Undo_RestoresPreviousState()
        {
            using var scope = EditorPreferenceScope.EnableFillPlaceholders();
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            document.EnsureMeasures();

            navigation.AddMeasure();
            history.Undo();

            Assert.Equal(1, navigation.MeasureCount);
            Assert.Empty(document.Score.Measures[0].MelodyNotes);
        }

        private sealed class EditorPreferenceScope : System.IDisposable
        {
            private readonly bool _previous;

            private EditorPreferenceScope(bool enabled)
            {
                _previous = AppTheme.FillMeasurePlaceholdersOnAdd;
                AppTheme.SetFillMeasurePlaceholdersOnAdd(enabled, persist: false);
            }

            public static EditorPreferenceScope EnableFillPlaceholders()
            {
                return new EditorPreferenceScope(true);
            }

            public static EditorPreferenceScope DisableFillPlaceholders()
            {
                return new EditorPreferenceScope(false);
            }

            public void Dispose()
            {
                AppTheme.SetFillMeasurePlaceholdersOnAdd(_previous, persist: false);
            }
        }

        [Fact]
        public void DuplicateMeasures_CopiesSelectedRange()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);
            document.EnsureMeasures();
            document.Score.Measures.Add(new JianpuMeasure { LyricText = "第二小节" });
            selection.UpdateFrom(new ScoreSelectionInfo
            {
                MeasureIndex = 0,
                SelectedMeasureIndices = new[] { 0 }
            });

            var result = navigation.DuplicateMeasures();

            Assert.True(result.Changed);
            Assert.Equal(3, document.Score.Measures.Count);
        }

        [Fact]
        public void NormalizeMeasureRange_SwapsWhenFromGreaterThanTo()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            var navigation = ViewModelTestHelper.CreateMeasureNavigation(document, selection, messenger, history);

            var range = navigation.NormalizeMeasureRange(3, 1, fromChanged: true);

            Assert.Equal(2, range.fromIndex);
            Assert.Equal(2, range.toIndex);
        }
    }
}
