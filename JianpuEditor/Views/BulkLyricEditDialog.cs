using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace JianpuEditor.Views
{
    public sealed class BulkLyricEditDialog : Form
    {
        private readonly int _measureCount;
        private readonly Func<int, int, IReadOnlyList<string>> _loadLinesForRange;
        private readonly NumericUpDown _fromMeasure;
        private readonly NumericUpDown _toMeasure;
        private readonly CheckBox _realignCheckBox;
        private readonly Panel _linesPanel;
        private readonly List<TextBox> _lineBoxes = new List<TextBox>();
        private bool _suppressRangeSync;

        public BulkLyricEditDialog(
            int measureCount,
            int fromMeasure,
            int toMeasure,
            Func<int, int, IReadOnlyList<string>> loadLinesForRange)
        {
            if (measureCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(measureCount));
            }

            if (loadLinesForRange == null)
            {
                throw new ArgumentNullException(nameof(loadLinesForRange));
            }

            _measureCount = measureCount;
            _loadLinesForRange = loadLinesForRange;

            Text = "批量编辑歌词";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(480, 420);

            var fromLabel = new Label
            {
                Text = "从第",
                Location = new Point(16, 18),
                AutoSize = true
            };
            _fromMeasure = new NumericUpDown
            {
                Location = new Point(56, 14),
                Width = 56,
                Minimum = 1,
                Maximum = measureCount,
                Value = Math.Max(1, Math.Min(measureCount, fromMeasure))
            };
            var toLabel = new Label
            {
                Text = "到第",
                Location = new Point(124, 18),
                AutoSize = true
            };
            _toMeasure = new NumericUpDown
            {
                Location = new Point(164, 14),
                Width = 56,
                Minimum = 1,
                Maximum = measureCount,
                Value = Math.Max(1, Math.Min(measureCount, toMeasure))
            };
            var measureSuffix = new Label
            {
                Text = "小节",
                Location = new Point(228, 18),
                AutoSize = true
            };
            _realignCheckBox = new CheckBox
            {
                Text = "重新对齐当前范围",
                Location = new Point(16, 48),
                AutoSize = true
            };
            _linesPanel = new Panel
            {
                Location = new Point(16, 80),
                Size = new Size(448, 288),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            var okButton = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new Point(308, 382),
                Width = 76
            };
            var cancelButton = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(392, 382),
                Width = 76
            };

            Controls.Add(fromLabel);
            Controls.Add(_fromMeasure);
            Controls.Add(toLabel);
            Controls.Add(_toMeasure);
            Controls.Add(measureSuffix);
            Controls.Add(_realignCheckBox);
            Controls.Add(_linesPanel);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
            AcceptButton = okButton;
            CancelButton = cancelButton;

            _fromMeasure.ValueChanged += OnRangeChanged;
            _toMeasure.ValueChanged += OnRangeChanged;

            RebuildLineEditors();
        }

        public int FromMeasure
        {
            get { return (int)_fromMeasure.Value; }
        }

        public int ToMeasure
        {
            get { return (int)_toMeasure.Value; }
        }

        public bool Realign
        {
            get { return _realignCheckBox.Checked; }
        }

        public IReadOnlyList<string> LyricLines
        {
            get
            {
                var lines = new List<string>(_lineBoxes.Count);
                for (var i = 0; i < _lineBoxes.Count; i++)
                {
                    lines.Add(_lineBoxes[i].Text ?? string.Empty);
                }

                return lines;
            }
        }

        private void OnRangeChanged(object sender, EventArgs e)
        {
            if (_suppressRangeSync)
            {
                return;
            }

            NormalizeRangeControls();
            RebuildLineEditors();
        }

        private void NormalizeRangeControls()
        {
            var from = (int)_fromMeasure.Value;
            var to = (int)_toMeasure.Value;
            if (from > to)
            {
                _suppressRangeSync = true;
                _toMeasure.Value = from;
                _suppressRangeSync = false;
            }
        }

        private void RebuildLineEditors()
        {
            _linesPanel.SuspendLayout();
            _linesPanel.Controls.Clear();
            _lineBoxes.Clear();

            var from = FromMeasure;
            var to = ToMeasure;
            var lines = _loadLinesForRange(from - 1, to - 1);
            var y = 8;
            for (var i = 0; i < lines.Count; i++)
            {
                var measureNumber = from + i;
                var label = new Label
                {
                    Text = "第 " + measureNumber + " 小节",
                    Location = new Point(8, y + 4),
                    AutoSize = true
                };
                var textBox = new TextBox
                {
                    Location = new Point(108, y),
                    Width = 320,
                    Text = lines[i] ?? string.Empty
                };
                _lineBoxes.Add(textBox);
                _linesPanel.Controls.Add(label);
                _linesPanel.Controls.Add(textBox);
                y += 34;
            }

            _linesPanel.ResumeLayout();
        }
    }
}
