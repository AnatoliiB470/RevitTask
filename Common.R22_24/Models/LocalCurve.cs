using Autodesk.Revit.DB;
using System;

namespace Common.R22_24.Models
{
    public readonly struct LocalCurve
    {
        public Curve OriginalCurve { get; }
        public double StartX { get; }
        public double EndX { get; }
        public double MinY { get; }
        public double MaxY { get; }
        public double MinZ { get; }

        public LocalCurve(Curve curve, PackContext packContext)
        {
            OriginalCurve = curve;
            XYZ p0 = packContext.ToLocal.OfPoint(curve.GetEndPoint(0));
            XYZ p1 = packContext.ToLocal.OfPoint(curve.GetEndPoint(1));

            StartX = Math.Min(p0.X, p1.X);
            EndX = Math.Max(p0.X, p1.X);
            MinY = Math.Min(p0.Y, p1.Y);
            MaxY = Math.Max(p0.Y, p1.Y);
            MinZ = Math.Min(curve.GetEndPoint(0).Z, curve.GetEndPoint(1).Z);
        }
    }
}
