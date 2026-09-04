using Autodesk.Revit.DB;

namespace Common.R22_24.Models
{
    public class PackSegment
    {
        public PackSegment(double length, double width, double centerY, int elementCount, XYZ position = null)
        {
            Length = length;
            Width = width;
            CenterY = centerY;
            ElementCount = elementCount;
            Position = position;
        }

        public double Length { get; }
        public double Width { get; }
        public double CenterY { get; }
        public int ElementCount { get; }
        public XYZ Position { get; }
    }
}