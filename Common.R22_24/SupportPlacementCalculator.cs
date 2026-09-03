using Autodesk.Revit.DB;
using Common.R22_24.Models;
using System;
using System.Collections.Generic;
using System.Data.Sql;
using System.Linq;

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

            var workZone = ComputeWorkZoneBounds(curves);
            perpWidth = workZone.PerpWidth;
            alongLength = workZone.AlongLength;

            if (!workZone.IsValid) return null;

            XYZ localStart = new XYZ(workZone.MaxStart, workZone.PerpCenter, 0);
            XYZ worldStart = workZone.ToWorld.OfPoint(localStart);

            return new XYZ(worldStart.X, worldStart.Y, workZone.MinZ - conduitRadius);
        }

        public static XYZ GetWorkZoneCenter(List<Curve> curves, out double perpWidth, out double alongLength)
        {
            var workZone = ComputeWorkZoneBounds(curves);
            perpWidth = workZone.PerpWidth;
            alongLength = workZone.AlongLength;

            if (!workZone.IsValid) return null;

            XYZ localCenter = new XYZ(workZone.AlongCenter, workZone.PerpCenter, 0);
            XYZ worldCenter = workZone.ToWorld.OfPoint(localCenter);

            return new XYZ(worldCenter.X, worldCenter.Y, workZone.MinZ);
        }

        public static XYZ GetPathDirection(Element element)
        {
            if ((element.Location as LocationCurve)?.Curve is Curve curve)
                return (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();

            return XYZ.BasisX;
        }

        public static List<PackSegment> BuildPackSegments(List<Curve> curves, double conduitRadius, double tolerance, out XYZ zoneStartPoint, out XYZ dir)
        {
            if (curves == null || curves.Count == 0)
            {
                zoneStartPoint = null;
                dir = XYZ.BasisX;
                return new List<PackSegment>();
            }

            var packContext = new PackContext(curves[0]);
            var localCurves = curves.Select(c => new LocalCurve(c, packContext)).ToList();
            dir = packContext.Dir;

            var rawPoints = localCurves.SelectMany(c => new[] { c.StartX, c.EndX })
                                 .OrderBy(x => x)
                                 .ToList();

            var breakpoints = new List<double>();

            foreach (double x in rawPoints)
            {
                if (breakpoints.Count == 0 || x - breakpoints[breakpoints.Count - 1] > tolerance)
                    breakpoints.Add(x);
            }

            double packStart = breakpoints[0];
            double minZ = localCurves.Min(c => c.MinZ);

            XYZ startWorld = packContext.ToWorld.OfPoint(new XYZ(packStart, 0, 0));
            zoneStartPoint = new XYZ(startWorld.X, startWorld.Y, minZ - conduitRadius);

            var segments = new List<PackSegment>();

            for (int i = 0; i < breakpoints.Count - 1; i++)
            {
                double segStart = breakpoints[i];
                double segEnd = breakpoints[i + 1];

                double segMid = (segStart + segEnd) / 2.0;

                var active = localCurves.Where(c => c.StartX <= segMid && c.EndX >= segMid).ToList();

                if (active.Count == 0) break;

                double width = active.Max(c => c.MaxY) - active.Min(c => c.MinY);
                double centerY = (active.Max(c => c.MaxY) + active.Min(c => c.MinY)) / 2.0;
                double segmentLength = segEnd - segStart;

                segments.Add(new PackSegment(segmentLength, width, centerY, active.Count));
            }

            return segments;
        }

        public static List<double> CalculateSymmetricPlacementDistances(double totalLength, double stepInFeet,
            double minEdgeOffsetInFeet,
            double maxEdgeOffsetInFeet)
        {
            var distances = new List<double>();

            double halfLength = totalLength / 2.0;

            if (halfLength >= minEdgeOffsetInFeet && halfLength <= maxEdgeOffsetInFeet)
            {
                distances.Add(halfLength);
                return distances;
            }

            if (halfLength < minEdgeOffsetInFeet)
                return distances;

            double availableSpan = totalLength - (2 * minEdgeOffsetInFeet);
            int stepCount = (int)Math.Floor(availableSpan / stepInFeet);

            if (stepCount == 0)
            {
                distances.Add(minEdgeOffsetInFeet);
                distances.Add(totalLength - minEdgeOffsetInFeet);
                return distances;
            }

            double actualEdgeOffset = (totalLength - (stepCount * stepInFeet)) / 2.0;

            for (int i = 0; i <= stepCount; i++)
                distances.Add(actualEdgeOffset + (i * stepInFeet));

            return distances;
        }

        private static WorkZoneBounds ComputeWorkZoneBounds(List<Curve> curves)
        {
            var packContext = new PackContext(curves[0]);
            var localCurves = curves.Select(c => new LocalCurve(c, packContext)).ToList();

            double maxStart = localCurves.Max(c => c.StartX);
            double minEnd = localCurves.Min(c => c.EndX);
            double minPerp = localCurves.Min(c => c.MinY);
            double maxPerp = localCurves.Max(c => c.MaxY);
            double minZ = localCurves.Min(c => c.MinZ);

            return new WorkZoneBounds(packContext.ToWorld, maxStart, minEnd, minPerp, maxPerp, minZ);
        }
    }
}