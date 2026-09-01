using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace Common.R22_24
{
    public static class SupportPlacementCalculator
    {
        public static XYZ CalculatePointFromStart(Curve curve, double offsetInFeet = 1.0)
        {
            if (curve == null) throw new ArgumentNullException(nameof(curve));

            double distance = curve.Length <= offsetInFeet ? curve.Length / 2.0 : offsetInFeet;

            return curve.Evaluate(distance, false);
        }

        public static XYZ GetWorkZoneStart(List<Curve> curves, double conduitRadius, out double perpWidth, out double alongLength)
        {
            if (curves == null || curves.Count == 0)
            {
                perpWidth = 0;
                alongLength = 0;
                return null;
            }

            Curve baseCurve = curves[0];
            XYZ startPoint = baseCurve.GetEndPoint(0);
            XYZ dir = (baseCurve.GetEndPoint(1) - startPoint).Normalize();
            XYZ perp = new XYZ(-dir.Y, dir.X, 0);

            Transform toWorld = Transform.Identity;
            toWorld.Origin = startPoint;
            toWorld.BasisX = dir;
            toWorld.BasisY = perp;
            toWorld.BasisZ = XYZ.BasisZ;
            Transform toLocal = toWorld.Inverse;

            double maxStart = double.MinValue;
            double minEnd = double.MaxValue;
            double minPerp = double.MaxValue;
            double maxPerp = double.MinValue;
            double minZ = double.MaxValue;

            foreach (Curve curve in curves)
            {
                XYZ l0 = toLocal.OfPoint(curve.GetEndPoint(0));
                XYZ l1 = toLocal.OfPoint(curve.GetEndPoint(1));

                maxStart = Math.Max(maxStart, Math.Min(l0.X, l1.X));
                minEnd = Math.Min(minEnd, Math.Max(l0.X, l1.X));
                minPerp = Math.Min(minPerp, Math.Min(l0.Y, l1.Y));
                maxPerp = Math.Max(maxPerp, Math.Max(l0.Y, l1.Y));
                minZ = Math.Min(minZ, Math.Min(curve.GetEndPoint(0).Z, curve.GetEndPoint(1).Z));
            }

            perpWidth = maxPerp - minPerp;
            alongLength = minEnd - maxStart;

            if (maxStart >= minEnd) return null;

            XYZ localStart = new XYZ(maxStart, (minPerp + maxPerp) * 0.5, 0);
            XYZ worldStart = toWorld.OfPoint(localStart);
            double bottomZ = minZ - conduitRadius;

            return new XYZ(worldStart.X, worldStart.Y, bottomZ);
        }

        public static XYZ GetPathDirection(Element element)
        {
            if ((element.Location as LocationCurve)?.Curve is Curve curve)
                return (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();

            return XYZ.BasisX;
        }

        public static XYZ GetWorkZoneCenter(List<Curve> curves, out double perpWidth, out double alongLength)
        {
            Curve baseCurve = curves[0];
            XYZ origin = baseCurve.GetEndPoint(0);
            XYZ dir = (baseCurve.GetEndPoint(1) - origin).Normalize();
            XYZ perp = new XYZ(-dir.Y, dir.X, 0);

            Transform toWorld = Transform.Identity;
            toWorld.Origin = origin;
            toWorld.BasisX = dir;
            toWorld.BasisY = perp;
            toWorld.BasisZ = XYZ.BasisZ;
            Transform toLocal = toWorld.Inverse;

            double maxStart = double.MinValue, minEnd = double.MaxValue;
            double minPerp = double.MaxValue, maxPerp = double.MinValue;
            double minZ = double.MaxValue;

            foreach (Curve curve in curves)
            {
                XYZ l0 = toLocal.OfPoint(curve.GetEndPoint(0));
                XYZ l1 = toLocal.OfPoint(curve.GetEndPoint(1));

                maxStart = Math.Max(maxStart, Math.Min(l0.X, l1.X));
                minEnd = Math.Min(minEnd, Math.Max(l0.X, l1.X));
                minPerp = Math.Min(minPerp, Math.Min(l0.Y, l1.Y));
                maxPerp = Math.Max(maxPerp, Math.Max(l0.Y, l1.Y));
                minZ = Math.Min(minZ, Math.Min(curve.GetEndPoint(0).Z, curve.GetEndPoint(1).Z));
            }

            perpWidth = maxPerp - minPerp;
            alongLength = minEnd - maxStart;

            if (maxStart >= minEnd) return null;

            XYZ localCenter = new XYZ((maxStart + minEnd) * 0.5, (minPerp + maxPerp) * 0.5, 0);
            XYZ worldCenter = toWorld.OfPoint(localCenter);

            return new XYZ(worldCenter.X, worldCenter.Y, minZ);
        }
    }
}