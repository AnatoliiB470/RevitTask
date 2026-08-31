using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Pop_Up.R25_26
{
    [Transaction(TransactionMode.Manual)]
    public class Messanger : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            TaskDialog.Show("Start Message", "Welcome to Revit");
            return Result.Succeeded;
        }
    }
}
