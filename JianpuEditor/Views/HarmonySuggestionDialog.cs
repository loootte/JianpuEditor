using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using JianpuEditor.Models;

namespace JianpuEditor.Views
{
    public sealed class HarmonySuggestionDialog : Form
    {
        private readonly IReadOnlyList<HarmonySuggestion> _suggestions;
        private readonly ListBox _suggestionList;

        public HarmonySuggestionDialog(
            int measureNumber,
            double beatPosition,
            string keySignature,
            IReadOnlyList<HarmonySuggestion> suggestions)
        {
            _suggestions = suggestions ?? Array.Empty<HarmonySuggestion>();

            Text = "和弦建议";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(420, 300);

            var contextLabel = new Label
            {
                Location = new Point(16, 16),
                Size = new Size(388, 36),
                Text = "第 " + measureNumber + " 小节，拍位 " + (beatPosition + 1) + "，调号 " + (keySignature ?? "1=C")
            };

            var hintLabel = new Label
            {
                Location = new Point(16, 52),
                Size = new Size(388, 20),
                ForeColor = Color.DimGray,
                Text = "根据当前调号与旋律音高，推荐 1-3 个和弦（纯本地规则）。"
            };

            _suggestionList = new ListBox
            {
                Location = new Point(16, 78),
                Size = new Size(388, 160),
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
                _suggestionList.Items.Add("(" + suggestion.RomanNumeral + ") " + suggestion.ChordSymbol + " — " + suggestion.Reason);
            }

            if (_suggestionList.Items.Count > 0)
            {
                _suggestionList.SelectedIndex = 0;
            }

            var applyButton = new Button
            {
                Text = "应用",
                DialogResult = DialogResult.OK,
                Location = new Point(232, 252),
                Width = 80
            };
            var cancelButton = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(324, 252),
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

        public HarmonySuggestion SelectedSuggestion
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
