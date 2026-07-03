using System.Drawing;
using System.Windows.Forms;

namespace JianpuEditor
{
    public sealed partial class MainForm
    {
        private void InitializeComponent()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1280, 820);
            MinimumSize = new Size(960, 640);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "简谱编辑器";
            Font = new Font("Microsoft YaHei", 9f);
            KeyPreview = true;
            ResumeLayout(false);
        }
    }
}