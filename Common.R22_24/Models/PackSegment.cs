namespace Common.R22_24.Models
{
    public class PackSegment
    {
        public PackSegment(double length, double width, double centerY)
        {
            Length = length;
            Width = width;
            CenterY = centerY;
        }

        public double Length { get; }
        public double Width { get; }
        public double CenterY { get; }
    }
}