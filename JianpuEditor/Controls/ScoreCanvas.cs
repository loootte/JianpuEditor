using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using JianpuEditor.Models;
using JianpuEditor.Rendering;

namespace JianpuEditor.Controls
{
    public sealed class ScoreSelectionChangedEventArgs : EventArgs
    {
        public int MeasureIndex { get; set; } = -1;

        public int NoteIndex { get; set; } = -1;

        public int InsertIndex { get; set; } = -1;

        public bool HasNoteSelected
        {
            get { return NoteIndex >= 0; }
        }

        public bool HasGapSelected
        {
            get { return InsertIndex >= 0; }
        }

        public IReadOnlyList<int> SelectedMeasureIndices { get; set; } = new int[0];
    }

    public sealed class ScoreCanvas : Panel
    {
        private readonly JianpuRenderer _renderer = new JianpuRenderer();
        private readonly Panel _contentPanel;
        private readonly Font _inlineTextFont = new Font("Arial", 20f, FontStyle.Bold);
        private JianpuScore _score = new JianpuScore();
        private TextBox _inlineEditor;
        private int _selectedMeasureIndex = -1;
        private int _selectedNoteIndex = -1;
        private int _selectedInsertIndex = -1;
        private int _editingMeasureIndex = -1;
        private bool _editingSecondaryText;
        private readonly List<int> _selectedMeasureIndices = new List<int>();
        private int _measureSelectionAnchor = -1;

        public ScoreCanvas()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(245, 245, 245);
            BorderStyle = BorderStyle.FixedSingle;
            AutoScroll = true;

            _contentPanel = new Panel
            {
                BackColor = Color.White,
                Location = new Point(0, 0)
            };
            _contentPanel.Paint += OnContentPaint;
            _contentPanel.MouseClick += OnContentClick;
            Controls.Add(_contentPanel);
        }

        public event EventHandler<ScoreSelectionChangedEventArgs> SelectionChanged;

        public event EventHandler MeasureTextEdited;

        public JianpuScore Score
        {
            get { return _score; }
            set
            {
                CommitInlineEdit();
                _score = value ?? new JianpuScore();
                if (_score.Measures == null || _score.Measures.Count == 0)
                {
                    _score.Measures = new System.Collections.Generic.List<JianpuMeasure> { new JianpuMeasure() };
                }

                _selectedMeasureIndex = Math.Min(Math.Max(_selectedMeasureIndex, 0), _score.Measures.Count - 1);
                SetSelectedMeasures(new[] { _selectedMeasureIndex }, _selectedMeasureIndex, false);
                ClearMelodySelection();
                UpdateContentSize();
                InvalidateSelection();
            }
        }

        public IReadOnlyList<int> GetSelectedMeasureIndices()
        {
            return _selectedMeasureIndices.ToArray();
        }

        public int SelectedMeasureIndex
        {
            get { return _selectedMeasureIndex; }
        }

        public int SelectedNoteIndex
        {
            get { return _selectedNoteIndex; }
        }

        public int SelectedInsertIndex
        {
            get { return _selectedInsertIndex; }
        }

        public bool HasMelodySelection
        {
            get { return _selectedNoteIndex >= 0 || _selectedInsertIndex >= 0; }
        }

        public void NavigateSelection(bool moveLeft)
        {
            CommitInlineEdit();
            EnsureMeasures();

            var measureCount = _score.Measures.Count;
            if (measureCount == 0)
            {
                return;
            }

            var lastMeasureIndex = measureCount - 1;

            if (!HasMelodySelection)
            {
                if (moveLeft)
                {
                    SelectGap(0, 0);
                }
                else
                {
                    SelectGap(lastMeasureIndex, _score.Measures[lastMeasureIndex].MelodyNotes.Count);
                }

                return;
            }

            if (_selectedNoteIndex >= 0)
            {
                if (moveLeft)
                {
                    SelectGap(_selectedMeasureIndex, _selectedNoteIndex);
                }
                else
                {
                    SelectGap(_selectedMeasureIndex, _selectedNoteIndex + 1);
                }

                return;
            }

            if (_selectedInsertIndex >= 0)
            {
                var measureIndex = _selectedMeasureIndex;
                var insertIndex = _selectedInsertIndex;
                var noteCount = _score.Measures[measureIndex].MelodyNotes.Count;

                if (moveLeft)
                {
                    if (insertIndex > 0)
                    {
                        SelectNote(measureIndex, insertIndex - 1);
                    }
                    else if (measureIndex > 0)
                    {
                        var previousMeasure = measureIndex - 1;
                        SelectGap(previousMeasure, _score.Measures[previousMeasure].MelodyNotes.Count);
                    }
                }
                else if (insertIndex < noteCount)
                {
                    SelectNote(measureIndex, insertIndex);
                }
                else if (measureIndex < lastMeasureIndex)
                {
                    SelectGap(measureIndex + 1, 0);
                }
            }
        }

        public void ClearMelodySelection()
        {
            _selectedNoteIndex = -1;
            _selectedInsertIndex = -1;
            InvalidateSelection();
        }

        public void SelectNote(int measureIndex, int noteIndex)
        {
            CommitInlineEdit();
            if (_score.Measures == null || measureIndex < 0 || measureIndex >= _score.Measures.Count)
            {
                return;
            }

            var notes = _score.Measures[measureIndex].MelodyNotes;
            if (noteIndex < 0 || noteIndex >= notes.Count)
            {
                return;
            }

            SetSelectedMeasures(new[] { measureIndex }, measureIndex, false);
            _measureSelectionAnchor = measureIndex;
            _selectedMeasureIndex = measureIndex;
            _selectedNoteIndex = noteIndex;
            _selectedInsertIndex = -1;
            RaiseSelectionChanged();
            InvalidateSelection();
        }

        public void SelectGap(int measureIndex, int insertIndex)
        {
            CommitInlineEdit();
            if (_score.Measures == null || measureIndex < 0 || measureIndex >= _score.Measures.Count)
            {
                return;
            }

            var noteCount = _score.Measures[measureIndex].MelodyNotes.Count;
            if (insertIndex < 0 || insertIndex > noteCount)
            {
                return;
            }

            SetSelectedMeasures(new[] { measureIndex }, measureIndex, false);
            _measureSelectionAnchor = measureIndex;
            _selectedMeasureIndex = measureIndex;
            _selectedNoteIndex = -1;
            _selectedInsertIndex = insertIndex;
            RaiseSelectionChanged();
            InvalidateSelection();
        }

        public void SelectMeasure(int measureIndex)
        {
            SelectSingleMeasure(measureIndex, true);
        }

        public void SelectMeasureRange(int startIndex, int endIndex)
        {
            CommitInlineEdit();
            EnsureMeasures();

            if (_score.Measures.Count == 0)
            {
                return;
            }

            var lo = Math.Max(0, Math.Min(startIndex, endIndex));
            var hi = Math.Min(_score.Measures.Count - 1, Math.Max(startIndex, endIndex));
            var indices = Enumerable.Range(lo, hi - lo + 1);
            var primary = _selectedMeasureIndex >= lo && _selectedMeasureIndex <= hi
                ? _selectedMeasureIndex
                : lo;
            SetSelectedMeasures(indices, primary, true);
            ClearMelodySelection();
            RaiseSelectionChanged();
            InvalidateSelection();
        }

        public void SetSelectedMeasures(IReadOnlyList<int> indices, int primaryMeasureIndex)
        {
            CommitInlineEdit();
            EnsureMeasures();
            SetSelectedMeasures(indices, primaryMeasureIndex, true);
            if (indices != null && indices.Count > 0)
            {
                _measureSelectionAnchor = indices[0];
            }

            ClearMelodySelection();
            RaiseSelectionChanged();
            InvalidateSelection();
        }

        public void RefreshScore()
        {
            UpdateContentSize();
            InvalidateSelection();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateContentSize();
        }

        private int GetDrawWidth()
        {
            return Math.Max(ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8, 600);
        }

        private void OnContentPaint(object sender, PaintEventArgs e)
        {
            _renderer.Draw(
                e.Graphics,
                _score,
                GetDrawWidth(),
                _selectedMeasureIndex,
                _selectedNoteIndex,
                _selectedInsertIndex,
                _selectedMeasureIndices);
        }

        private void OnContentClick(object sender, MouseEventArgs e)
        {
            if (_inlineEditor != null)
            {
                var editorBounds = _inlineEditor.Bounds;
                if (editorBounds.Contains(e.Location))
                {
                    return;
                }

                CommitInlineEdit();
            }

            var hit = _renderer.HitTest(_score, GetDrawWidth(), e.Location);
            if (hit.HitType == ScoreHitType.None)
            {
                return;
            }

            switch (hit.HitType)
            {
                case ScoreHitType.Note:
                    SelectSingleMeasure(hit.MeasureIndex, false);
                    _selectedNoteIndex = hit.NoteIndex;
                    _selectedInsertIndex = -1;
                    RaiseSelectionChanged();
                    InvalidateSelection();
                    break;
                case ScoreHitType.Gap:
                    SelectSingleMeasure(hit.MeasureIndex, false);
                    _selectedNoteIndex = -1;
                    _selectedInsertIndex = hit.InsertIndex;
                    RaiseSelectionChanged();
                    InvalidateSelection();
                    break;
                case ScoreHitType.SecondaryText:
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        HandleMeasureSelectionClick(hit.MeasureIndex);
                        break;
                    }

                    SelectSingleMeasure(hit.MeasureIndex, false);
                    RaiseSelectionChanged();
                    StartInlineEdit(hit.MeasureIndex, true, hit.Bounds);
                    break;
                case ScoreHitType.LyricText:
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        HandleMeasureSelectionClick(hit.MeasureIndex);
                        break;
                    }

                    SelectSingleMeasure(hit.MeasureIndex, false);
                    RaiseSelectionChanged();
                    StartInlineEdit(hit.MeasureIndex, false, hit.Bounds);
                    break;
                default:
                    HandleMeasureSelectionClick(hit.MeasureIndex);
                    break;
            }
        }

        private void SelectSingleMeasure(int measureIndex, bool clearMelodySelection)
        {
            CommitInlineEdit();
            if (_score.Measures == null || measureIndex < 0 || measureIndex >= _score.Measures.Count)
            {
                return;
            }

            SetSelectedMeasures(new[] { measureIndex }, measureIndex, false);
            _measureSelectionAnchor = measureIndex;
            if (clearMelodySelection)
            {
                ClearMelodySelection();
                RaiseSelectionChanged();
                InvalidateSelection();
            }
        }

        private void HandleMeasureSelectionClick(int measureIndex)
        {
            CommitInlineEdit();
            EnsureMeasures();
            if (measureIndex < 0 || measureIndex >= _score.Measures.Count)
            {
                return;
            }

            var modifiers = Control.ModifierKeys;
            if ((modifiers & Keys.Shift) == Keys.Shift && _measureSelectionAnchor >= 0)
            {
                SelectMeasureRange(_measureSelectionAnchor, measureIndex);
                _selectedMeasureIndex = measureIndex;
                ClearMelodySelection();
                RaiseSelectionChanged();
                InvalidateSelection();
                return;
            }

            if ((modifiers & Keys.Control) == Keys.Control)
            {
                var next = new List<int>(_selectedMeasureIndices);
                if (next.Contains(measureIndex))
                {
                    if (next.Count > 1)
                    {
                        next.Remove(measureIndex);
                    }
                }
                else
                {
                    next.Add(measureIndex);
                }

                next.Sort();
                SetSelectedMeasures(next, measureIndex, false);
                _measureSelectionAnchor = measureIndex;
                ClearMelodySelection();
                RaiseSelectionChanged();
                InvalidateSelection();
                return;
            }

            SelectSingleMeasure(measureIndex, true);
        }

        private void SetSelectedMeasures(IEnumerable<int> indices, int primaryMeasureIndex, bool validatePrimary)
        {
            _selectedMeasureIndices.Clear();
            if (indices != null)
            {
                foreach (var index in indices.Distinct().OrderBy(i => i))
                {
                    if (index >= 0 && index < _score.Measures.Count)
                    {
                        _selectedMeasureIndices.Add(index);
                    }
                }
            }

            if (_selectedMeasureIndices.Count == 0 && _score.Measures.Count > 0)
            {
                _selectedMeasureIndices.Add(0);
            }

            if (validatePrimary && primaryMeasureIndex >= 0 && primaryMeasureIndex < _score.Measures.Count)
            {
                _selectedMeasureIndex = primaryMeasureIndex;
            }
            else if (_selectedMeasureIndices.Count > 0)
            {
                _selectedMeasureIndex = _selectedMeasureIndices.Contains(primaryMeasureIndex)
                    ? primaryMeasureIndex
                    : _selectedMeasureIndices[0];
            }
        }

        private void StartInlineEdit(int measureIndex, bool secondary, Rectangle bounds)
        {
            CommitInlineEdit();

            _editingMeasureIndex = measureIndex;
            _editingSecondaryText = secondary;
            var measure = _score.Measures[measureIndex];
            var text = secondary ? measure.SecondaryText : measure.LyricText;

            _inlineEditor = new TextBox
            {
                Bounds = bounds,
                Text = text ?? string.Empty,
                BorderStyle = BorderStyle.FixedSingle,
                Font = _inlineTextFont,
                BackColor = Color.FromArgb(255, 255, 240)
            };
            _inlineEditor.KeyDown += OnInlineEditorKeyDown;
            _inlineEditor.LostFocus += OnInlineEditorLostFocus;
            _contentPanel.Controls.Add(_inlineEditor);
            _inlineEditor.Focus();
            _inlineEditor.SelectAll();
        }

        private void OnInlineEditorKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CommitInlineEdit();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                CancelInlineEdit();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void OnInlineEditorLostFocus(object sender, EventArgs e)
        {
            if (_inlineEditor == null)
            {
                return;
            }

            if (_inlineEditor.Focused)
            {
                return;
            }

            CommitInlineEdit();
        }

        private void CommitInlineEdit()
        {
            if (_inlineEditor == null || _editingMeasureIndex < 0)
            {
                return;
            }

            var text = _inlineEditor.Text ?? string.Empty;
            var measure = _score.Measures[_editingMeasureIndex];
            if (_editingSecondaryText)
            {
                measure.SecondaryText = text;
            }
            else
            {
                measure.LyricText = text;
            }

            RemoveInlineEditor();
            UpdateContentSize();
            InvalidateSelection();
            MeasureTextEdited?.Invoke(this, EventArgs.Empty);
        }

        private void CancelInlineEdit()
        {
            RemoveInlineEditor();
            InvalidateSelection();
        }

        private void RemoveInlineEditor()
        {
            if (_inlineEditor != null)
            {
                _inlineEditor.KeyDown -= OnInlineEditorKeyDown;
                _inlineEditor.LostFocus -= OnInlineEditorLostFocus;
                _contentPanel.Controls.Remove(_inlineEditor);
                _inlineEditor.Dispose();
                _inlineEditor = null;
            }

            _editingMeasureIndex = -1;
        }

        private void RaiseSelectionChanged()
        {
            SelectionChanged?.Invoke(this, new ScoreSelectionChangedEventArgs
            {
                MeasureIndex = _selectedMeasureIndex,
                NoteIndex = _selectedNoteIndex,
                InsertIndex = _selectedInsertIndex,
                SelectedMeasureIndices = _selectedMeasureIndices.ToArray()
            });
        }

        private void InvalidateSelection()
        {
            _contentPanel.Invalidate();
            Invalidate();
        }

        private void UpdateContentSize()
        {
            var drawWidth = GetDrawWidth();
            var size = _renderer.MeasureScore(_score, drawWidth);
            _contentPanel.Size = new Size(Math.Max(drawWidth, size.Width), Math.Max(360, size.Height));
            AutoScrollMinSize = _contentPanel.Size;
        }

        private void EnsureMeasures()
        {
            if (_score.Measures == null || _score.Measures.Count == 0)
            {
                _score.Measures = new System.Collections.Generic.List<JianpuMeasure> { new JianpuMeasure() };
            }
        }
    }
}