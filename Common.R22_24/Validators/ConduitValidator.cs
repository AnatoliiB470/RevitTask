using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace Common.R22_24.Validators
{
    public static class ConduitValidator
    {
        private const double PARALLEL_TOLERANCE = 0.9999;
        private const double SLOPE_TOLERANCE = 0.001;

        public static void Validate(List<Element> elements, List<Curve> curves)
        {
            if (elements == null || !elements.Any())
                throw new ArgumentException("No elements were provided for support placement.", nameof(elements));

            foreach (var element in elements)
            {
                if (element?.Category?.Id.IntegerValue != (int)BuiltInCategory.OST_Conduit)
                    throw new InvalidOperationException("Selected elements must be conduits.");
            }

            if (curves == null || !curves.Any())
                throw new InvalidOperationException("No valid curves found for the selected elements.");

            XYZ baseDir = (curves[0].GetEndPoint(1) - curves[0].GetEndPoint(0)).Normalize();

            foreach (var curve in curves)
            {
                XYZ p0 = curve.GetEndPoint(0);
                XYZ p1 = curve.GetEndPoint(1);
                XYZ dir = (p1 - p0).Normalize();

                if (Math.Abs(baseDir.DotProduct(dir)) < PARALLEL_TOLERANCE)
                    throw new InvalidOperationException("Selected conduits are not parallel.");

                if (Math.Abs(p0.Z - p1.Z) > SLOPE_TOLERANCE)
                    throw new InvalidOperationException("Selected conduits cannot be sloped.");
            }
        }
    }
}