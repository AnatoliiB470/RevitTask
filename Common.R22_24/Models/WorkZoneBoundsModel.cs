using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.R22_24.Models
{
    public struct WorkZoneBounds
    {
        public WorkZoneBounds(Transform toWorld, double maxStart, double minEnd, double minPerp, double maxPerp, double minZ)
        {
            ToWorld = toWorld;
            MaxStart = maxStart;
            MinEnd = minEnd;
            MinPerp = minPerp;
            MaxPerp = maxPerp;
            MinZ = minZ;
        }

        public Transform ToWorld { get; }
        public double MaxStart { get; }
        public double MinEnd { get; }
        public double MinPerp { get; }
        public double MaxPerp { get; }
        public double MinZ { get; }

        public bool IsValid => MaxStart < MinEnd;
        public double PerpWidth => MaxPerp - MinPerp;
        public double AlongLength => MinEnd - MaxStart;
        public double PerpCenter => (MinPerp + MaxPerp) * 0.5;
        public double AlongCenter => (MaxStart + MinEnd) * 0.5;

    }
}
