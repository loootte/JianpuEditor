using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using JianpuEditor.Models;

namespace JianpuEditor.Views
{
    public sealed class HarmonyProgressionSuggestionDialog : Form
    {
        private readonly IReadOnlyList<HarmonyProgressionSuggestion> _suggestions;
        private readonly ListBox _suggestionList;

        public HarmonyProgressionSuggestionDialog(
            int fromMeasureNumber,
            int toMeasureNumber,
            string keySignature,
            IReadOnlyList<HarmonyProgressionSuggestion> suggestions)
        {
            _suggestions = suggestions ?? Array.Empty<HarmonyProgressionSuggestion>();

            Text = "和弦进行建议";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(520, 340);

            var contextLabel = new Label
            {
                Location = new Point(16, 16),
                Size = new Size(488, 36),
                Text = "第 " + fromMeasureNumber + "–" + toMeasureNumber + " 小节，调号 " + (keySignature ?? "1=C")
            };

            var hintLabel = new Label
            {
                Location = new Point(16, 52),
                Size = new Size(488, 32),
                ForeColor = Color.DimGray,
                Text = "综合旋律低音与和声走向，推荐 1–3 组连续和弦进行（纯本地规则）。"
            };

            _suggestionList = new ListBox
            {
                Location = new Point(16, 88),
                Size = new Size(488, 196),
                IntegralHeight = false
            };
            _suggestionList.DoubleClick += (s, e) =>
            {
                if (_suggestionList.SelectedIndex >= 0)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };

            foreach (var suggestion in _suggestions)
            {
                var symbols = string.Join(" - ", suggestion.Steps.Select(step => step.ChordSymbol));
                _suggestionList.Items.Add(
                    suggestion.Label + "  |  " + symbols + "  |  " + suggestion.BassLineSummary + "  |  " + suggestion.Reason);
            }

            if (_suggestionList.Items.Count > 0)
            {
                _suggestionList.SelectedIndex = 0;
            }

            var applyButton = new Button
            {
                Text = "应用到各小节",
                DialogResult = DialogResult.OK,
                Location = new Point(300, 296),
                Width = 110
            };
            var cancelButton = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(424, 296),
                Width = 80
            };

            Controls.Add(contextLabel);
            Controls.Add(hintLabel);
            Controls.Add(_suggestionList);
            Controls.Add(applyButton);
            Controls.Add(cancelButton);
            AcceptButton = applyButton;
            CancelButton = cancelButton;
        }

        public HarmonyProgressionSuggestion SelectedSuggestion
        {
            get
            {
                if (_suggestionList.SelectedIndex < 0 || _suggestionList.SelectedIndex >= _suggestions.Count)
                {
                    return null;
                }

                return _suggestions[_suggestionList.SelectedIndex];
            }
        }
    }
}
