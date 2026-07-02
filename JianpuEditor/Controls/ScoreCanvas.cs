using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using JianpuEditor.Models;
using JianpuEditor.Rendering;
using JianpuEditor.Services;

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

        public bool HasTieSelected
        {
            get { return TieIndex >= 0; }
        }

        public int TieIndex { get; set; } = -1;

        public bool HasChordSelected
        {
            get { return ChordMarkerIndex >= 0; }
        }

        public int ChordMeasureIndex { get; set; } = -1;

        public int ChordMarkerIndex { get; set; } = -1;

        public IReadOnlyList<int> SelectedMeasureIndices { get; set; } = new int[0];
    }

    public sealed class ScoreCanvas : Panel
    {
        private readonly JianpuRenderer _renderer = new JianpuRenderer();
        private readonly Panel _contentPanel;
        private readonly Font _inlineTextFont = new Font("Arial", 20f, FontStyle.Bold);
        private JianpuScore _score = new JianpuScore();
        private TextBox _inlineEditor;
        private TextBox _chordInlineEditor;
        private int _editingChordMeasureIndex = -1;
        private int _editingChordMarkerIndex = -1;
        private int _selectedMeasureIndex = -1;
        private int _selectedNoteIndex = -1;
        private int _selectedInsertIndex = -1;
        private int _editingMeasureIndex = -1;
        private readonly List<int> _selectedMeasureIndices = new List<int>();
        private int _measureSelectionAnchor = -1;
        private int _selectedTieIndex = -1;
        private int _selectedChordMeasureIndex = -1;
        private int _selectedChordMarkerIndex = -1;
        private bool _draggingChordMarker;
        private int _dragChordMeasureIndex = -1;
        private int _dragChordMarkerIndex = -1;
        private IReadOnlyList<PlaybackMeasureSegment> _playbackSegments = new PlaybackMeasureSegment[0];
        private double _playbackPositionQuarter;
        private bool _showPlaybackHead;
        private bool _draggingPlaybackHead;
        private bool _playbackHeadDragMoved;
        private int _playbackHeadHitZone = 12;
        private Bitmap _scoreBitmap;
        private bool _scoreBitmapDirty = true;

        public ScoreCanvas()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            BackColor = Color.FromArgb(245, 245, 245);
            BorderStyle = BorderStyle.FixedSingle;
            AutoScroll = true;

            _contentPanel = new Panel
            {
                BackColor = Color.White,
                Location = new Point(0, 0)
            };
            EnableDoubleBuffer(_contentPanel);
            _contentPanel.Paint += OnContentPaint;
            _contentPanel.MouseClick += OnContentClick;
            _contentPanel.MouseDown += OnContentMouseDown;
            _contentPanel.MouseMove += OnContentMouseMove;
            _contentPanel.MouseUp += OnContentMouseUp;
            Controls.Add(_contentPanel);
        }

        public event EventHandler<ScoreSelectionChangedEventArgs> SelectionChanged;

        public event EventHandler MeasureTextEdited;

        public event EventHandler ChordMarkersChanged;

        public event Action<double> PlaybackSeeked;

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

                ChordMarkerService.NormalizeScore(_score);
                _selectedMeasureIndex = Math.Min(Math.Max(_selectedMeasureIndex, 0), _score.Measures.Count - 1);
                SetSelectedMeasures(new[] { _selectedMeasureIndex }, _selectedMeasureIndex, false);
                ClearMelodySelection();
                ClearTieSelection();
                ClearChordSelection();
                UpdateContentSize();
                InvalidateSelection();
            }
        }

        public int SelectedChordMeasureIndex
        {
            get { return _selectedChordMeasureIndex; }
        }

        public int SelectedChordMarkerIndex
        {
            get { return _selectedChordMarkerIndex; }
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

        public int SelectedTieIndex
        {
            get { return _selectedTieIndex; }
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

        public void ClearTieSelection()
        {
            _selectedTieIndex = -1;
            InvalidateSelection();
        }

        public void ClearChordSelection()
        {
            CommitChordInlineEdit();
            _selectedChordMeasureIndex = -1;
            _selectedChordMarkerIndex = -1;
            InvalidateSelection();
        }

        public void SelectChordMarker(int measureIndex, int markerIndex, bool startInlineEdit = true)
        {
            CommitInlineEdit();
            CommitChordInlineEdit();
            if (_score.Measures == null || measureIndex < 0 || measureIndex >= _score.Measures.Count)
            {
                return;
            }

            var measure = _score.Measures[measureIndex];
            ChordMarkerService.NormalizeMeasure(measure);
            if (markerIndex < 0 || markerIndex >= measure.ChordMarkers.Count)
            {
                return;
            }

            SetSelectedMeasures(new[] { measureIndex }, measureIndex, false);
            _measureSelectionAnchor = measureIndex;
            _selectedMeasureIndex = measureIndex;
            _selectedNoteIndex = -1;
            _selectedInsertIndex = -1;
            _selectedTieIndex = -1;
            _selectedChordMeasureIndex = measureIndex;
            _selectedChordMarkerIndex = markerIndex;
            RaiseSelectionChanged();
            InvalidateSelection();
            if (startInlineEdit)
            {
                StartChordInlineEdit(measureIndex, markerIndex);
            }
        }

        public bool TryAddChordToMeasure(int measureIndex, double beatPosition = 0)
        {
            CommitInlineEdit();
            if (_score.Measures == null || measureIndex < 0 || measureIndex >= _score.Measures.Count)
            {
                return false;
            }

            var measure = _score.Measures[measureIndex];
            if (!ChordMarkerService.TryAddMarker(measure, beatPosition))
            {
                return false;
            }

            SelectMeasure(measureIndex);
            SelectChordMarker(measureIndex, measure.ChordMarkers.Count - 1);
            UpdateContentSize();
            ChordMarkersChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public bool TryRemoveSelectedChord()
        {
            if (_selectedChordMeasureIndex < 0 || _selectedChordMarkerIndex < 0)
            {
                return false;
            }

            var measure = _score.Measures[_selectedChordMeasureIndex];
            if (!ChordMarkerService.TryRemoveMarker(measure, _selectedChordMarkerIndex))
            {
                return false;
            }

            ClearChordSelection();
            UpdateContentSize();
            ChordMarkersChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void UpdateSelectedChordText(string text)
        {
            if (_selectedChordMeasureIndex < 0 || _selectedChordMarkerIndex < 0)
            {
                return;
            }

            var measure = _score.Measures[_selectedChordMeasureIndex];
            if (_selectedChordMarkerIndex >= measure.ChordMarkers.Count)
            {
                return;
            }

            measure.ChordMarkers[_selectedChordMarkerIndex].Text = text ?? string.Empty;
            if (_chordInlineEditor != null
                && _editingChordMeasureIndex == _selectedChordMeasureIndex
                && _editingChordMarkerIndex == _selectedChordMarkerIndex
                && !string.Equals(_chordInlineEditor.Text, text ?? string.Empty, StringComparison.Ordinal))
            {
                _chordInlineEditor.Text = text ?? string.Empty;
            }

            InvalidateSelection();
            ChordMarkersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SelectTie(int tieIndex)
        {
            CommitInlineEdit();
            if (_score.Ties == null || tieIndex < 0 || tieIndex >= _score.Ties.Count)
            {
                return;
            }

            var tie = _score.Ties[tieIndex];
            SetSelectedMeasures(new[] { tie.StartMeasureIndex }, tie.StartMeasureIndex, false);
            _measureSelectionAnchor = tie.StartMeasureIndex;
            _selectedMeasureIndex = tie.StartMeasureIndex;
            _selectedNoteIndex = -1;
            _selectedInsertIndex = -1;
            _selectedTieIndex = tieIndex;
            _selectedChordMeasureIndex = -1;
            _selectedChordMarkerIndex = -1;
            RaiseSelectionChanged();
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
            _selectedTieIndex = -1;
            _selectedChordMeasureIndex = -1;
            _selectedChordMarkerIndex = -1;
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
            _selectedTieIndex = -1;
            _selectedChordMeasureIndex = -1;
            _selectedChordMarkerIndex = -1;
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
            ClearTieSelection();
            ClearChordSelection();
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

        public void SetPlaybackPosition(double quarterBeat, bool showHead = true, bool ensureVisible = false)
        {
            var oldBounds = GetPlaybackHeadBounds(_playbackPositionQuarter, _showPlaybackHead);
            _playbackPositionQuarter = Math.Max(0, quarterBeat);
            _showPlaybackHead = showHead;
            var newBounds = GetPlaybackHeadBounds(_playbackPositionQuarter, _showPlaybackHead);
            if (oldBounds == newBounds && !ensureVisible)
            {
                return;
            }

            InvalidatePlaybackRegion(oldBounds, newBounds);
            if (ensureVisible)
            {
                EnsurePlaybackVisibleIfNeeded();
            }
        }

        public void HidePlaybackHead()
        {
            var oldBounds = GetPlaybackHeadBounds(_playbackPositionQuarter, _showPlaybackHead);
            _showPlaybackHead = false;
            _draggingPlaybackHead = false;
            InvalidatePlaybackRegion(oldBounds, Rectangle.Empty);
        }

        public double PlaybackPositionQuarter
        {
            get { return _playbackPositionQuarter; }
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
            EnsureScoreBitmap();
            if (_scoreBitmap != null)
            {
                e.Graphics.DrawImage(_scoreBitmap, 0, 0);
            }

            DrawPlaybackHead(e.Graphics);
        }

        private void DrawPlaybackHead(Graphics graphics)
        {
            if (!_showPlaybackHead)
            {
                return;
            }

            var marker = PlaybackLayout.GetMarkerPosition(_playbackSegments, _playbackPositionQuarter);
            if (!marker.IsVisible)
            {
                return;
            }

            using (var pen = new Pen(Color.FromArgb(220, 57, 120, 215), 2f))
            using (var brush = new SolidBrush(Color.FromArgb(230, 57, 120, 215)))
            {
                graphics.DrawLine(pen, marker.X, marker.Top, marker.X, marker.Bottom);
                var triangle = new[]
                {
                    new Point(marker.X - 7, marker.Top - 2),
                    new Point(marker.X + 7, marker.Top - 2),
                    new Point(marker.X, marker.Top + 10)
                };
                graphics.FillPolygon(brush, triangle);
                graphics.DrawPolygon(pen, triangle);
            }
        }

        private void OnContentMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (_showPlaybackHead)
            {
                var marker = PlaybackLayout.GetMarkerPosition(_playbackSegments, _playbackPositionQuarter);
                if (marker.IsVisible && Math.Abs(e.X - marker.X) <= _playbackHeadHitZone)
                {
                    _draggingPlaybackHead = true;
                    _playbackHeadDragMoved = false;
                    _contentPanel.Capture = true;
                    return;
                }
            }

            var hit = _renderer.HitTest(_score, GetDrawWidth(), e.Location);
            if (hit.HitType == ScoreHitType.ChordDragHandle)
            {
                _draggingChordMarker = true;
                _dragChordMeasureIndex = hit.MeasureIndex;
                _dragChordMarkerIndex = hit.ChordMarkerIndex;
                SelectChordMarker(hit.MeasureIndex, hit.ChordMarkerIndex, startInlineEdit: false);
                _contentPanel.Capture = true;
            }
        }

        private void OnContentMouseMove(object sender, MouseEventArgs e)
        {
            if (_draggingChordMarker)
            {
                UpdateChordMarkerDrag(e.X);
                return;
            }

            if (!_draggingPlaybackHead)
            {
                UpdatePlaybackCursor(e.Location);
                return;
            }

            _playbackHeadDragMoved = true;
            var beat = PlaybackLayout.MapXToBeat(_playbackSegments, e.X);
            SetPlaybackPosition(beat, showHead: true, ensureVisible: false);
            PlaybackSeeked?.Invoke(beat);
        }

        private void OnContentMouseUp(object sender, MouseEventArgs e)
        {
            if (_draggingChordMarker)
            {
                _draggingChordMarker = false;
                _contentPanel.Capture = false;
                CommitChordMarkerDrag(e.X);
                return;
            }

            if (!_draggingPlaybackHead)
            {
                return;
            }

            _draggingPlaybackHead = false;
            _contentPanel.Capture = false;
            var beat = PlaybackLayout.MapXToBeat(_playbackSegments, e.X);
            SetPlaybackPosition(beat, showHead: true, ensureVisible: true);
            PlaybackSeeked?.Invoke(beat);
        }

        private void UpdatePlaybackCursor(Point location)
        {
            if (!_showPlaybackHead)
            {
                _contentPanel.Cursor = Cursors.Default;
                return;
            }

            var marker = PlaybackLayout.GetMarkerPosition(_playbackSegments, _playbackPositionQuarter);
            _contentPanel.Cursor = marker.IsVisible && Math.Abs(location.X - marker.X) <= _playbackHeadHitZone
                ? Cursors.SizeWE
                : Cursors.Default;
        }

        private void OnContentClick(object sender, MouseEventArgs e)
        {
            if (_playbackHeadDragMoved)
            {
                _playbackHeadDragMoved = false;
                return;
            }

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
                case ScoreHitType.Tie:
                    SelectTie(hit.TieIndex);
                    break;
                case ScoreHitType.Note:
                    SelectSingleMeasure(hit.MeasureIndex, false);
                    _selectedNoteIndex = hit.NoteIndex;
                    _selectedInsertIndex = -1;
                    _selectedTieIndex = -1;
                    ClearChordSelection();
                    RaiseSelectionChanged();
                    InvalidateSelection();
                    break;
                case ScoreHitType.Gap:
                    SelectSingleMeasure(hit.MeasureIndex, false);
                    _selectedNoteIndex = -1;
                    _selectedInsertIndex = hit.InsertIndex;
                    _selectedTieIndex = -1;
                    ClearChordSelection();
                    RaiseSelectionChanged();
                    InvalidateSelection();
                    break;
                case ScoreHitType.ChordMarker:
                    SelectChordMarker(hit.MeasureIndex, hit.ChordMarkerIndex, startInlineEdit: true);
                    break;
                case ScoreHitType.ChordDelete:
                    SelectChordMarker(hit.MeasureIndex, hit.ChordMarkerIndex);
                    TryRemoveSelectedChord();
                    break;
                case ScoreHitType.ChordAddSlot:
                    {
                        var measure = _score.Measures[hit.MeasureIndex];
                        var duration = ScoreMidiSchedule.GetMeasureDurationUnits(measure);
                        var layouts = _renderer.GetMeasureLayouts(_score, GetDrawWidth());
                        var layout = layouts.FirstOrDefault(item => item.MeasureIndex == hit.MeasureIndex);
                        var beat = layout == null
                            ? 0
                            : ChordMarkerService.MapXToBeat(layout.X, layout.Width, e.X, duration);
                        TryAddChordToMeasure(hit.MeasureIndex, beat);
                    }

                    break;
                case ScoreHitType.ChordRow:
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        HandleMeasureSelectionClick(hit.MeasureIndex);
                        break;
                    }

                    SelectSingleMeasure(hit.MeasureIndex, false);
                    ClearChordSelection();
                    RaiseSelectionChanged();
                    break;
                case ScoreHitType.LyricText:
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        HandleMeasureSelectionClick(hit.MeasureIndex);
                        break;
                    }

                    SelectSingleMeasure(hit.MeasureIndex, false);
                    RaiseSelectionChanged();
                    StartInlineEdit(hit.MeasureIndex, hit.Bounds);
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
                ClearTieSelection();
                ClearChordSelection();
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
                ClearTieSelection();
                ClearChordSelection();
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
                ClearTieSelection();
                ClearChordSelection();
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

        private void StartInlineEdit(int measureIndex, Rectangle bounds)
        {
            CommitInlineEdit();

            _editingMeasureIndex = measureIndex;
            var measure = _score.Measures[measureIndex];
            var text = measure.LyricText;

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
            CommitChordInlineEdit();
            if (_inlineEditor == null || _editingMeasureIndex < 0)
            {
                return;
            }

            var text = _inlineEditor.Text ?? string.Empty;
            var measure = _score.Measures[_editingMeasureIndex];
            measure.LyricText = text;

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
                TieIndex = _selectedTieIndex,
                ChordMeasureIndex = _selectedChordMeasureIndex,
                ChordMarkerIndex = _selectedChordMarkerIndex,
                SelectedMeasureIndices = _selectedMeasureIndices.ToArray()
            });
        }

        private void InvalidateSelection()
        {
            MarkScoreBitmapDirty();
            _contentPanel.Invalidate();
        }

        private void MarkScoreBitmapDirty()
        {
            _scoreBitmapDirty = true;
        }

        private void EnsureScoreBitmap()
        {
            if (!_scoreBitmapDirty && _scoreBitmap != null &&
                _scoreBitmap.Width == _contentPanel.Width && _scoreBitmap.Height == _contentPanel.Height)
            {
                return;
            }

            if (_contentPanel.Width <= 0 || _contentPanel.Height <= 0)
            {
                return;
            }

            _scoreBitmap?.Dispose();
            _scoreBitmap = new Bitmap(_contentPanel.Width, _contentPanel.Height);
            using (var graphics = Graphics.FromImage(_scoreBitmap))
            {
                graphics.Clear(Color.White);
                _renderer.Draw(
                    graphics,
                    _score,
                    GetDrawWidth(),
                    _selectedMeasureIndex,
                    _selectedNoteIndex,
                    _selectedInsertIndex,
                    _selectedMeasureIndices,
                    _selectedTieIndex,
                    _selectedChordMeasureIndex,
                    _selectedChordMarkerIndex,
                    ScoreLayoutOptions.Editor);
            }

            _scoreBitmapDirty = false;
        }

        private static void EnableDoubleBuffer(Control control)
        {
            typeof(Control).InvokeMember(
                "DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetProperty,
                null,
                control,
                new object[] { true });
        }

        private Rectangle GetPlaybackHeadBounds(double quarterBeat, bool visible)
        {
            if (!visible)
            {
                return Rectangle.Empty;
            }

            var marker = PlaybackLayout.GetMarkerPosition(_playbackSegments, quarterBeat);
            if (!marker.IsVisible)
            {
                return Rectangle.Empty;
            }

            return new Rectangle(
                marker.X - _playbackHeadHitZone,
                marker.Top - 14,
                _playbackHeadHitZone * 2,
                marker.Bottom - marker.Top + 16);
        }

        private void InvalidatePlaybackRegion(Rectangle oldBounds, Rectangle newBounds)
        {
            if (!oldBounds.IsEmpty)
            {
                _contentPanel.Invalidate(oldBounds);
            }

            if (!newBounds.IsEmpty)
            {
                _contentPanel.Invalidate(newBounds);
            }
        }

        private void UpdateContentSize()
        {
            var drawWidth = GetDrawWidth();
            var size = _renderer.MeasureScore(_score, drawWidth);
            _contentPanel.Size = new Size(Math.Max(drawWidth, size.Width), Math.Max(360, size.Height));
            AutoScrollMinSize = _contentPanel.Size;
            _playbackSegments = _renderer.BuildPlaybackSegments(_score, drawWidth);
            MarkScoreBitmapDirty();
            SyncChordInlineEditorBounds();
        }

        private void UpdateChordMarkerDrag(int x)
        {
            if (_dragChordMeasureIndex < 0 || _dragChordMarkerIndex < 0)
            {
                return;
            }

            var layouts = _renderer.GetMeasureLayouts(_score, GetDrawWidth());
            var layout = layouts.FirstOrDefault(item => item.MeasureIndex == _dragChordMeasureIndex);
            if (layout == null)
            {
                return;
            }

            var measure = _score.Measures[_dragChordMeasureIndex];
            if (_dragChordMarkerIndex < 0 || _dragChordMarkerIndex >= measure.ChordMarkers.Count)
            {
                return;
            }

            var marker = measure.ChordMarkers[_dragChordMarkerIndex];
            var duration = ScoreMidiSchedule.GetMeasureDurationUnits(measure);
            var beat = ChordMarkerService.MapXToBeat(layout.X, layout.Width, x, duration);
            ChordMarkerService.SetMarkerBeat(measure, _dragChordMarkerIndex, beat);
            _dragChordMarkerIndex = measure.ChordMarkers.IndexOf(marker);
            SyncChordInlineEditorBounds();
            MarkScoreBitmapDirty();
            _contentPanel.Invalidate();
        }

        private void CommitChordMarkerDrag(int x)
        {
            UpdateChordMarkerDrag(x);
            if (_dragChordMeasureIndex >= 0 && _dragChordMarkerIndex >= 0)
            {
                SelectChordMarker(_dragChordMeasureIndex, _dragChordMarkerIndex);
            }

            ChordMarkersChanged?.Invoke(this, EventArgs.Empty);
        }

        private void StartChordInlineEdit(int measureIndex, int markerIndex)
        {
            CommitChordInlineEdit();
            var layouts = _renderer.GetMeasureLayouts(_score, GetDrawWidth(), ScoreLayoutOptions.Editor);
            var layout = layouts.FirstOrDefault(item => item.MeasureIndex == measureIndex);
            if (layout == null)
            {
                return;
            }

            var measure = _score.Measures[measureIndex];
            var marker = measure.ChordMarkers[markerIndex];
            var bounds = ChordMarkerLayout.GetMarkerBounds(layout, measure, measureIndex, markerIndex, marker).TextBoxBounds;
            _editingChordMeasureIndex = measureIndex;
            _editingChordMarkerIndex = markerIndex;
            _chordInlineEditor = new TextBox
            {
                Bounds = bounds,
                Text = marker.Text ?? string.Empty,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Arial", 11f, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 255, 240),
                ForeColor = Color.Black
            };
            _chordInlineEditor.KeyDown += OnChordInlineEditorKeyDown;
            _chordInlineEditor.LostFocus += OnChordInlineEditorLostFocus;
            _chordInlineEditor.TextChanged += OnChordInlineEditorTextChanged;
            _contentPanel.Controls.Add(_chordInlineEditor);
            _chordInlineEditor.BringToFront();
            _chordInlineEditor.Focus();
            _chordInlineEditor.SelectAll();
        }

        private void OnChordInlineEditorKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CommitChordInlineEdit();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                CancelChordInlineEdit();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void OnChordInlineEditorLostFocus(object sender, EventArgs e)
        {
            if (_chordInlineEditor == null || _chordInlineEditor.Focused)
            {
                return;
            }

            CommitChordInlineEdit();
        }

        private void OnChordInlineEditorTextChanged(object sender, EventArgs e)
        {
            if (_chordInlineEditor == null || _editingChordMeasureIndex < 0 || _editingChordMarkerIndex < 0)
            {
                return;
            }

            var measure = _score.Measures[_editingChordMeasureIndex];
            if (_editingChordMarkerIndex >= measure.ChordMarkers.Count)
            {
                return;
            }

            measure.ChordMarkers[_editingChordMarkerIndex].Text = _chordInlineEditor.Text ?? string.Empty;
            MarkScoreBitmapDirty();
            _contentPanel.Invalidate();
            ChordMarkersChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CommitChordInlineEdit()
        {
            if (_chordInlineEditor == null)
            {
                return;
            }

            if (_editingChordMeasureIndex >= 0
                && _editingChordMarkerIndex >= 0
                && _editingChordMeasureIndex < _score.Measures.Count)
            {
                var measure = _score.Measures[_editingChordMeasureIndex];
                if (_editingChordMarkerIndex < measure.ChordMarkers.Count)
                {
                    measure.ChordMarkers[_editingChordMarkerIndex].Text = _chordInlineEditor.Text ?? string.Empty;
                }
            }

            RemoveChordInlineEditor();
            MarkScoreBitmapDirty();
            _contentPanel.Invalidate();
            ChordMarkersChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CancelChordInlineEdit()
        {
            RemoveChordInlineEditor();
            MarkScoreBitmapDirty();
            _contentPanel.Invalidate();
        }

        private void RemoveChordInlineEditor()
        {
            if (_chordInlineEditor != null)
            {
                _chordInlineEditor.KeyDown -= OnChordInlineEditorKeyDown;
                _chordInlineEditor.LostFocus -= OnChordInlineEditorLostFocus;
                _chordInlineEditor.TextChanged -= OnChordInlineEditorTextChanged;
                _contentPanel.Controls.Remove(_chordInlineEditor);
                _chordInlineEditor.Dispose();
                _chordInlineEditor = null;
            }

            _editingChordMeasureIndex = -1;
            _editingChordMarkerIndex = -1;
        }

        private void SyncChordInlineEditorBounds()
        {
            if (_chordInlineEditor == null || _editingChordMeasureIndex < 0 || _editingChordMarkerIndex < 0)
            {
                return;
            }

            var layouts = _renderer.GetMeasureLayouts(_score, GetDrawWidth(), ScoreLayoutOptions.Editor);
            var layout = layouts.FirstOrDefault(item => item.MeasureIndex == _editingChordMeasureIndex);
            if (layout == null)
            {
                return;
            }

            var measure = _score.Measures[_editingChordMeasureIndex];
            if (_editingChordMarkerIndex >= measure.ChordMarkers.Count)
            {
                return;
            }

            var marker = measure.ChordMarkers[_editingChordMarkerIndex];
            var bounds = ChordMarkerLayout.GetMarkerBounds(
                layout,
                measure,
                _editingChordMeasureIndex,
                _editingChordMarkerIndex,
                marker).TextBoxBounds;
            _chordInlineEditor.Bounds = bounds;
        }

        private void ClearChordEditors()
        {
            RemoveChordInlineEditor();
        }

        private void EnsurePlaybackVisibleIfNeeded()
        {
            if (!_showPlaybackHead)
            {
                return;
            }

            var marker = PlaybackLayout.GetMarkerPosition(_playbackSegments, _playbackPositionQuarter);
            if (!marker.IsVisible)
            {
                return;
            }

            var scrollX = -AutoScrollPosition.X;
            var scrollY = -AutoScrollPosition.Y;
            var viewWidth = Math.Max(0, ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
            var viewHeight = Math.Max(0, ClientSize.Height - SystemInformation.HorizontalScrollBarHeight);
            const int margin = 72;
            if (marker.X >= scrollX + margin && marker.X <= scrollX + viewWidth - margin &&
                marker.Top >= scrollY + margin && marker.Bottom <= scrollY + viewHeight - margin)
            {
                return;
            }

            var targetX = marker.X - viewWidth / 3;
            var targetY = marker.Top - 24;
            var maxX = Math.Max(0, _contentPanel.Width - viewWidth);
            var maxY = Math.Max(0, _contentPanel.Height - viewHeight);
            AutoScrollPosition = new Point(
                -Math.Max(0, Math.Min(maxX, targetX)),
                -Math.Max(0, Math.Min(maxY, targetY)));
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