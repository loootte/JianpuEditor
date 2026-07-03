using System;
using System.Drawing;
using System.Windows.Forms;
using JianpuEditor.Glue;
using JianpuEditor.Rendering;

namespace JianpuEditor.Views
{
    public sealed class WinFormsLayoutService : ILayoutService
    {
        private MainFormLayoutContext _context;
        private Size _designDpi = new Size(96, 96);

        public void Attach(MainFormLayoutContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            ValidateContext(_context);
        }

        public void RestoreLayout()
        {
            EnsureAttached();
            var form = _context.Form;
            var width = Math.Max(form.ClientSize.Width, 400);

            UpdateChromeHeights(width);
            ResetCanvasViewport();
            EnforceZOrder();
            form.PerformLayout();
        }

        public void ApplyTheme()
        {
            EnsureAttached();
            var form = _context.Form;
            var menu = _context.MenuStrip;
            if (menu != null)
            {
                menu.Visible = true;
                if (form.MainMenuStrip != menu)
                {
                    form.MainMenuStrip = menu;
                }
            }

            WinFormsThemeApplier.Apply(form, _context.ScoreCanvas);
        }

        public void ApplyDpiScaling()
        {
            EnsureAttached();
            var form = _context.Form;
            if (form.AutoScaleMode == AutoScaleMode.Dpi || form.AutoScaleMode == AutoScaleMode.Font)
            {
                return;
            }

            if (!form.IsHandleCreated)
            {
                return;
            }

            using (var graphics = form.CreateGraphics())
            {
                var currentDpi = new SizeF(graphics.DpiX, graphics.DpiY);
                if (Math.Abs(currentDpi.Width - _designDpi.Width) < 0.5f)
                {
                    return;
                }

                var scaleX = currentDpi.Width / _designDpi.Width;
                var scaleY = currentDpi.Height / _designDpi.Height;
                ApplyScale(form, scaleX, scaleY);
            }
        }

        public void EnforceZOrder()
        {
            EnsureAttached();
            var form = _context.Form;
            var mainLayout = _context.MainLayout;
            if (mainLayout == null || form.Controls.Count == 0)
            {
                return;
            }

            if (!ReferenceEquals(form.Controls[form.Controls.Count - 1], mainLayout))
            {
                form.Controls.SetChildIndex(mainLayout, form.Controls.Count - 1);
            }

            mainLayout.BringToFront();
            _context.ScoreCanvas?.BringToFront();
            _context.MenuStrip?.BringToFront();
        }

        private void UpdateChromeHeights(int clientWidth)
        {
            var chrome = _context.ChromeLayout;
            var toolbar = _context.ToolbarPanel;
            if (chrome == null)
            {
                return;
            }

            var menuHeight = Math.Max(_context.MenuStrip?.PreferredSize.Height ?? 0, MainFormLayoutContext.MinimumMenuHeight);
            var toolbarHeight = MeasureToolbarHeight(toolbar, clientWidth);

            chrome.RowStyles[0] = new RowStyle(SizeType.Absolute, menuHeight);
            chrome.RowStyles[1] = new RowStyle(SizeType.Absolute, toolbarHeight);

            var chromeHeight = menuHeight + toolbarHeight + chrome.Padding.Vertical;
            chrome.Height = chromeHeight;
            chrome.MinimumSize = new Size(0, chromeHeight);
        }

        private static int MeasureToolbarHeight(FlowLayoutPanel toolbar, int width)
        {
            if (toolbar == null)
            {
                return MainFormLayoutContext.DefaultToolbarHeight;
            }

            toolbar.MaximumSize = new Size(width, 0);
            toolbar.Width = width;
            toolbar.PerformLayout();
            var height = toolbar.GetPreferredSize(new Size(width, 0)).Height;
            return Math.Max(height + 4, MainFormLayoutContext.MinimumToolbarHeight);
        }

        private void ResetCanvasViewport()
        {
            var canvas = _context.ScoreCanvas;
            if (canvas == null || !canvas.IsHandleCreated)
            {
                return;
            }

            canvas.AutoScrollPosition = new Point(0, 0);
            canvas.RefreshScore();
        }

        private static void ApplyScale(Control control, float scaleX, float scaleY)
        {
            if (control == null)
            {
                return;
            }

            control.Scale(new SizeF(scaleX, scaleY));
            foreach (Control child in control.Controls)
            {
                ApplyScale(child, scaleX, scaleY);
            }
        }

        private static void ValidateContext(MainFormLayoutContext context)
        {
            if (context.Form == null
                || context.MainLayout == null
                || context.ChromeLayout == null
                || context.MenuStrip == null
                || context.ToolbarPanel == null
                || context.ScoreCanvas == null
                || context.StatusLabel == null)
            {
                throw new InvalidOperationException("MainFormLayoutContext is incomplete.");
            }
        }

        private void EnsureAttached()
        {
            if (_context == null)
            {
                throw new InvalidOperationException("LayoutService has not been attached to a MainFormLayoutContext.");
            }
        }

    }
}
