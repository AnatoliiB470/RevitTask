using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static XYZ GetCommonPlacementPoint(List<Curve> curves, double offsetInFeet = 1.0)
        {
            if (curves == null || curves.Count == 0) return null;

            Curve baseCurve = curves[0];
            XYZ startPoint = baseCurve.GetEndPoint(0);

            double maxStart = 0.0;
            double minEnd = baseCurve.Length;

            XYZ dir = (baseCurve.GetEndPoint(1) - startPoint).Normalize();

            foreach (Curve curve in curves)
            {
                double p0 = (curve.GetEndPoint(0) - startPoint).DotProduct(dir);
                double p1 = (curve.GetEndPoint(1) - startPoint).DotProduct(dir);

                double start = Math.Min(p0, p1);
                double end = Math.Max(p0, p1);

                if (start > maxStart) maxStart = start;
                if (end < minEnd) minEnd = end;
            }

            if (maxStart >= minEnd) return null;

            double targetDistance = maxStart + offsetInFeet;

            if (targetDistance > minEnd)
                targetDistance = (maxStart + minEnd) / 2.0;

            return baseCurve.Evaluate(targetDistance, false);
        }

        public static BoundingBoxXYZ GetPackBoundingBox(List<Element> elements)
        {
            var boxes = elements.Select(e => e.get_BoundingBox(null)).Where(b => b != null).ToList();

            return new BoundingBoxXYZ
            {
                Min = new XYZ(boxes.Min(b => b.Min.X), boxes.Min(b => b.Min.Y), boxes.Min(b => b.Min.Z)),
                Max = new XYZ(boxes.Max(b => b.Max.X), boxes.Max(b => b.Max.Y), boxes.Max(b => b.Max.Z))
            };
        }

        public static XYZ GetDefaultPlacementPoint(BoundingBoxXYZ packBox) => new XYZ(
            (packBox.Min.X + packBox.Max.X) * 0.5,
            (packBox.Min.Y + packBox.Max.Y) * 0.5,
            packBox.Min.Z);

        public static XYZ GetCenterPoint(Line line, BoundingBoxXYZ packBox, XYZ dir)
        {
            XYZ lineMid = (line.GetEndPoint(0) + line.GetEndPoint(1)) * 0.5;

            return Math.Abs(dir.X) > 0.5
                ? new XYZ(lineMid.X, (packBox.Min.Y + packBox.Max.Y) * 0.5, packBox.Min.Z)
                : new XYZ((packBox.Min.X + packBox.Max.X) * 0.5, lineMid.Y, packBox.Min.Z);
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