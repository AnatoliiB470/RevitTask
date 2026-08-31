using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SupportCreator
{
    [Transaction(TransactionMode.Manual)]
    internal class CreateSupportCommand : IExternalCommand
    {
        private const string SUPPORT_FAMILY_NAME = "TRAPEZE 1 TIER";
        private const string DEFAULT_LEVEL_NAME = "Level 1";
        private const int CONDUIT_ID = 126160;

        private static readonly XYZ TargetPoint = new XYZ(35.0, -113.3, 11.28);

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            try
            {
                ElementFinder finder = new ElementFinder(doc);

                FamilySymbol symbol = finder.GetSupportSymbol(SUPPORT_FAMILY_NAME);
                Element host = finder.GetConduitById(CONDUIT_ID);
                Level level = finder.GetActiveLevel(DEFAULT_LEVEL_NAME);

                SupportCreator placementService = new SupportCreator(doc);
                FamilyInstance createdSupport = placementService.CreateSupport(symbol, TargetPoint, host, level);

                return createdSupport != null ? Result.Succeeded : Result.Failed;
            }
            catch (System.Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
