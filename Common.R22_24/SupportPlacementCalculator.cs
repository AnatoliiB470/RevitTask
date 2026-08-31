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
    }
}
