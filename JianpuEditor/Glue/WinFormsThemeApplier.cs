using System.Drawing;
using System.Windows.Forms;
using JianpuEditor.Controls;
using JianpuEditor.Rendering;

namespace JianpuEditor.Glue
{
    internal static class WinFormsThemeApplier
    {
        public static void Apply(Form form, ScoreCanvas canvas)
        {
            if (form == null)
            {
                return;
            }

            form.BackColor = AppTheme.FormBackground;
            form.ForeColor = AppTheme.FormForeground;
            ApplyControlTree(form.Controls, canvas);
            form.Invalidate(true);
        }

        private static void ApplyControlTree(Control.ControlCollection controls, ScoreCanvas canvas)
        {
            foreach (Control control in controls)
            {
                ApplyControl(control, canvas);
                if (control.HasChildren)
                {
                    ApplyControlTree(control.Controls, canvas);
                }
            }
        }

        private static void ApplyControl(Control control, ScoreCanvas canvas)
        {
            if (control is ScoreCanvas scoreCanvas)
            {
                scoreCanvas.ApplyTheme();
                return;
            }

            if (control is MenuStrip || control is ToolStrip)
            {
                control.BackColor = AppTheme.FormBackground;
                control.ForeColor = AppTheme.FormForeground;
                return;
            }

            if (control is TextBox || control is NumericUpDown)
            {
                control.BackColor = AppTheme.InputBackground;
                control.ForeColor = AppTheme.InputForeground;
                return;
            }

            if (control is Label label)
            {
                label.ForeColor = AppTheme.FormForeground;
                label.BackColor = Color.Transparent;
                return;
            }

            if (control is Button button)
            {
                button.BackColor = AppTheme.IsDarkMode ? Color.FromArgb(58, 58, 64) : SystemColors.Control;
                button.ForeColor = AppTheme.FormForeground;
                button.FlatStyle = FlatStyle.Standard;
                return;
            }

            if (control is Panel panel && panel.Width <= 4)
            {
                panel.BackColor = AppTheme.Separator;
            }
        }
    }
}
