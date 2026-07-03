using System.Windows.Forms;
using JianpuEditor.Controls;

namespace JianpuEditor.Views
{
    public sealed class MainFormLayoutContext
    {
        public const int StatusRowHeight = 28;
        public const int DefaultToolbarHeight = 160;
        public const int MinimumToolbarHeight = 80;
        public const int MinimumMenuHeight = 24;

        public Form Form { get; set; }

        public TableLayoutPanel MainLayout { get; set; }

        public TableLayoutPanel ChromeLayout { get; set; }

        public MenuStrip MenuStrip { get; set; }

        public FlowLayoutPanel ToolbarPanel { get; set; }

        public ScoreCanvas ScoreCanvas { get; set; }

        public Label StatusLabel { get; set; }
    }
}