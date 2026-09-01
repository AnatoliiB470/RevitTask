using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Common.R22_24.Validators
{
    public static class ConduitValidator
    {
        public static bool Validate(List<Element> elements, List<Curve> curves)
        {
            if (elements == null || !elements.Any()) return false;

            foreach (var element in elements)
            {
                if (element.Category?.Id.IntegerValue != (int)BuiltInCategory.OST_Conduit)
                {
                    TaskDialog.Show("Support Creation", "Selected elements must be conduits.");
                    return false;
                }
            }

            if (curves == null || !curves.Any())
            {
                TaskDialog.Show("Support Creation", "No valid curves found for the selected elements.");
                return false;
            }

            XYZ baseDir = (curves[0].GetEndPoint(1) - curves[0].GetEndPoint(0)).Normalize();

            foreach (var curve in curves)
            {
                XYZ p0 = curve.GetEndPoint(0);
                XYZ p1 = curve.GetEndPoint(1);
                XYZ dir = (p1 - p0).Normalize();

                if (Math.Abs(baseDir.DotProduct(dir)) < 0.9999)
                {
                    TaskDialog.Show("Support Creation", "Selected conduits are not parallel.");
                    return false;
                }

                if (Math.Abs(p0.Z - p1.Z) > 0.001)
                {
                    TaskDialog.Show("Support Creation", "Selected conduits cannot be sloped.");
                    return false;
                }
            }

            return true;
        }
    }
}