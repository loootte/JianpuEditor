using System;
using JianpuEditor.Models;

namespace JianpuEditor.Controls
{
    public sealed class ScoreHeaderEditedEventArgs : EventArgs
    {
        public ScoreHeaderField Field { get; set; }

        public string Text { get; set; }
    }
}