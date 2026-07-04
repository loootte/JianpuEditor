using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using JianpuEditor.Models;
using JianpuEditor.Services;

namespace JianpuEditor.Rendering
{
    public sealed class JianpuRenderer : IDisposable
    {
        public const int NoteCellWidth = 96;
        public const int MinMeasureWidth = 120;
        public const int MarginLeft = 72;
        public const int MarginTop = 72;
        public const int MelodyRowHeight = 88;
        public const int SecondaryRowHeight = 40;
        public const int TextRowHeight = SecondaryRowHeight;
        public const int RowGap = 6;
        public const int StaffBlockHeight = MelodyRowHeight + RowGap + SecondaryRowHeight + RowGap + TextRowHeight;
        public const int StaffBlockSpacing = 48;
        public const int GapEdgeWidth = 10;
        public const int BarHitWidth = 10;
        public const int GapCaretWidth = 6;
        public const int MinNoteWidth = 28;
        public const float LyricBaseFontSize = 20f;

        private readonly Font _rowLabelFont = new Font("Microsoft YaHei", 9f, FontStyle.Regular);
        private readonly Font _ornamentFont = new Font("Microsoft YaHei", 10f, FontStyle.Regular);
        private readonly Font _ornamentLatinFont = new Font("Arial", 10f, FontStyle.Italic);
        private readonly Font _noteFont = new Font("Arial", 26f, FontStyle.Bold);
        private readonly Font _secondaryFont = new Font("Arial", 20f, FontStyle.Bold);
        private bool _disposed;
        private ScoreLayoutOptions _activeLayoutOptions;

        private Color InkColor
        {
            get
            {
                return _activeLayoutOptions != null
                       && _activeLayoutOptions.RespectAppTheme
                       && AppTheme.IsDarkMode
                    ? AppTheme.PrimaryText
                    : Color.Black;
            }
        }

        private Brush CreateInkBrush()
        {
            return new SolidBrush(InkColor);
        }

        private Pen CreateInkPen(float width)
        {
            return new Pen(InkColor, width);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _rowLabelFont.Dispose();
            _ornamentFont.Dispose();
            _ornamentLatinFont.Dispose();
            _noteFont.Dispose();
            _secondaryFont.Dispose();
            _disposed = true;
        }

        public IReadOnlyList<PlaybackMeasureSegment> BuildPlaybackSegments(JianpuScore score, int width)
        {
            var segments = new List<PlaybackMeasureSegment>();
            if (score?.Measures == null || score.Measures.Count == 0)
            {
                return segments;
            }

            var layout = BuildLayout(score, width, ScoreLayoutOptions.Default);
            var beat = 0.0;
            foreach (var measure in layout.Measures)
            {
                if (measure.MeasureIndex < 0 || measure.MeasureIndex >= score.Measures.Count)
                {
                    continue;
                }

                var duration = ScoreMidiSchedule.GetMeasureDurationUnits(score.Measures[measure.MeasureIndex]);
                segments.Add(new PlaybackMeasureSegment
                {
                    StartBeat = beat,
                    DurationBeat = duration,
                    X = measure.X,
                    Width = measure.Width,
                    BlockTop = measure.BlockTop
                });
                beat += duration;
            }

            return segments;
        }

        public Size MeasureScore(JianpuScore score, int maxWidth, ScoreLayoutOptions options = null)
        {
            options = options ?? ScoreLayoutOptions.Default;
            var layout = BuildLayout(score, maxWidth, options);
            var marginTop = GetMarginTop(options);
            var height = marginTop + layout.Lines.Count * StaffBlockHeight
                         + Math.Max(0, layout.Lines.Count - 1) * StaffBlockSpacing + 48;
            return new Size(Math.Max(maxWidth, layout.TotalWidth + MarginLeft), Math.Max(320, height));
        }

        public IReadOnlyList<MeasureLayout> GetMeasureLayouts(JianpuScore score, int width, ScoreLayoutOptions layoutOptions = null)
        {
            layoutOptions = layoutOptions ?? ScoreLayoutOptions.Default;
            return BuildLayout(score, width, layoutOptions).Measures;
        }

        public void Draw(
            Graphics graphics,
            JianpuScore score,
            int width,
            int selectedMeasureIndex = -1,
            int selectedNoteIndex = -1,
            int selectedInsertIndex = -1,
            IReadOnlyList<int> selectedMeasureIndices = null,
            int selectedTieIndex = -1,
            int selectedChordMeasureIndex = -1,
            int selectedChordMarkerIndex = -1,
            IReadOnlyList<ScoreNoteRef> selectedNotes = null,
            ScoreLayoutOptions layoutOptions = null)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            layoutOptions = layoutOptions ?? ScoreLayoutOptions.Default;
            _activeLayoutOptions = layoutOptions;
            graphics.Clear(AppTheme.GetScoreBackground(layoutOptions.RespectAppTheme));
            var layout = BuildLayout(score, width, layoutOptions);
            DrawHeader(graphics, score, width, layoutOptions);
            DrawRowLabels(graphics, layout);
            DrawStaff(
                graphics,
                score,
                layout,
                selectedMeasureIndex,
                selectedNoteIndex,
                selectedInsertIndex,
                selectedMeasureIndices,
                selectedTieIndex,
                selectedChordMeasureIndex,
                selectedChordMarkerIndex,
                selectedNotes,
                layoutOptions);
            _activeLayoutOptions = null;
        }

        public static double GetDurationUnits(JianpuNote note)
        {
            if (note == null)
            {
                return 1;
            }

            // 简谱增时线：0条=四分(1拍)，1条=二分(2拍)，3条=全音(4拍)，按时值线性叠加而非 2^n
            var lengthUnits = 1.0 + note.Dashes;
            var divisor = Math.Pow(2, note.Underlines);
            var duration = lengthUnits / divisor;
            if (note.Dotted)
            {
                duration *= 1.5;
            }

            return duration;
        }

        public static int GetNoteWidth(JianpuNote note, double scale = 1.0)
        {
            if (scale <= 0)
            {
                scale = 1.0;
            }

            var width = (int)Math.Round(NoteCellWidth * scale * GetDurationUnits(note));
            return Math.Max(Math.Max(12, (int)Math.Round(MinNoteWidth * scale)), width);
        }

        private static int GetGapEdgeWidth(int noteWidth)
        {
            return Math.Min(GapEdgeWidth, Math.Max(4, noteWidth / 5));
        }

        public ScoreHitResult HitTest(JianpuScore score, int width, Point point)
        {
            var headerHit = HitTestHeader(score, width, point);
            if (headerHit != null)
            {
                return headerHit;
            }

            var layout = BuildLayout(score, width, null);

            var barHit = HitTestBarLineGap(score, layout, point);
            if (barHit != null)
            {
                return barHit;
            }

            var tieHit = HitTestTies(score, layout, point);
            if (tieHit != null)
            {
                return tieHit;
            }

            var chordHit = HitTestChordMarkers(score, layout, point);
            if (chordHit != null)
            {
                return chordHit;
            }

            foreach (var measure in layout.Measures)
            {
                var blockBounds = new Rectangle(measure.X, measure.BlockTop, measure.Width, StaffBlockHeight);
                if (!blockBounds.Contains(point))
                {
                    continue;
                }

                var melodyBottom = measure.BlockTop + MelodyRowHeight;
                var secondaryTop = melodyBottom + RowGap;
                var secondaryBottom = secondaryTop + SecondaryRowHeight;
                var lyricTop = secondaryBottom + RowGap;
                var lyricBottom = lyricTop + TextRowHeight;

                if (point.Y < melodyBottom)
                {
                    return HitTestMelodyRow(score, measure, point);
                }

                if (point.Y < secondaryBottom)
                {
                    return HitTestSecondaryRow(score, measure, point, secondaryTop);
                }

                if (point.Y < lyricBottom)
                {
                    return new ScoreHitResult
                    {
                        HitType = ScoreHitType.LyricText,
                        MeasureIndex = measure.MeasureIndex,
                        Bounds = GetTextCellBounds(measure, lyricTop, TextRowHeight).ToIntRect()
                    };
                }

                return new ScoreHitResult
                {
                    HitType = ScoreHitType.Measure,
                    MeasureIndex = measure.MeasureIndex
                };
            }

            return new ScoreHitResult();
        }

        private ScoreHitResult HitTestMelodyRow(JianpuScore score, MeasureLayout measure, Point point)
        {
            var notes = score.Measures[measure.MeasureIndex].MelodyNotes;
            var relX = point.X - measure.X;
            var noteCount = notes.Count;

            if (noteCount == 0)
            {
                return CreateGapHit(measure, 0);
            }

            for (var i = 0; i < noteCount; i++)
            {
                var cellStart = measure.GetNoteOffset(i);
                var cellEnd = cellStart + measure.GetNoteWidth(i);
                if (relX < cellStart || relX >= cellEnd)
                {
                    continue;
                }

                var offset = relX - cellStart;
                var noteWidth = measure.GetNoteWidth(i);
                var edgeWidth = GetGapEdgeWidth(noteWidth);
                if (offset < edgeWidth)
                {
                    return CreateGapHit(measure, i);
                }

                if (offset > noteWidth - edgeWidth)
                {
                    return CreateGapHit(measure, i + 1);
                }

                return new ScoreHitResult
                {
                    HitType = ScoreHitType.Note,
                    MeasureIndex = measure.MeasureIndex,
                    NoteIndex = i,
                    Bounds = GetNoteBounds(measure, i).ToIntRect()
                };
            }

            if (relX >= 0 && relX < measure.Width)
            {
                return CreateGapHit(measure, noteCount);
            }

            return new ScoreHitResult
            {
                HitType = ScoreHitType.Measure,
                MeasureIndex = measure.MeasureIndex
            };
        }

        private ScoreHitResult HitTestBarLineGap(JianpuScore score, ScoreLayout layout, Point point)
        {
            foreach (var measure in layout.Measures)
            {
                var melodyTop = measure.BlockTop;
                var melodyBottom = melodyTop + MelodyRowHeight;
                if (point.Y < melodyTop || point.Y >= melodyBottom)
                {
                    continue;
                }

                if (Math.Abs(point.X - measure.X) <= BarHitWidth)
                {
                    if (measure.MeasureIndex > 0)
                    {
                        var prevLayout = FindMeasureLayout(layout, measure.MeasureIndex - 1);
                        if (prevLayout != null && prevLayout.BlockTop == measure.BlockTop && point.X < measure.X)
                        {
                            var prevCount = score.Measures[prevLayout.MeasureIndex].MelodyNotes.Count;
                            return CreateGapHit(prevLayout, prevCount);
                        }
                    }

                    return CreateGapHit(measure, 0);
                }

                if (Math.Abs(point.X - measure.BarLineX) <= BarHitWidth)
                {
                    var noteCount = score.Measures[measure.MeasureIndex].MelodyNotes.Count;
                    var hasNext = measure.MeasureIndex < score.Measures.Count - 1;
                    if (hasNext && point.X > measure.BarLineX)
                    {
                        var nextMeasure = FindMeasureLayout(layout, measure.MeasureIndex + 1);
                        if (nextMeasure != null && nextMeasure.BlockTop == measure.BlockTop)
                        {
                            return CreateGapHit(nextMeasure, 0);
                        }
                    }

                    return CreateGapHit(measure, noteCount);
                }
            }

            return null;
        }

        private static MeasureLayout FindMeasureLayout(ScoreLayout layout, int measureIndex)
        {
            foreach (var measure in layout.Measures)
            {
                if (measure.MeasureIndex == measureIndex)
                {
                    return measure;
                }
            }

            return null;
        }

        private static ScoreHitResult CreateGapHit(MeasureLayout measure, int insertIndex)
        {
            return new ScoreHitResult
            {
                HitType = ScoreHitType.Gap,
                MeasureIndex = measure.MeasureIndex,
                InsertIndex = insertIndex,
                Bounds = GetGapBounds(measure, insertIndex).ToIntRect()
            };
        }

        public static Rectangle GetGapBounds(MeasureLayout measure, int insertIndex)
        {
            var x = measure.X + measure.GetInsertOffset(insertIndex) - GapCaretWidth / 2;
            return new Rectangle(x, measure.BlockTop + 6, GapCaretWidth, MelodyRowHeight - 12);
        }

        public Bitmap RenderToBitmap(JianpuScore score, int width, ScoreLayoutOptions options = null)
        {
            options = options ?? ScoreLayoutOptions.Default;
            var size = MeasureScore(score, width, options);
            var bitmap = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                Draw(g, score, width, -1, -1, -1, null, -1, -1, -1, null, options);
            }

            return bitmap;
        }

        public int GetStaffLineCount(JianpuScore score, int width, ScoreLayoutOptions options = null)
        {
            options = options ?? ScoreLayoutOptions.Default;
            return BuildLayout(score, width, options).Lines.Count;
        }

        public Bitmap RenderPdfPageToBitmap(
            JianpuScore score,
            int width,
            ScoreLayoutOptions options,
            PdfPageSlice slice)
        {
            options = options ?? ScoreLayoutOptions.PdfExport;
            slice = slice ?? new PdfPageSlice { PageNumber = 1, TotalPages = 1 };
            var layout = BuildLayout(score, width, options);
            if (layout.Lines.Count == 0 || slice.LineCount <= 0)
            {
                var emptyHeight = slice.PageNumber == 1
                    ? GetMarginTop(options)
                    : PdfPagePlanner.CompactHeaderHeight;
                emptyHeight += PdfPagePlanner.BottomMargin;
                var emptyBitmap = new Bitmap(Math.Max(1, width), Math.Max(1, emptyHeight));
                using (var g = Graphics.FromImage(emptyBitmap))
                {
                    g.Clear(AppTheme.GetScoreBackground(false));
                    if (slice.PageNumber == 1)
                    {
                        DrawHeader(g, score, width, options);
                    }
                    else
                    {
                        DrawPdfContinuationHeader(g, score, width, slice);
                    }
                }

                return emptyBitmap;
            }

            var start = Math.Max(0, slice.FirstLineIndex);
            var count = Math.Min(slice.LineCount, layout.Lines.Count - start);
            var firstLine = layout.Lines[start];
            var lastLine = layout.Lines[start + count - 1];
            var contentHeight = lastLine.BlockTop + StaffBlockHeight - firstLine.BlockTop;
            var headerHeight = slice.PageNumber == 1
                ? GetMarginTop(options)
                : PdfPagePlanner.CompactHeaderHeight;
            var bitmapHeight = headerHeight + contentHeight + PdfPagePlanner.BottomMargin;
            var bitmap = new Bitmap(width, Math.Max(1, bitmapHeight));
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                _activeLayoutOptions = options;
                g.Clear(AppTheme.GetScoreBackground(false));
                if (slice.PageNumber == 1)
                {
                    DrawHeader(g, score, width, options);
                }
                else
                {
                    DrawPdfContinuationHeader(g, score, width, slice);
                }

                g.TranslateTransform(0, headerHeight - firstLine.BlockTop);
                DrawRowLabelsForBlock(g, firstLine.BlockTop);
                DrawStaffLineRange(g, score, layout, start, count, options);
                DrawTiesForLineRange(g, score, layout, start, count, -1);
                _activeLayoutOptions = null;
            }

            return bitmap;
        }

        public static Rectangle GetNoteBounds(MeasureLayout measure, int noteIndex)
        {
            return new Rectangle(
                measure.X + measure.GetNoteOffset(noteIndex),
                measure.BlockTop,
                measure.GetNoteWidth(noteIndex),
                MelodyRowHeight);
        }

        public static bool TryGetSyllableAnchorX(
            MeasureLayout layout,
            JianpuMeasure measure,
            LyricSyllable syllable,
            out float centerX)
        {
            centerX = 0;
            if (layout == null || measure == null || syllable == null || string.IsNullOrEmpty(syllable.Text))
            {
                return false;
            }

            var noteIndex = LyricSyllableService.ResolveNoteIndex(measure, syllable);
            var noteCount = measure.MelodyNotes?.Count ?? 0;
            if (noteIndex < 0 || noteIndex >= noteCount)
            {
                return false;
            }

            var melodyScale = layout.MelodyScale;
            var minDrawWidth = melodyScale < 0.999
                ? Math.Max(6, (int)Math.Round(MinNoteWidth * melodyScale))
                : MinNoteWidth;
            GetNoteDrawBounds(layout, noteIndex, noteCount, melodyScale, minDrawWidth, out var noteX, out var noteWidth);
            centerX = GetNoteHeadCenterX(noteX, noteWidth);
            return true;
        }

        public static bool TryGetOrnamentAnchorX(
            MeasureLayout layout,
            JianpuMeasure measure,
            JianpuOrnament ornament,
            out float anchorX)
        {
            anchorX = 0;
            if (layout == null || measure == null || ornament == null || ornament.Type == OrnamentType.Unknown)
            {
                return false;
            }

            var noteIndex = OrnamentService.ResolveNoteIndex(measure, ornament);
            var noteCount = measure.MelodyNotes?.Count ?? 0;
            if (noteIndex < 0 || noteIndex >= noteCount)
            {
                return false;
            }

            var melodyScale = layout.MelodyScale;
            var minDrawWidth = melodyScale < 0.999
                ? Math.Max(6, (int)Math.Round(MinNoteWidth * melodyScale))
                : MinNoteWidth;
            GetNoteDrawBounds(layout, noteIndex, noteCount, melodyScale, minDrawWidth, out var noteX, out var noteWidth);
            anchorX = GetOrnamentAnchorX(ornament.Type, noteX, noteWidth);
            return true;
        }

        public static Rectangle GetTextCellBounds(MeasureLayout measure, int rowTop, int rowHeight)
        {
            return new Rectangle(measure.X + 4, rowTop + 2, measure.Width - 8, rowHeight - 4);
        }

        public static int GetSecondaryRowTop(MeasureLayout measure)
        {
            return measure.BlockTop + MelodyRowHeight + RowGap;
        }

        public static int GetLyricRowTop(MeasureLayout measure)
        {
            return GetSecondaryRowTop(measure) + SecondaryRowHeight + RowGap;
        }

        private void DrawPdfContinuationHeader(Graphics g, JianpuScore score, int width, PdfPageSlice slice)
        {
            var title = string.IsNullOrWhiteSpace(score.Title) ? "未命名乐曲" : score.Title.Trim();
            var pageText = title + "    第 " + slice.PageNumber + " / " + slice.TotalPages + " 页";
            using (var font = new Font("Microsoft YaHei", 14f, FontStyle.Regular))
            using (var ink = CreateInkBrush())
            {
                g.DrawString(pageText, font, ink, MarginLeft, 12f);
            }
        }

        private void DrawRowLabelsForBlock(Graphics g, int blockTop)
        {
            DrawRowLabel(g, "主旋律", blockTop + 28);
            DrawRowLabel(g, "副旋律", blockTop + MelodyRowHeight + RowGap + 10);
            DrawRowLabel(g, "歌词", blockTop + MelodyRowHeight + RowGap + SecondaryRowHeight + RowGap + 8);
        }

        private void DrawHeader(Graphics g, JianpuScore score, int width, ScoreLayoutOptions options)
        {
            var title = score.Title ?? string.Empty;
            using (var titleFont = new Font("Microsoft YaHei", options.TitleFontSize, FontStyle.Bold))
            using (var metaFont = new Font("Microsoft YaHei", options.MetaFontSize, FontStyle.Regular))
            {
                var titleTop = 20f;
                var titleSize = g.MeasureString(title, titleFont);
                using (var ink = CreateInkBrush())
                {
                    g.DrawString(title, titleFont, ink, (width - titleSize.Width) / 2f, titleTop);
                }

                var bpm = score.Bpm > 0 ? score.Bpm : 120;
                var meta = string.Format(
                    "{0}    {1}    BPM {2}",
                    score.KeySignature ?? "1=C",
                    score.Tempo ?? string.Empty,
                    bpm);
                if (!string.IsNullOrWhiteSpace(score.Composer))
                {
                    meta += "    作曲: " + score.Composer;
                }

                var metaTop = titleTop + titleFont.Size + 10f;
                var metaX = options.HeaderMetaLeftAligned
                    ? (float)MarginLeft
                    : (width - g.MeasureString(meta, metaFont).Width) / 2f;
                using (var metaBrush = new SolidBrush(AppTheme.SecondaryText))
                {
                    g.DrawString(meta, metaFont, metaBrush, metaX, metaTop);
                }
            }
        }

        private ScoreHitResult HitTestHeader(JianpuScore score, int width, Point point, ScoreLayoutOptions options = null)
        {
            options = options ?? ScoreLayoutOptions.Default;
            var marginTop = GetMarginTop(options);
            if (point.Y < 0 || point.Y >= marginTop)
            {
                return null;
            }

            using (var bitmap = new Bitmap(1, 1))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                var headerLayout = BuildHeaderLayout(graphics, score, width, options);
                if (headerLayout.TitleBounds.Contains(point))
                {
                    return CreateHeaderHit(ScoreHeaderField.Title, headerLayout.TitleBounds);
                }

                if (headerLayout.KeyBounds.Contains(point))
                {
                    return CreateHeaderHit(ScoreHeaderField.KeySignature, headerLayout.KeyBounds);
                }

                if (headerLayout.TempoBounds.Contains(point))
                {
                    return CreateHeaderHit(ScoreHeaderField.Tempo, headerLayout.TempoBounds);
                }

                if (headerLayout.BpmBounds.Contains(point))
                {
                    return CreateHeaderHit(ScoreHeaderField.Bpm, headerLayout.BpmBounds);
                }

                if (headerLayout.ComposerBounds.Contains(point))
                {
                    return CreateHeaderHit(ScoreHeaderField.Composer, headerLayout.ComposerBounds);
                }
            }

            return null;
        }

        private static ScoreHitResult CreateHeaderHit(ScoreHeaderField field, Rectangle bounds)
        {
            return new ScoreHitResult
            {
                HitType = ScoreHitType.ScoreHeader,
                HeaderField = field,
                Bounds = bounds.ToIntRect()
            };
        }

        private sealed class ScoreHeaderLayout
        {
            public Rectangle TitleBounds { get; set; }

            public Rectangle KeyBounds { get; set; }

            public Rectangle TempoBounds { get; set; }

            public Rectangle BpmBounds { get; set; }

            public Rectangle ComposerBounds { get; set; }
        }

        private ScoreHeaderLayout BuildHeaderLayout(Graphics g, JianpuScore score, int width, ScoreLayoutOptions options)
        {
            var layout = new ScoreHeaderLayout();
            using (var titleFont = new Font("Microsoft YaHei", options.TitleFontSize, FontStyle.Bold))
            using (var metaFont = new Font("Microsoft YaHei", options.MetaFontSize, FontStyle.Regular))
            {
                const float titleTop = 20f;
                const float metaGap = 10f;
                var metaTop = titleTop + titleFont.Size + metaGap;
                var rowHeight = Math.Max(titleFont.Height, metaFont.Height) + 8f;
                var titleText = string.IsNullOrWhiteSpace(score.Title) ? "点击输入标题" : score.Title;
                var titleWidth = g.MeasureString(titleText, titleFont).Width;
                layout.TitleBounds = Rectangle.Round(new RectangleF(
                    Math.Max(8f, (width - titleWidth) / 2f - 12f),
                    titleTop - 4f,
                    Math.Min(width - 16f, titleWidth + 24f),
                    rowHeight));

                var keyText = score.KeySignature ?? "1=C";
                var tempoText = score.Tempo ?? string.Empty;
                var bpmValue = score.Bpm > 0 ? score.Bpm : 120;
                var bpmText = bpmValue.ToString();
                var composerText = score.Composer ?? string.Empty;
                var gapText = "    ";
                var gapWidth = g.MeasureString(gapText, metaFont).Width;

                var keyWidth = g.MeasureString(keyText, metaFont).Width;
                var tempoWidth = Math.Max(g.MeasureString(tempoText, metaFont).Width, 36f);
                var bpmSegment = "BPM " + bpmText;
                var bpmWidth = g.MeasureString(bpmSegment, metaFont).Width;
                var composerSegment = "作曲: " + composerText;
                var composerWidth = Math.Max(g.MeasureString(composerSegment, metaFont).Width, 56f);

                var metaTotalWidth = keyWidth + gapWidth + tempoWidth + gapWidth + bpmWidth + gapWidth + composerWidth;

                var metaX = options.HeaderMetaLeftAligned
                    ? (float)MarginLeft
                    : Math.Max(8f, (width - metaTotalWidth) / 2f);
                var metaY = metaTop - 4f;
                var metaRowHeight = metaFont.Height + 8f;

                layout.KeyBounds = Rectangle.Round(new RectangleF(metaX, metaY, keyWidth + 12f, metaRowHeight));
                metaX += keyWidth + gapWidth;
                layout.TempoBounds = Rectangle.Round(new RectangleF(metaX - 6f, metaY, tempoWidth + 12f, metaRowHeight));
                metaX += tempoWidth + gapWidth;
                layout.BpmBounds = Rectangle.Round(new RectangleF(metaX - 6f, metaY, bpmWidth + 12f, metaRowHeight));
                metaX += bpmWidth + gapWidth;
                layout.ComposerBounds = Rectangle.Round(new RectangleF(
                    metaX - 6f,
                    metaY,
                    composerWidth + 12f,
                    metaRowHeight));
            }

            return layout;
        }

        public string GetHeaderFieldText(JianpuScore score, ScoreHeaderField field)
        {
            switch (field)
            {
                case ScoreHeaderField.Title:
                    return score.Title ?? string.Empty;
                case ScoreHeaderField.KeySignature:
                    return score.KeySignature ?? string.Empty;
                case ScoreHeaderField.Tempo:
                    return score.Tempo ?? string.Empty;
                case ScoreHeaderField.Bpm:
                    return (score.Bpm > 0 ? score.Bpm : 120).ToString();
                case ScoreHeaderField.Composer:
                    return score.Composer ?? string.Empty;
                default:
                    return string.Empty;
            }
        }

        private void DrawRowLabels(Graphics g, ScoreLayout layout)
        {
            if (layout.Lines.Count == 0)
            {
                return;
            }

            var blockTop = layout.Lines[0].BlockTop;
            DrawRowLabel(g, "主旋律", blockTop + 28);
            DrawRowLabel(g, "副旋律", blockTop + MelodyRowHeight + RowGap + 10);
            DrawRowLabel(g, "歌词", blockTop + MelodyRowHeight + RowGap + SecondaryRowHeight + RowGap + 8);
        }

        private void DrawRowLabel(Graphics g, string text, float y)
        {
            g.DrawString(text, _rowLabelFont, Brushes.DimGray, 8, y);
        }

        private void DrawStaff(
            Graphics g,
            JianpuScore score,
            ScoreLayout layout,
            int selectedMeasureIndex,
            int selectedNoteIndex,
            int selectedInsertIndex,
            IReadOnlyList<int> selectedMeasureIndices,
            int selectedTieIndex,
            int selectedChordMeasureIndex,
            int selectedChordMarkerIndex,
            IReadOnlyList<ScoreNoteRef> selectedNotes,
            ScoreLayoutOptions layoutOptions)
        {
            DrawStaffLineRange(
                g,
                score,
                layout,
                0,
                layout.Lines.Count,
                selectedMeasureIndex,
                selectedNoteIndex,
                selectedInsertIndex,
                selectedMeasureIndices,
                selectedChordMeasureIndex,
                selectedChordMarkerIndex,
                selectedNotes,
                layoutOptions);
            DrawTiesForLineRange(g, score, layout, 0, layout.Lines.Count, selectedTieIndex);
        }

        private void DrawStaffLineRange(
            Graphics g,
            JianpuScore score,
            ScoreLayout layout,
            int firstLineIndex,
            int lineCount,
            ScoreLayoutOptions layoutOptions)
        {
            DrawStaffLineRange(
                g,
                score,
                layout,
                firstLineIndex,
                lineCount,
                -1,
                -1,
                -1,
                null,
                -1,
                -1,
                null,
                layoutOptions);
        }

        private void DrawStaffLineRange(
            Graphics g,
            JianpuScore score,
            ScoreLayout layout,
            int firstLineIndex,
            int lineCount,
            int selectedMeasureIndex,
            int selectedNoteIndex,
            int selectedInsertIndex,
            IReadOnlyList<int> selectedMeasureIndices,
            int selectedChordMeasureIndex,
            int selectedChordMarkerIndex,
            IReadOnlyList<ScoreNoteRef> selectedNotes,
            ScoreLayoutOptions layoutOptions)
        {
            layoutOptions = layoutOptions ?? ScoreLayoutOptions.Default;
            var visibleMeasures = BuildVisibleMeasures(layout, firstLineIndex, lineCount);
            foreach (var measure in layout.Measures)
            {
                if (!visibleMeasures.Contains(measure))
                {
                    continue;
                }

                var measureData = score.Measures[measure.MeasureIndex];
                var isInSelection = IsMeasureSelected(measure.MeasureIndex, selectedMeasureIndices, selectedMeasureIndex);
                var hasSelectedNotes = selectedNotes != null && selectedNotes.Count > 0;
                var isSelectedMeasure = isInSelection && selectedInsertIndex < 0 && !hasSelectedNotes && selectedNoteIndex < 0;

                if (isInSelection)
                {
                    var alpha = measure.MeasureIndex == selectedMeasureIndex ? 42 : 28;
                    using (var brush = new SolidBrush(Color.FromArgb(alpha, 66, 133, 244)))
                    {
                        g.FillRectangle(brush, measure.X, measure.BlockTop, measure.Width, StaffBlockHeight);
                    }

                    if (measure.MeasureIndex == selectedMeasureIndex && selectedMeasureIndices != null && selectedMeasureIndices.Count > 1)
                    {
                        using (var pen = new Pen(Color.FromArgb(180, 41, 98, 255), 2f))
                        {
                            g.DrawRectangle(pen, measure.X + 1, measure.BlockTop + 1, measure.Width - 2, StaffBlockHeight - 2);
                        }
                    }
                }

                DrawMelodyRow(g, measureData, measure, selectedMeasureIndex, selectedNoteIndex, selectedInsertIndex, selectedNotes);
                DrawChordMarkersRow(
                    g,
                    measureData,
                    measure,
                    isSelectedMeasure,
                    selectedMeasureIndex,
                    selectedChordMeasureIndex,
                    selectedChordMarkerIndex,
                    layoutOptions);
                var hasStructuredLyrics = LyricSyllableService.HasStructuredLyrics(measureData);
                DrawLyricRow(
                    g,
                    measureData,
                    measure,
                    isSelectedMeasure,
                    !hasStructuredLyrics && string.IsNullOrWhiteSpace(measureData.LyricText));
                DrawBarLine(g, measure.X, measure.BlockTop, StaffBlockHeight);
                DrawBarLine(g, measure.BarLineX, measure.BlockTop, StaffBlockHeight);
            }
        }

        private static HashSet<MeasureLayout> BuildVisibleMeasures(ScoreLayout layout, int firstLineIndex, int lineCount)
        {
            var visibleMeasures = new HashSet<MeasureLayout>();
            if (layout?.Lines == null || lineCount <= 0)
            {
                return visibleMeasures;
            }

            var endLine = Math.Min(layout.Lines.Count, firstLineIndex + lineCount);
            for (var lineIndex = Math.Max(0, firstLineIndex); lineIndex < endLine; lineIndex++)
            {
                var line = layout.Lines[lineIndex];
                if (line.Measures == null)
                {
                    continue;
                }

                foreach (var measure in line.Measures)
                {
                    visibleMeasures.Add(measure);
                }
            }

            return visibleMeasures;
        }

        private void DrawTiesForLineRange(
            Graphics g,
            JianpuScore score,
            ScoreLayout layout,
            int firstLineIndex,
            int lineCount,
            int selectedTieIndex)
        {
            if (score.Ties == null || score.Ties.Count == 0)
            {
                return;
            }

            var visibleMeasures = BuildVisibleMeasures(layout, firstLineIndex, lineCount);
            for (var i = 0; i < score.Ties.Count; i++)
            {
                var tie = score.Ties[i];
                var startLayout = FindMeasureLayout(layout, tie.StartMeasureIndex);
                var endLayout = FindMeasureLayout(layout, tie.EndMeasureIndex);
                if (startLayout == null
                    || endLayout == null
                    || !visibleMeasures.Contains(startLayout)
                    || !visibleMeasures.Contains(endLayout))
                {
                    continue;
                }

                if (!TryGetTieGeometry(score, layout, tie, out var geometry))
                {
                    continue;
                }

                var isSelected = i == selectedTieIndex;
                if (isSelected)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(48, 66, 133, 244)))
                    using (var path = new GraphicsPath())
                    {
                        path.AddBezier(
                            geometry.X1,
                            geometry.BaseY,
                            geometry.X1,
                            geometry.ArchTop,
                            geometry.X2,
                            geometry.ArchTop,
                            geometry.X2,
                            geometry.BaseY);
                        g.FillPath(brush, path);
                    }
                }

                var color = isSelected ? AppTheme.TieActive : AppTheme.TieInactive;
                var width = isSelected ? 3f : 2f;
                using (var pen = new Pen(color, width))
                {
                    if (isSelected)
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                    }

                    g.DrawBezier(
                        pen,
                        geometry.X1,
                        geometry.BaseY,
                        geometry.X1,
                        geometry.ArchTop,
                        geometry.X2,
                        geometry.ArchTop,
                        geometry.X2,
                        geometry.BaseY);
                }
            }
        }

        private ScoreHitResult HitTestTies(JianpuScore score, ScoreLayout layout, Point point)
        {
            if (score?.Ties == null || score.Ties.Count == 0)
            {
                return null;
            }

            const float hitThreshold = 8f;
            for (var i = score.Ties.Count - 1; i >= 0; i--)
            {
                if (!TryGetTieGeometry(score, layout, score.Ties[i], out var geometry))
                {
                    continue;
                }

                if (!IsPointNearTie(point, geometry, hitThreshold))
                {
                    continue;
                }

                var bounds = Rectangle.FromLTRB(
                    (int)Math.Floor(geometry.X1) - 4,
                    (int)Math.Floor(geometry.ArchTop) - 4,
                    (int)Math.Ceiling(geometry.X2) + 4,
                    (int)Math.Ceiling(geometry.BaseY) + 4);
                return new ScoreHitResult
                {
                    HitType = ScoreHitType.Tie,
                    TieIndex = i,
                    MeasureIndex = score.Ties[i].StartMeasureIndex,
                    Bounds = bounds.ToIntRect()
                };
            }

            return null;
        }

        private void DrawTies(Graphics g, JianpuScore score, ScoreLayout layout, int selectedTieIndex)
        {
            if (score.Ties == null || score.Ties.Count == 0)
            {
                return;
            }

            for (var i = 0; i < score.Ties.Count; i++)
            {
                if (!TryGetTieGeometry(score, layout, score.Ties[i], out var geometry))
                {
                    continue;
                }

                var isSelected = i == selectedTieIndex;
                if (isSelected)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(48, 66, 133, 244)))
                    using (var path = new GraphicsPath())
                    {
                        path.AddBezier(
                            geometry.X1,
                            geometry.BaseY,
                            geometry.X1,
                            geometry.ArchTop,
                            geometry.X2,
                            geometry.ArchTop,
                            geometry.X2,
                            geometry.BaseY);
                        g.FillPath(brush, path);
                    }
                }

                var color = isSelected ? AppTheme.TieActive : AppTheme.TieInactive;
                var width = isSelected ? 3f : 2f;
                using (var pen = new Pen(color, width))
                {
                    if (isSelected)
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                    }

                    g.DrawBezier(
                        pen,
                        geometry.X1,
                        geometry.BaseY,
                        geometry.X1,
                        geometry.ArchTop,
                        geometry.X2,
                        geometry.ArchTop,
                        geometry.X2,
                        geometry.BaseY);
                }
            }
        }

        private static bool TryGetTieGeometry(JianpuScore score, ScoreLayout layout, JianpuTie tie, out TieGeometry geometry)
        {
            geometry = default;
            var startLayout = FindMeasureLayout(layout, tie.StartMeasureIndex);
            var endLayout = FindMeasureLayout(layout, tie.EndMeasureIndex);
            if (startLayout == null || endLayout == null)
            {
                return false;
            }

            if (tie.StartMeasureIndex < 0 || tie.StartMeasureIndex >= score.Measures.Count
                || tie.EndMeasureIndex < 0 || tie.EndMeasureIndex >= score.Measures.Count)
            {
                return false;
            }

            var startMeasure = score.Measures[tie.StartMeasureIndex];
            var endMeasure = score.Measures[tie.EndMeasureIndex];
            if (startMeasure.MelodyNotes == null || endMeasure.MelodyNotes == null
                || tie.StartNoteIndex < 0 || tie.StartNoteIndex >= startMeasure.MelodyNotes.Count
                || tie.EndNoteIndex < 0 || tie.EndNoteIndex >= endMeasure.MelodyNotes.Count)
            {
                return false;
            }

            var startScale = startLayout.MelodyScale;
            var endScale = endLayout.MelodyScale;
            var startMinWidth = startScale < 0.999
                ? Math.Max(6, (int)Math.Round(MinNoteWidth * startScale))
                : MinNoteWidth;
            var endMinWidth = endScale < 0.999
                ? Math.Max(6, (int)Math.Round(MinNoteWidth * endScale))
                : MinNoteWidth;
            var startCount = startMeasure.MelodyNotes.Count;
            var endCount = endMeasure.MelodyNotes.Count;

            GetNoteDrawBounds(startLayout, tie.StartNoteIndex, startCount, startScale, startMinWidth, out var startX, out var startWidth);
            GetNoteDrawBounds(endLayout, tie.EndNoteIndex, endCount, endScale, endMinWidth, out var endX, out var endWidth);

            var x1 = GetNoteHeadCenterX(startX, startWidth);
            var x2 = GetNoteHeadCenterX(endX, endWidth);
            if (x2 <= x1)
            {
                return false;
            }

            geometry = new TieGeometry
            {
                X1 = x1,
                X2 = x2,
                BaseY = startLayout.BlockTop + 8f,
                ArchTop = startLayout.BlockTop - 6f
            };
            return true;
        }

        private static bool IsPointNearTie(Point point, TieGeometry geometry, float threshold)
        {
            if (point.X < geometry.X1 - threshold || point.X > geometry.X2 + threshold
                || point.Y < geometry.ArchTop - threshold || point.Y > geometry.BaseY + threshold)
            {
                return false;
            }

            const int steps = 24;
            var thresholdSq = threshold * threshold;
            for (var i = 0; i <= steps; i++)
            {
                var t = (float)i / steps;
                var curvePoint = EvaluateCubicBezier(
                    t,
                    geometry.X1,
                    geometry.BaseY,
                    geometry.X1,
                    geometry.ArchTop,
                    geometry.X2,
                    geometry.ArchTop,
                    geometry.X2,
                    geometry.BaseY);
                var dx = point.X - curvePoint.X;
                var dy = point.Y - curvePoint.Y;
                if (dx * dx + dy * dy <= thresholdSq)
                {
                    return true;
                }
            }

            return false;
        }

        private static PointF EvaluateCubicBezier(float t, float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3)
        {
            var u = 1f - t;
            var tt = t * t;
            var uu = u * u;
            var uuu = uu * u;
            var ttt = tt * t;
            var x = uuu * x0 + 3f * uu * t * x1 + 3f * u * tt * x2 + ttt * x3;
            var y = uuu * y0 + 3f * uu * t * y1 + 3f * u * tt * y2 + ttt * y3;
            return new PointF(x, y);
        }

        private struct TieGeometry
        {
            public float X1;
            public float X2;
            public float BaseY;
            public float ArchTop;
        }

        private void DrawMelodyRow(
            Graphics g,
            JianpuMeasure measure,
            MeasureLayout layout,
            int selectedMeasureIndex,
            int selectedNoteIndex,
            int selectedInsertIndex,
            IReadOnlyList<ScoreNoteRef> selectedNotes)
        {
            if (layout.MeasureIndex == selectedMeasureIndex && selectedInsertIndex >= 0)
            {
                DrawGapCaret(g, layout, selectedInsertIndex);
            }

            var melodyScale = layout.MelodyScale;
            var minDrawWidth = melodyScale < 0.999
                ? Math.Max(6, (int)Math.Round(MinNoteWidth * melodyScale))
                : MinNoteWidth;
            var noteCount = measure.MelodyNotes.Count;
            for (var i = 0; i < noteCount; i++)
            {
                var isSelected = selectedNotes != null && selectedNotes.Count > 0
                    ? selectedNotes.Any(note => note.MeasureIndex == layout.MeasureIndex && note.NoteIndex == i)
                    : layout.MeasureIndex == selectedMeasureIndex && i == selectedNoteIndex;
                GetNoteDrawBounds(layout, i, noteCount, melodyScale, minDrawWidth, out var noteX, out var noteWidth);

                DrawNote(
                    g,
                    measure,
                    i,
                    measure.MelodyNotes[i],
                    noteX,
                    layout.BlockTop,
                    noteWidth,
                    isSelected);
            }

            DrawBeatGroupUnderlines(g, measure, layout, melodyScale, minDrawWidth);
            DrawOrnaments(g, measure, layout, melodyScale, minDrawWidth);
        }

        private void DrawOrnaments(
            Graphics g,
            JianpuMeasure measure,
            MeasureLayout layout,
            double melodyScale,
            int minDrawWidth)
        {
            OrnamentService.NormalizeMeasure(measure);
            if (measure.Ornaments == null || measure.Ornaments.Count == 0)
            {
                return;
            }

            var noteCount = measure.MelodyNotes?.Count ?? 0;
            if (noteCount == 0)
            {
                return;
            }

            var grouped = measure.Ornaments
                .GroupBy(item => OrnamentService.ResolveNoteIndex(measure, item))
                .Where(group => group.Key >= 0 && group.Key < noteCount)
                .OrderBy(group => group.Key);
            foreach (var group in grouped)
            {
                GetNoteDrawBounds(layout, group.Key, noteCount, melodyScale, minDrawWidth, out var noteX, out var noteWidth);
                var noteOrnaments = NoteTopAnnotationPlanner.GetOrnamentsForNote(measure, group.Key);
                NoteTopAnnotationLayout topLayout = null;
                if (_activeLayoutOptions != null && _activeLayoutOptions.CompactAccidentalGlyphs)
                {
                    topLayout = NoteTopAnnotationPlanner.Plan(
                        measure.MelodyNotes[group.Key],
                        noteX,
                        Math.Min(NoteCellWidth, noteWidth),
                        noteOrnaments,
                        true);
                }

                var stackIndex = 0;
                foreach (var ornament in group)
                {
                    DrawOrnamentGlyph(
                        g,
                        ornament,
                        noteX,
                        noteWidth,
                        layout.BlockTop,
                        stackIndex,
                        group.Count(),
                        topLayout);
                    stackIndex++;
                }
            }
        }

        private void DrawOrnamentGlyph(
            Graphics g,
            JianpuOrnament ornament,
            int noteX,
            int noteWidth,
            int rowTop,
            int stackIndex,
            int stackCount,
            NoteTopAnnotationLayout topLayout = null)
        {
            var glyph = OrnamentService.GetPlaceholderGlyph(ornament.Type);
            if (string.IsNullOrEmpty(glyph))
            {
                return;
            }

            var headWidth = Math.Min(NoteCellWidth, noteWidth);
            Font font;
            if (topLayout != null && ornament.Type != OrnamentType.Fermata)
            {
                font = UsesLatinOrnamentFont(ornament.Type)
                    ? new Font("Arial", 11f, FontStyle.Italic)
                    : new Font("Microsoft YaHei", 11f, FontStyle.Regular);
            }
            else
            {
                font = UsesLatinOrnamentFont(ornament.Type) ? _ornamentLatinFont : _ornamentFont;
            }

            var size = g.MeasureString(glyph, font);
            float anchorX;
            float drawY;
            if (topLayout != null)
            {
                anchorX = topLayout.GetOrnamentAnchorX(ornament.Type, noteX, headWidth);
                drawY = rowTop + topLayout.GetOrnamentY(ornament.Type);
            }
            else
            {
                anchorX = GetOrnamentAnchorX(ornament.Type, noteX, noteWidth);
                drawY = rowTop + GetOrnamentTopOffset(ornament.Type);
            }

            var totalWidth = stackCount * size.Width + Math.Max(0, stackCount - 1) * 2f;
            var drawX = anchorX - totalWidth / 2f + stackIndex * (size.Width + 2f);
            using (var ink = CreateInkBrush())
            {
                g.DrawString(glyph, font, ink, drawX, drawY);
            }

            if (!ReferenceEquals(font, _ornamentFont) && !ReferenceEquals(font, _ornamentLatinFont))
            {
                font.Dispose();
            }
        }

        private static bool UsesLatinOrnamentFont(OrnamentType type)
        {
            return type == OrnamentType.Trill || type == OrnamentType.Mordent;
        }

        private static float GetOrnamentAnchorX(OrnamentType type, int noteX, int noteWidth)
        {
            var headWidth = Math.Min(NoteCellWidth, noteWidth);
            if (type == OrnamentType.GraceNote)
            {
                return noteX + Math.Min(14f, headWidth * 0.25f);
            }

            return GetNoteHeadCenterX(noteX, noteWidth);
        }

        private static float GetOrnamentTopOffset(OrnamentType type)
        {
            return type == OrnamentType.Fermata ? 0f : 2f;
        }

        private static float GetNoteHeadCenterX(int noteX, int noteWidth)
        {
            var headWidth = Math.Min(NoteCellWidth, noteWidth);
            return noteX + headWidth / 2f;
        }

        private static void GetNoteDrawBounds(
            MeasureLayout layout,
            int noteIndex,
            int noteCount,
            double melodyScale,
            int minDrawWidth,
            out int noteX,
            out int noteWidth)
        {
            noteWidth = Math.Max(minDrawWidth, (int)Math.Round(layout.GetNoteWidth(noteIndex) * melodyScale));
            noteX = layout.X + (int)Math.Round(layout.GetNoteOffset(noteIndex) * melodyScale);
            if (melodyScale < 0.999 && noteIndex == noteCount - 1)
            {
                noteWidth = Math.Max(minDrawWidth, layout.X + layout.Width - noteX);
            }
        }

        private static List<List<int>> GroupNotesByQuarterBeat(List<JianpuNote> notes)
        {
            var groups = new List<List<int>>();
            var current = new List<int>();
            var sum = 0.0;

            for (var i = 0; i < notes.Count; i++)
            {
                var duration = GetDurationUnits(notes[i]);
                if (duration <= 0)
                {
                    duration = 1;
                }

                if (current.Count > 0 && sum + duration > 1.0001)
                {
                    groups.Add(current);
                    current = new List<int>();
                    sum = 0;
                }

                current.Add(i);
                sum += duration;

                if (sum >= 0.9999)
                {
                    groups.Add(current);
                    current = new List<int>();
                    sum = 0;
                }
            }

            if (current.Count > 0)
            {
                groups.Add(current);
            }

            return groups;
        }

        private void DrawBeatGroupUnderlines(
            Graphics g,
            JianpuMeasure measure,
            MeasureLayout layout,
            double melodyScale,
            int minDrawWidth)
        {
            var notes = measure.MelodyNotes;
            if (notes == null || notes.Count == 0)
            {
                return;
            }

            var groups = GroupNotesByQuarterBeat(notes);
            var rowTop = layout.BlockTop;
            var noteCount = notes.Count;

            foreach (var group in groups)
            {
                if (group.Count == 0)
                {
                    continue;
                }

                var maxUnderlines = 0;
                foreach (var noteIndex in group)
                {
                    maxUnderlines = Math.Max(maxUnderlines, notes[noteIndex].Underlines);
                }

                if (maxUnderlines == 0)
                {
                    continue;
                }

                for (var underlineIndex = 0; underlineIndex < maxUnderlines; underlineIndex++)
                {
                    var spanStart = -1;
                    var spanEnd = -1;
                    foreach (var noteIndex in group)
                    {
                        if (notes[noteIndex].Underlines <= underlineIndex)
                        {
                            continue;
                        }

                        if (spanStart < 0)
                        {
                            spanStart = noteIndex;
                        }

                        spanEnd = noteIndex;
                    }

                    if (spanStart < 0 || spanEnd < 0)
                    {
                        continue;
                    }

                    GetNoteDrawBounds(layout, spanStart, noteCount, melodyScale, minDrawWidth, out var startX, out _);
                    GetNoteDrawBounds(layout, spanEnd, noteCount, melodyScale, minDrawWidth, out var endNoteX, out var endNoteWidth);
                    var endX = endNoteX + endNoteWidth;
                    var lineY = rowTop + 62 + underlineIndex * 6;
                    g.DrawLine(Pens.Black, startX, lineY, endX, lineY);
                }
            }
        }

        private void DrawGapCaret(Graphics g, MeasureLayout layout, int insertIndex)
        {
            var x = layout.X + layout.GetInsertOffset(insertIndex);
            var top = layout.BlockTop + 8;
            var height = MelodyRowHeight - 16;

            using (var brush = new SolidBrush(Color.FromArgb(80, 76, 175, 80)))
            {
                g.FillRectangle(brush, x - GapCaretWidth / 2, top, GapCaretWidth, height);
            }

            using (var pen = new Pen(Color.FromArgb(220, 46, 125, 50), 2f))
            {
                g.DrawLine(pen, x, top, x, top + height);
            }
        }

        private ScoreHitResult HitTestChordMarkers(JianpuScore score, ScoreLayout layout, Point point)
        {
            var boundsList = ChordMarkerLayout.BuildBounds(score, layout.Measures);
            for (var i = boundsList.Count - 1; i >= 0; i--)
            {
                var bounds = boundsList[i];
                if (bounds.DeleteBounds.Contains(point))
                {
                    return new ScoreHitResult
                    {
                        HitType = ScoreHitType.ChordDelete,
                        MeasureIndex = bounds.MeasureIndex,
                        ChordMarkerIndex = bounds.MarkerIndex,
                        Bounds = bounds.DeleteBounds.ToIntRect()
                    };
                }

                if (bounds.DragHandleBounds.Contains(point))
                {
                    return new ScoreHitResult
                    {
                        HitType = ScoreHitType.ChordDragHandle,
                        MeasureIndex = bounds.MeasureIndex,
                        ChordMarkerIndex = bounds.MarkerIndex,
                        Bounds = bounds.DragHandleBounds.ToIntRect()
                    };
                }

                if (bounds.TextBoxBounds.Contains(point))
                {
                    return new ScoreHitResult
                    {
                        HitType = ScoreHitType.ChordMarker,
                        MeasureIndex = bounds.MeasureIndex,
                        ChordMarkerIndex = bounds.MarkerIndex,
                        Bounds = bounds.TextBoxBounds.ToIntRect()
                    };
                }
            }

            return null;
        }

        private ScoreHitResult HitTestSecondaryRow(JianpuScore score, MeasureLayout measure, Point point, int secondaryTop)
        {
            var measureData = score.Measures[measure.MeasureIndex];
            ChordMarkerService.NormalizeMeasure(measureData);
            var bounds = ChordMarkerLayout.GetSecondaryRowBounds(measure);
            if (!bounds.Contains(point))
            {
                return new ScoreHitResult
                {
                    HitType = ScoreHitType.ChordRow,
                    MeasureIndex = measure.MeasureIndex,
                    Bounds = bounds.ToIntRect()
                };
            }

            if (measureData.ChordMarkers.Count < JianpuMeasure.MaxChordMarkers)
            {
                var duration = ScoreMidiSchedule.GetMeasureDurationUnits(measureData);
                var beat = ChordMarkerService.MapXToBeat(measure.X, measure.Width, point.X, duration);
                var occupied = measureData.ChordMarkers.Any(marker => Math.Abs(marker.BeatPosition - beat) < 0.001);
                if (!occupied)
                {
                    return new ScoreHitResult
                    {
                        HitType = ScoreHitType.ChordAddSlot,
                        MeasureIndex = measure.MeasureIndex,
                        Bounds = bounds.ToIntRect()
                    };
                }
            }

            return new ScoreHitResult
            {
                HitType = ScoreHitType.ChordRow,
                MeasureIndex = measure.MeasureIndex,
                Bounds = bounds.ToIntRect()
            };
        }

        private void DrawChordMarkersRow(
            Graphics g,
            JianpuMeasure measureData,
            MeasureLayout measure,
            bool isSelectedMeasure,
            int selectedMeasureIndex,
            int selectedChordMeasureIndex,
            int selectedChordMarkerIndex,
            ScoreLayoutOptions layoutOptions)
        {
            ChordMarkerService.NormalizeMeasure(measureData);
            var rowBounds = ChordMarkerLayout.GetSecondaryRowBounds(measure);
            var textOnly = layoutOptions.ChordMarkersTextOnly;
            var showAffordances = layoutOptions.ShowChordEditorAffordances;

            if (showAffordances && isSelectedMeasure && measureData.ChordMarkers.Count == 0)
            {
                using (var brush = new SolidBrush(Color.FromArgb(28, 120, 144, 156)))
                {
                    g.FillRectangle(brush, rowBounds);
                }
            }

            if (showAffordances)
            {
                var duration = ScoreMidiSchedule.GetMeasureDurationUnits(measureData);
                var beatCount = (int)Math.Max(1, Math.Round(duration));
                using (var gridPen = new Pen(Color.FromArgb(36, 120, 144, 156), 1f))
                {
                    for (var beat = 0; beat <= beatCount; beat++)
                    {
                        var x = ChordMarkerLayout.GetBeatAnchorX(measure, measureData, beat);
                        g.DrawLine(gridPen, x, rowBounds.Top + 2, x, rowBounds.Bottom - 2);
                    }
                }
            }

            for (var i = 0; i < measureData.ChordMarkers.Count; i++)
            {
                var marker = measureData.ChordMarkers[i];
                var markerBounds = ChordMarkerLayout.GetMarkerBounds(measure, measureData, measure.MeasureIndex, i, marker);
                if (textOnly)
                {
                    var isSelected = measure.MeasureIndex == selectedChordMeasureIndex && i == selectedChordMarkerIndex;
                    DrawChordMarkerTextOnly(g, markerBounds, marker.Text, rowBounds, isSelected, showAffordances);
                }
                else
                {
                    var isSelected = measure.MeasureIndex == selectedChordMeasureIndex && i == selectedChordMarkerIndex;
                    DrawChordMarkerChrome(g, markerBounds, marker.Text, isSelected);
                }
            }

            if (showAffordances
                && isSelectedMeasure
                && measure.MeasureIndex == selectedMeasureIndex
                && measureData.ChordMarkers.Count < JianpuMeasure.MaxChordMarkers)
            {
                using (var font = new Font("Microsoft YaHei", 8f, FontStyle.Regular))
                {
                    var hint = "+ 点击空白拍位添加和弦";
                    g.DrawString(hint, font, Brushes.DimGray, rowBounds.Left + 4, rowBounds.Bottom - 14);
                }
            }
        }

        private void DrawChordMarkerTextOnly(
            Graphics g,
            ChordMarkerBounds bounds,
            string text,
            Rectangle rowBounds,
            bool isSelected,
            bool showAffordances)
        {
            var y = rowBounds.Top + (rowBounds.Height - _secondaryFont.Height) / 2f;
            if (showAffordances && isSelected)
            {
                var chromeBounds = Rectangle.Union(
                    bounds.TextBoxBounds,
                    Rectangle.Union(bounds.DeleteBounds, bounds.DragHandleBounds));
                using (var brush = new SolidBrush(Color.FromArgb(255, 255, 240)))
                using (var pen = new Pen(Color.FromArgb(220, 41, 98, 255), 2f))
                {
                    g.FillRectangle(brush, chromeBounds);
                    g.DrawRectangle(pen, chromeBounds);
                }

                using (var handleFont = new Font("Arial", 8f, FontStyle.Bold))
                using (var deleteFont = new Font("Arial", 10f, FontStyle.Bold))
                {
                    g.DrawString("::", handleFont, Brushes.DimGray, bounds.DragHandleBounds.Left + 1, bounds.DragHandleBounds.Top + 4);
                    g.DrawString("x", deleteFont, Brushes.IndianRed, bounds.DeleteBounds.Left + 4, bounds.DeleteBounds.Top + 2);
                }
            }

            var displayText = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
            if (string.IsNullOrEmpty(displayText))
            {
                return;
            }

            using (var brush = new SolidBrush(AppTheme.PrimaryText))
            {
                g.DrawString(displayText, _secondaryFont, brush, bounds.AnchorX, y);
            }
        }

        private void DrawChordMarkerChrome(Graphics g, ChordMarkerBounds bounds, string text, bool isSelected)
        {
            var backColor = isSelected ? AppTheme.ChordSelectedBackground : AppTheme.ChordBackground;
            var borderColor = isSelected ? AppTheme.ChordSelectedBorder : AppTheme.ChordBorder;

            using (var backBrush = new SolidBrush(backColor))
            using (var borderPen = new Pen(borderColor, isSelected ? 2f : 1f))
            {
                g.FillRectangle(backBrush, bounds.TextBoxBounds);
                g.DrawRectangle(borderPen, bounds.TextBoxBounds);
                g.FillRectangle(backBrush, bounds.DeleteBounds);
                g.DrawRectangle(borderPen, bounds.DeleteBounds);
                g.FillRectangle(backBrush, bounds.DragHandleBounds);
                g.DrawRectangle(borderPen, bounds.DragHandleBounds);
            }

            using (var handleFont = new Font("Arial", 8f, FontStyle.Bold))
            using (var deleteFont = new Font("Arial", 10f, FontStyle.Bold))
            using (var textFont = _secondaryFont)
            {
                using (var handleBrush = new SolidBrush(AppTheme.SecondaryText))
                using (var deleteBrush = new SolidBrush(Color.IndianRed))
                using (var textBrush = new SolidBrush(AppTheme.PrimaryText))
                {
                    g.DrawString("::", handleFont, handleBrush, bounds.DragHandleBounds.Left + 1, bounds.DragHandleBounds.Top + 4);
                    g.DrawString("x", deleteFont, deleteBrush, bounds.DeleteBounds.Left + 4, bounds.DeleteBounds.Top + 2);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var textRect = new RectangleF(
                            bounds.TextBoxBounds.Left + 4,
                            bounds.TextBoxBounds.Top,
                            bounds.TextBoxBounds.Width - 8,
                            bounds.TextBoxBounds.Height);
                        using (var format = new StringFormat
                        {
                            Alignment = StringAlignment.Near,
                            LineAlignment = StringAlignment.Center,
                            Trimming = StringTrimming.EllipsisCharacter,
                            FormatFlags = StringFormatFlags.NoWrap
                        })
                        {
                            g.DrawString(text.Trim(), textFont, textBrush, textRect, format);
                        }
                    }
                }
            }
        }

        private void DrawLyricRow(
            Graphics g,
            JianpuMeasure measureData,
            MeasureLayout layout,
            bool isSelectedMeasure,
            bool isEmpty)
        {
            var rowTop = GetLyricRowTop(layout);
            var bounds = GetTextCellBounds(layout, rowTop, TextRowHeight);
            var fontSize = Math.Max(8f, LyricBaseFontSize * (float)layout.MelodyScale);
            using (var font = new Font("Arial", fontSize, FontStyle.Bold))
            {
                if (LyricSyllableService.HasStructuredLyrics(measureData))
                {
                    DrawStructuredLyricRow(g, measureData, layout, bounds, rowTop, isSelectedMeasure, isEmpty, font);
                    return;
                }

                var text = measureData.LyricText ?? string.Empty;
                DrawTextRowCore(g, text, bounds, isSelectedMeasure, isEmpty, font, StringAlignment.Near);
            }
        }

        private void DrawStructuredLyricRow(
            Graphics g,
            JianpuMeasure measureData,
            MeasureLayout layout,
            Rectangle bounds,
            int rowTop,
            bool isSelectedMeasure,
            bool isEmpty,
            Font font)
        {
            if (isSelectedMeasure && isEmpty)
            {
                using (var pen = new Pen(Color.FromArgb(180, 180, 180)) { DashStyle = DashStyle.Dot })
                {
                    g.DrawRectangle(pen, bounds);
                }
            }

            var melodyScale = layout.MelodyScale;
            var minDrawWidth = melodyScale < 0.999
                ? Math.Max(6, (int)Math.Round(MinNoteWidth * melodyScale))
                : MinNoteWidth;
            var noteCount = measureData.MelodyNotes?.Count ?? 0;
            foreach (var syllable in measureData.LyricSyllables)
            {
                if (string.IsNullOrEmpty(syllable?.Text))
                {
                    continue;
                }

                var noteIndex = LyricSyllableService.ResolveNoteIndex(measureData, syllable);
                if (noteIndex < 0 || noteIndex >= noteCount)
                {
                    continue;
                }

                GetNoteDrawBounds(layout, noteIndex, noteCount, melodyScale, minDrawWidth, out var noteX, out var noteWidth);
                var centerX = GetNoteHeadCenterX(noteX, noteWidth);
                var textWidth = (int)Math.Ceiling(g.MeasureString(syllable.Text, font).Width) + 4;
                var syllableWidth = Math.Max(12, Math.Min(noteWidth, textWidth));
                var syllableLeft = (int)Math.Round(centerX - syllableWidth / 2f);
                var syllableBounds = new Rectangle(
                    syllableLeft,
                    rowTop + 2,
                    syllableWidth,
                    TextRowHeight - 4);

                using (var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                })
                using (var ink = CreateInkBrush())
                {
                    var rect = new RectangleF(
                        syllableBounds.X,
                        syllableBounds.Y,
                        syllableBounds.Width,
                        syllableBounds.Height);
                    g.DrawString(syllable.Text, font, ink, rect, format);
                }
            }
        }

        private void DrawTextRowCore(
            Graphics g,
            string text,
            Rectangle bounds,
            bool isSelectedMeasure,
            bool isEmpty,
            Font font,
            StringAlignment alignment)
        {
            if (isSelectedMeasure && isEmpty)
            {
                using (var pen = new Pen(Color.FromArgb(180, 180, 180)) { DashStyle = DashStyle.Dot })
                {
                    g.DrawRectangle(pen, bounds);
                }
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var rect = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            using (var format = new StringFormat
            {
                Alignment = alignment,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
            {
                using (var ink = CreateInkBrush())
                {
                    g.DrawString(text, font, ink, rect, format);
                }
            }
        }

        private const float CompactAccidentalFontSize = 10f;

        private void DrawNote(
            Graphics g,
            JianpuMeasure measure,
            int noteIndex,
            JianpuNote note,
            int x,
            int y,
            int noteWidth,
            bool isSelected)
        {
            var headWidth = Math.Min(NoteCellWidth, noteWidth);
            var useTopLayout = _activeLayoutOptions != null && _activeLayoutOptions.CompactAccidentalGlyphs;
            NoteTopAnnotationLayout topLayout = null;
            if (useTopLayout && note != null && note.Type == NoteType.Note)
            {
                var ornaments = measure == null
                    ? new List<JianpuOrnament>()
                    : NoteTopAnnotationPlanner.GetOrnamentsForNote(measure, noteIndex);
                topLayout = NoteTopAnnotationPlanner.Plan(note, x, headWidth, ornaments, true);
            }

            if (isSelected)
            {
                using (var brush = new SolidBrush(Color.FromArgb(90, 255, 214, 102)))
                {
                    g.FillRectangle(brush, x + 2, y + 2, noteWidth - 4, MelodyRowHeight - 4);
                }

                using (var pen = new Pen(Color.FromArgb(220, 180, 60), 2f))
                {
                    g.DrawRectangle(pen, x + 2, y + 2, noteWidth - 4, MelodyRowHeight - 4);
                }
            }

            using (var ink = CreateInkBrush())
            using (var inkPen = CreateInkPen(2f))
            {
                var headCenterX = x + headWidth / 2f;
                if (note.Type == NoteType.Rest)
                {
                    DrawCenteredNoteText(g, "0", x, y, headWidth, _noteFont, ink);
                }
                else if (topLayout != null)
                {
                    DrawCenteredNoteText(g, JianpuPitchCodec.GetDisplayDegree(note).ToString(), x, y, headWidth, _noteFont, ink);
                    DrawCompactAccidentalMark(g, note, y, topLayout, ink);
                }
                else
                {
                    DrawCenteredNoteText(
                        g,
                        JianpuPitchCodec.GetPitchDisplayText(note),
                        x,
                        y,
                        headWidth,
                        _noteFont,
                        ink);
                }

                if (topLayout != null)
                {
                    DrawNoteOctaveDots(g, note, y, topLayout, ink);
                }
                else
                {
                    DrawNoteOctaveDots(g, note, x, y, headCenterX, ink);
                }
                DrawNoteDottedAndDashes(g, note, x, y, noteWidth, headWidth, headCenterX, ink, inkPen);
            }
        }

        private void DrawCenteredNoteText(
            Graphics g,
            string text,
            int x,
            int y,
            int headWidth,
            Font font,
            Brush ink)
        {
            var textSize = g.MeasureString(text, font);
            var textX = x + (headWidth - textSize.Width) / 2f;
            var textY = y + 18f;
            g.DrawString(text, font, ink, textX, textY);
        }

        private void DrawCompactAccidentalMark(
            Graphics g,
            JianpuNote note,
            int y,
            NoteTopAnnotationLayout topLayout,
            Brush ink)
        {
            if (topLayout == null || !topLayout.HasAccidental)
            {
                return;
            }

            var mark = JianpuPitchCodec.GetAccidentalMark(note);
            if (string.IsNullOrEmpty(mark))
            {
                return;
            }

            using (var accidentalFont = new Font("Arial", CompactAccidentalFontSize, FontStyle.Bold))
            {
                g.DrawString(mark, accidentalFont, ink, topLayout.AccidentalX, y + topLayout.AccidentalY);
            }
        }

        private void DrawNoteOctaveDots(Graphics g, JianpuNote note, int y, NoteTopAnnotationLayout topLayout, Brush ink)
        {
            if (note.Octave > 0)
            {
                for (var i = 0; i < note.Octave; i++)
                {
                    g.FillEllipse(
                        ink,
                        topLayout.OctaveDotCenterX - 3,
                        y + topLayout.OctaveDotBaseY + i * NoteTopAnnotationLayout.OctaveDotStackSpacing,
                        (int)NoteTopAnnotationLayout.OctaveDotDiameter,
                        (int)NoteTopAnnotationLayout.OctaveDotDiameter);
                }
            }
            else if (note.Octave < 0)
            {
                for (var i = 0; i < Math.Abs(note.Octave); i++)
                {
                    g.FillEllipse(ink, topLayout.HeadCenterX - 3, y + 52 + i * 10, 6, 6);
                }
            }
        }

        private void DrawNoteOctaveDots(Graphics g, JianpuNote note, int x, int y, float headCenterX, Brush ink)
        {
            if (note.Octave > 0)
            {
                for (var i = 0; i < note.Octave; i++)
                {
                    g.FillEllipse(ink, headCenterX - 3, y + 4 + i * 10, 6, 6);
                }
            }
            else if (note.Octave < 0)
            {
                for (var i = 0; i < Math.Abs(note.Octave); i++)
                {
                    g.FillEllipse(ink, headCenterX - 3, y + 52 + i * 10, 6, 6);
                }
            }
        }

        private void DrawNoteDottedAndDashes(
            Graphics g,
            JianpuNote note,
            int x,
            int y,
            int noteWidth,
            int headWidth,
            float headCenterX,
            Brush ink,
            Pen inkPen)
        {
            if (note.Dotted)
            {
                var dotX = Math.Min(x + headWidth - 8, headCenterX + 12);
                g.FillEllipse(ink, dotX, y + 42, 5, 5);
            }

            var extensionWidth = noteWidth - headWidth;
            if (extensionWidth > 0 && note.Dashes > 0)
            {
                for (var i = 0; i < note.Dashes; i++)
                {
                    var dashX = x + headWidth + (extensionWidth * (i + 1)) / (note.Dashes + 1) - 4;
                    g.DrawLine(inkPen, dashX, y + 36, dashX + 8, y + 36);
                }
            }
            else
            {
                for (var i = 0; i < note.Dashes; i++)
                {
                    var dashX = x + headWidth - 8 + i * 12;
                    g.DrawLine(inkPen, dashX, y + 36, dashX + 8, y + 36);
                }
            }
        }

        private void DrawBarLine(Graphics g, int x, int top, int height)
        {
            using (var pen = CreateInkPen(2f))
            {
                g.DrawLine(pen, x, top + 4, x, top + height - 4);
            }
        }

        private static int GetMarginTop(ScoreLayoutOptions options)
        {
            return options.HeaderMarginTop > 0 ? options.HeaderMarginTop : MarginTop;
        }

        private ScoreLayout BuildLayout(JianpuScore score, int maxWidth, ScoreLayoutOptions options)
        {
            options = options ?? ScoreLayoutOptions.Default;
            var layout = new ScoreLayout();
            if (score.Measures == null || score.Measures.Count == 0)
            {
                return layout;
            }

            var marginTop = GetMarginTop(options);
            var usableWidth = Math.Max(
                options.MeasuresPerLine > 0 ? 200 : 500,
                maxWidth - MarginLeft - 16);
            var slotWidth = options.MeasuresPerLine > 0 && options.EqualizeMeasureWidths
                ? usableWidth / options.MeasuresPerLine
                : 0;
            var x = MarginLeft;
            var blockTop = marginTop;
            var line = new StaffLineLayout { BlockTop = blockTop };
            layout.Lines.Add(line);
            var maxRight = x;

            for (var index = 0; index < score.Measures.Count; index++)
            {
                var measureWidth = slotWidth > 0
                    ? slotWidth
                    : CalculateMeasureWidth(score.Measures[index], options.NoteWidthScale);

                var needNewLine = false;
                if (options.MeasuresPerLine > 0)
                {
                    needNewLine = line.Measures.Count >= options.MeasuresPerLine;
                }
                else if (x > MarginLeft && x + measureWidth > MarginLeft + usableWidth)
                {
                    needNewLine = true;
                }

                if (needNewLine)
                {
                    blockTop += StaffBlockHeight + StaffBlockSpacing;
                    line = new StaffLineLayout { BlockTop = blockTop };
                    layout.Lines.Add(line);
                    x = MarginLeft;
                }

                var measureLayout = new MeasureLayout
                {
                    MeasureIndex = index,
                    X = x,
                    Width = measureWidth,
                    BlockTop = blockTop,
                    BarLineX = x + measureWidth
                };
                measureLayout.ComputeNoteLayout(score.Measures[index], options.NoteWidthScale);
                measureLayout.ApplyMelodyScale(measureWidth);
                layout.Measures.Add(measureLayout);
                line.Measures.Add(measureLayout);

                x += measureWidth;
                maxRight = Math.Max(maxRight, x);
            }

            layout.TotalWidth = options.MeasuresPerLine > 0 && options.EqualizeMeasureWidths
                ? MarginLeft + usableWidth + 24
                : maxRight + 24;
            return layout;
        }

        private static bool IsMeasureSelected(int measureIndex, IReadOnlyList<int> selectedMeasureIndices, int selectedMeasureIndex)
        {
            if (selectedMeasureIndices != null && selectedMeasureIndices.Count > 0)
            {
                foreach (var index in selectedMeasureIndices)
                {
                    if (index == measureIndex)
                    {
                        return true;
                    }
                }

                return false;
            }

            return measureIndex == selectedMeasureIndex;
        }

        private int CalculateMeasureWidth(JianpuMeasure measure, double noteWidthScale = 1.0)
        {
            var melodyWidth = NoteCellWidth;
            if (measure.MelodyNotes != null && measure.MelodyNotes.Count > 0)
            {
                melodyWidth = 0;
                foreach (var note in measure.MelodyNotes)
                {
                    melodyWidth += GetNoteWidth(note, noteWidthScale);
                }
            }

            return Math.Max(MinMeasureWidth, melodyWidth);
        }

        private sealed class ScoreLayout
        {
            public List<StaffLineLayout> Lines { get; set; } = new List<StaffLineLayout>();
            public List<MeasureLayout> Measures { get; set; } = new List<MeasureLayout>();
            public int TotalWidth { get; set; }
        }

        private sealed class StaffLineLayout
        {
            public int BlockTop { get; set; }
            public List<MeasureLayout> Measures { get; set; } = new List<MeasureLayout>();
        }

        public sealed class MeasureLayout
        {
            private int[] _noteWidths = Array.Empty<int>();
            private int[] _noteOffsets = Array.Empty<int>();
            private int _melodyContentWidth;

            public double MelodyScale { get; private set; } = 1.0;

            public int MeasureIndex { get; set; }
            public int X { get; set; }
            public int Width { get; set; }
            public int BlockTop { get; set; }
            public int BarLineX { get; set; }

            public void ComputeNoteLayout(JianpuMeasure measure, double noteWidthScale = 1.0)
            {
                var notes = measure.MelodyNotes ?? new List<JianpuNote>();
                _noteWidths = new int[notes.Count];
                _noteOffsets = new int[notes.Count];
                var offset = 0;
                for (var i = 0; i < notes.Count; i++)
                {
                    _noteOffsets[i] = offset;
                    _noteWidths[i] = JianpuRenderer.GetNoteWidth(notes[i], noteWidthScale);
                    offset += _noteWidths[i];
                }

                _melodyContentWidth = offset;
                MelodyScale = 1.0;
            }

            public void ApplyMelodyScale(int displayWidth)
            {
                var availableWidth = Math.Max(12, displayWidth - 4);
                if (_melodyContentWidth > availableWidth && _melodyContentWidth > 0)
                {
                    MelodyScale = (double)availableWidth / _melodyContentWidth;
                }
                else
                {
                    MelodyScale = 1.0;
                }
            }

            public int GetNoteWidth(int noteIndex)
            {
                if (noteIndex < 0 || noteIndex >= _noteWidths.Length)
                {
                    return NoteCellWidth;
                }

                return _noteWidths[noteIndex];
            }

            public int GetNoteOffset(int noteIndex)
            {
                if (noteIndex < 0 || noteIndex >= _noteOffsets.Length)
                {
                    return 0;
                }

                return _noteOffsets[noteIndex];
            }

            public int GetInsertOffset(int insertIndex)
            {
                if (insertIndex <= 0)
                {
                    return 0;
                }

                if (_noteOffsets.Length == 0)
                {
                    return 0;
                }

                if (insertIndex >= _noteOffsets.Length)
                {
                    return _melodyContentWidth;
                }

                return _noteOffsets[insertIndex];
            }
        }
    }
}
