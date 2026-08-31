using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.R22_24.Filters
{
    public class ConduitSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is Conduit;

        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
