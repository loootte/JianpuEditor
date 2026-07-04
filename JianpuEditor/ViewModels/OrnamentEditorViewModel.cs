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
            var message = refs.Count > 1
                ? "已为 " + refs.Count + " 个音符添加装饰音「" + glyph + "」"
                : "已添加装饰音「" + glyph + "」";
            var commands = BuildAddCommands(refs, type, message);
            if (commands.Count == 0)
            {
                return ScoreEditResult.Unchanged;
            }

            return ExecuteCommands("添加装饰音", commands, message);
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

        private List<INoteEditCommand> BuildAddCommands(
            IReadOnlyList<ScoreNoteRef> refs,
            OrnamentType type,
            string message)
        {
            var commands = new List<INoteEditCommand>();
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

                    OrnamentService.TryAddOrnament(CreateWorkingMeasure(measure, working), noteIndex, type);
                }

                if (!OrnamentsChanged(oldOrnaments, working))
                {
                    continue;
                }

                commands.Add(new ModifyMeasureOrnamentsCommand(
                    _document.Score,
                    _messenger,
                    group.Key,
                    oldOrnaments,
                    working,
                    "添加装饰音",
                    message));
            }

            return commands;
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
