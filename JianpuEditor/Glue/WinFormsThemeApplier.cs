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
            ApplyMenuStrip(form.MainMenuStrip);
            ApplyControlTree(form.Controls, canvas);
            form.Invalidate(true);
        }

        public static void ApplyMenuStrip(MenuStrip menuStrip)
        {
            if (menuStrip == null)
            {
                return;
            }

            menuStrip.Visible = true;
            menuStrip.GripStyle = ToolStripGripStyle.Hidden;
            menuStrip.ForeColor = AppTheme.IsDarkMode ? AppTheme.FormForeground : SystemColors.ControlText;
            if (AppTheme.IsDarkMode)
            {
                menuStrip.BackColor = AppTheme.FormBackground;
            }
            else
            {
                menuStrip.RenderMode = ToolStripRenderMode.System;
                menuStrip.Renderer = null;
                menuStrip.BackColor = SystemColors.MenuBar;
            }
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

            if (control is MenuStrip menuStrip)
            {
                ApplyMenuStrip(menuStrip);
                return;
            }

            if (control is ToolStrip toolStrip)
            {
                toolStrip.BackColor = AppTheme.FormBackground;
                toolStrip.ForeColor = AppTheme.FormForeground;
                return;
            }

            if (control is TableLayoutPanel || control is FlowLayoutPanel)
            {
                control.BackColor = AppTheme.FormBackground;
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
