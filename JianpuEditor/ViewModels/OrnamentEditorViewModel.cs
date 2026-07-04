using System;
using System.Collections.Generic;
using System.Linq;
using JianpuEditor.Core.Abstractions;
using JianpuEditor.Core.Messaging;
using JianpuEditor.Core.Messaging.Messages;
using JianpuEditor.Models;
using JianpuEditor.Services;
using JianpuEditor.Services.EditCommands;
using JianpuEditor.Services.NoteEditCommands;

namespace JianpuEditor.ViewModels
{
    public sealed class OrnamentEditorViewModel
    {
        private readonly ScoreDocumentViewModel _document;
        private readonly ScoreSelectionViewModel _selection;
        private readonly IEditCommandHistory _history;
        private readonly IAppMessenger _messenger;

        public OrnamentEditorViewModel(
            ScoreDocumentViewModel document,
            ScoreSelectionViewModel selection,
            IEditCommandHistory history,
            IAppMessenger messenger)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public ScoreEditResult AddOrnament(OrnamentType type)
        {
            if (type == OrnamentType.Unknown)
            {
                return ScoreEditResult.Unchanged;
            }

            var refs = GetSelectedNoteRefs();
            if (refs.Count == 0)
            {
                _messenger.Send(new StatusChangedMessage("请先选中音符"));
                return ScoreEditResult.Unchanged;
            }

            _document.EnsureMeasures();
            var glyph = OrnamentService.GetPlaceholderGlyph(type);
            var commands = BuildToggleCommands(refs, type, glyph, out var description, out var message);
            if (commands.Count == 0)
            {
                return ScoreEditResult.Unchanged;
            }

            return ExecuteCommands(description, commands, message);
        }

        public ScoreEditResult TryRemoveOrnamentsForSelection()
        {
            if (!_selection.HasNoteSelected)
            {
                return ScoreEditResult.Unchanged;
            }

            var refs = GetSelectedNoteRefs();
            if (refs.Count == 0)
            {
                return ScoreEditResult.Unchanged;
            }

            _document.EnsureMeasures();
            var removedCount = 0;
            foreach (var group in refs.GroupBy(item => item.MeasureIndex))
            {
                if (group.Key < 0 || group.Key >= _document.Score.Measures.Count)
                {
                    continue;
                }

                var measure = _document.Score.Measures[group.Key];
                foreach (var noteIndex in group.Select(item => item.NoteIndex).Distinct())
                {
                    if (OrnamentService.TryRemoveForNote(measure, noteIndex))
                    {
                        removedCount++;
                    }
                }
            }

            if (removedCount == 0)
            {
                return ScoreEditResult.Unchanged;
            }

            var message = removedCount > 1
                ? "已删除 " + removedCount + " 个音符的装饰音"
                : "已删除装饰音";
            return ScoreEditResult.WithMessage(message);
        }

        private ScoreEditResult ExecuteCommands(
            string description,
            IReadOnlyList<INoteEditCommand> commands,
            string message)
        {
            if (commands.Count == 1)
            {
                return EditCommandHelper.Execute(_history, commands[0]);
            }

            _history.Execute(new CompositeEditCommand(description, commands.Cast<IEditCommand>().ToList()));
            _messenger.Send(new ScoreEditedMessage(message, markDirty: true));
            return ScoreEditResult.WithMessage(message);
        }

        private List<INoteEditCommand> BuildToggleCommands(
            IReadOnlyList<ScoreNoteRef> refs,
            OrnamentType type,
            string glyph,
            out string description,
            out string message)
        {
            var pending = new List<(int MeasureIndex, List<JianpuOrnament> OldOrnaments, List<JianpuOrnament> NewOrnaments)>();
            var addedCount = 0;
            var removedCount = 0;
            foreach (var group in refs.GroupBy(item => item.MeasureIndex))
            {
                if (group.Key < 0 || group.Key >= _document.Score.Measures.Count)
                {
                    continue;
                }

                var measure = _document.Score.Measures[group.Key];
                var oldOrnaments = OrnamentService.CloneOrnaments(measure.Ornaments);
                var working = OrnamentService.CloneOrnaments(measure.Ornaments);
                foreach (var noteIndex in group.Select(item => item.NoteIndex).Distinct())
                {
                    if (noteIndex < 0 || noteIndex >= measure.MelodyNotes.Count)
                    {
                        continue;
                    }

                    var workingMeasure = CreateWorkingMeasure(measure, working);
                    if (OrnamentService.HasOrnament(workingMeasure, noteIndex, type))
                    {
                        if (OrnamentService.TryRemoveForNote(workingMeasure, noteIndex, type))
                        {
                            removedCount++;
                        }
                    }
                    else if (OrnamentService.TryAddOrnament(workingMeasure, noteIndex, type))
                    {
                        addedCount++;
                    }
                }

                if (!OrnamentsChanged(oldOrnaments, working))
                {
                    continue;
                }

                pending.Add((group.Key, oldOrnaments, working));
            }

            description = removedCount > 0 && addedCount == 0
                ? "删除装饰音"
                : addedCount > 0 && removedCount == 0
                    ? "添加装饰音"
                    : "切换装饰音";
            message = BuildToggleMessage(glyph, refs.Count, addedCount, removedCount);
            var commands = new List<INoteEditCommand>();
            foreach (var change in pending)
            {
                commands.Add(new ModifyMeasureOrnamentsCommand(
                    _document.Score,
                    _messenger,
                    change.MeasureIndex,
                    change.OldOrnaments,
                    change.NewOrnaments,
                    description,
                    message));
            }

            return commands;
        }

        private static string BuildToggleMessage(string glyph, int selectionCount, int addedCount, int removedCount)
        {
            if (removedCount > 0 && addedCount == 0)
            {
                return removedCount > 1 || selectionCount > 1
                    ? "已取消 " + removedCount + " 个音符的装饰音「" + glyph + "」"
                    : "已取消装饰音「" + glyph + "」";
            }

            if (addedCount > 0 && removedCount == 0)
            {
                return addedCount > 1 || selectionCount > 1
                    ? "已为 " + addedCount + " 个音符添加装饰音「" + glyph + "」"
                    : "已添加装饰音「" + glyph + "」";
            }

            return "已更新装饰音「" + glyph + "」";
        }

        private static JianpuMeasure CreateWorkingMeasure(JianpuMeasure measure, List<JianpuOrnament> working)
        {
            return new JianpuMeasure
            {
                MelodyNotes = measure.MelodyNotes,
                Ornaments = working
            };
        }

        private static bool OrnamentsChanged(
            IReadOnlyList<JianpuOrnament> before,
            IReadOnlyList<JianpuOrnament> after)
        {
            if (before.Count != after.Count)
            {
                return true;
            }

            for (var i = 0; i < before.Count; i++)
            {
                var left = before[i];
                var right = after[i];
                if (left.Type != right.Type
                    || left.NoteIndex != right.NoteIndex
                    || Math.Abs(left.BeatPosition - right.BeatPosition) > 0.001)
                {
                    return true;
                }
            }

            return false;
        }

        private List<ScoreNoteRef> GetSelectedNoteRefs()
        {
            if (!_selection.HasNoteSelected)
            {
                return new List<ScoreNoteRef>();
            }

            if (_selection.SelectedNotes != null && _selection.SelectedNotes.Count > 0)
            {
                return _selection.SelectedNotes.ToList();
            }

            if (_selection.NoteIndex >= 0 && _selection.MeasureIndex >= 0)
            {
                return new List<ScoreNoteRef>
                {
                    new ScoreNoteRef(_selection.MeasureIndex, _selection.NoteIndex)
                };
            }

            return new List<ScoreNoteRef>();
        }
    }
}
