using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.R22_24.Models
{
    public class PackContext
    {
        public Transform ToLocal { get; }
        public Transform ToWorld { get; }
        public XYZ Dir { get; }
        public XYZ Origin { get; }

        public PackContext(Curve baseCurve)
        {
            Origin = baseCurve.GetEndPoint(0);
            Dir = (baseCurve.GetEndPoint(1) - Origin).Normalize();
            XYZ perp = new XYZ(-Dir.Y, Dir.X, 0).Normalize();

            ToWorld = Transform.Identity;
            ToWorld.Origin = Origin;
            ToWorld.BasisX = Dir;
            ToWorld.BasisY = perp;
            ToWorld.BasisZ = XYZ.BasisZ;

            ToLocal = ToWorld.Inverse;
        }
    }
}