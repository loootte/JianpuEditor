using System;
using System.Windows.Forms;

namespace JianpuEditor.Glue
{
    internal sealed class UndoRedoMessageFilter : IMessageFilter
    {
        private const int WmKeyDown = 0x0100;
        private const int WmSysKeyDown = 0x0104;

        private readonly Func<bool> _undo;
        private readonly Func<bool> _redo;

        public UndoRedoMessageFilter(Func<bool> undo, Func<bool> redo)
        {
            _undo = undo ?? throw new ArgumentNullException(nameof(undo));
            _redo = redo ?? throw new ArgumentNullException(nameof(redo));
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WmKeyDown && m.Msg != WmSysKeyDown)
            {
                return false;
            }

            var keyCode = (Keys)(int)m.WParam & Keys.KeyCode;
            var modifiers = Control.ModifierKeys;
            var keyData = keyCode | modifiers;

            if (keyData == (Keys.Control | Keys.Z))
            {
                return _undo();
            }

            if (keyData == (Keys.Control | Keys.Y))
            {
                return _redo();
            }

            return false;
        }
    }
}