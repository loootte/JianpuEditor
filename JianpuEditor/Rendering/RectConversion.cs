using System.Drawing;
using JianpuEditor.Models;

namespace JianpuEditor.Rendering
{
    public static class RectConversion
    {
        public static Rectangle ToRectangle(this IntRect rect)
        {
            return rect.IsEmpty ? Rectangle.Empty : new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static IntRect ToIntRect(this Rectangle rect)
        {
            return rect.IsEmpty ? IntRect.Empty : new IntRect(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }
}
