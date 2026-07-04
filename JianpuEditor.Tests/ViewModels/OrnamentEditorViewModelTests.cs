using System.Collections.Generic;
using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.Tests.Helpers;
using JianpuEditor.ViewModels;
using Xunit;

namespace JianpuEditor.Tests.ViewModels
{
    public sealed class OrnamentEditorViewModelTests
    {
        [Fact]
        public void AddOrnament_BindsToSelectedNote()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(ScoreTestHelper.Note(1));
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });
            var editor = ViewModelTestHelper.CreateOrnamentEditor(document, selection, messenger, history);

            var result = editor.AddOrnament(OrnamentType.Trill);

            Assert.True(result.Changed);
            Assert.Single(document.Score.Measures[0].Ornaments);
            Assert.Equal(OrnamentType.Trill, document.Score.Measures[0].Ornaments[0].Type);
            Assert.Equal(0, document.Score.Measures[0].Ornaments[0].NoteIndex);
        }

        [Fact]
        public void AddOrnament_AppliesToMultipleSelectedNotes()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.AddRange(new[]
            {
                ScoreTestHelper.Note(1),
                ScoreTestHelper.Note(2),
                ScoreTestHelper.Note(3)
            });
            selection.UpdateFrom(new ScoreSelectionInfo
            {
                SelectedNotes = new List<ScoreNoteRef>
                {
                    new ScoreNoteRef(0, 0),
                    new ScoreNoteRef(0, 2)
                }
            });
            var editor = ViewModelTestHelper.CreateOrnamentEditor(document, selection, messenger, history);

            var result = editor.AddOrnament(OrnamentType.Turn);

            Assert.True(result.Changed);
            Assert.Equal(2, document.Score.Measures[0].Ornaments.Count);
            Assert.Contains(document.Score.Measures[0].Ornaments, item => item.NoteIndex == 0 && item.Type == OrnamentType.Turn);
            Assert.Contains(document.Score.Measures[0].Ornaments, item => item.NoteIndex == 2 && item.Type == OrnamentType.Turn);
        }

        [Fact]
        public void AddOrnament_UndoRestoresPreviousState()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(ScoreTestHelper.Note(1));
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });
            var editor = ViewModelTestHelper.CreateOrnamentEditor(document, selection, messenger, history);

            editor.AddOrnament(OrnamentType.Fermata);
            history.Undo();

            Assert.Empty(document.Score.Measures[0].Ornaments);
        }

        [Fact]
        public void AddOrnament_SecondClickRemovesSameType()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(ScoreTestHelper.Note(1));
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });
            var editor = ViewModelTestHelper.CreateOrnamentEditor(document, selection, messenger, history);

            editor.AddOrnament(OrnamentType.Trill);
            var result = editor.AddOrnament(OrnamentType.Trill);

            Assert.True(result.Changed);
            Assert.Empty(document.Score.Measures[0].Ornaments);
        }

        [Fact]
        public void AddOrnament_SecondClickRemovesOnlyMatchingType()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(ScoreTestHelper.Note(1));
            OrnamentService.TryAddOrnament(document.Score.Measures[0], 0, OrnamentType.Trill);
            OrnamentService.TryAddOrnament(document.Score.Measures[0], 0, OrnamentType.Fermata);
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });
            var editor = ViewModelTestHelper.CreateOrnamentEditor(document, selection, messenger, history);

            editor.AddOrnament(OrnamentType.Trill);

            Assert.Single(document.Score.Measures[0].Ornaments);
            Assert.Equal(OrnamentType.Fermata, document.Score.Measures[0].Ornaments[0].Type);
        }

        [Fact]
        public void TryRemoveOrnamentsForSelection_RemovesBoundOrnaments()
        {
            var (document, selection, messenger, history) = ViewModelTestHelper.CreateDocumentWithSelection();
            document.EnsureMeasures();
            document.Score.Measures[0].MelodyNotes.Add(ScoreTestHelper.Note(1));
            OrnamentService.TryAddOrnament(document.Score.Measures[0], 0, OrnamentType.GraceNote);
            selection.UpdateFrom(new ScoreSelectionInfo { MeasureIndex = 0, NoteIndex = 0 });
            var editor = ViewModelTestHelper.CreateOrnamentEditor(document, selection, messenger, history);

            var result = editor.TryRemoveOrnamentsForSelection();

            Assert.True(result.Changed);
            Assert.Empty(document.Score.Measures[0].Ornaments);
        }
    }
}
