namespace JianpuEditor.Models
{
    public readonly struct IntRect
    {
        public static IntRect Empty => default;

        public int X { get; }

        public int Y { get; }

        public int Width { get; }

        public int Height { get; }

        public bool IsEmpty => Width <= 0 && Height <= 0;

        public IntRect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
