namespace Common.R22_24.Models
{
    public class PackSegment
    {
        public PackSegment(double length, double width, double centerY, int elementCount)
        {
            Length = length;
            Width = width;
            CenterY = centerY;
            ElementCount = elementCount;
        }

        public double Length { get; }
        public double Width { get; }
        public double CenterY { get; }
        public int ElementCount { get; }
    }
}