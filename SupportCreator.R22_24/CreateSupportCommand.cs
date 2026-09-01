using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using Common.R22_24;
using System.Collections.Generic;

namespace CreateSupportCommand.R22_24
{
    [Transaction(TransactionMode.Manual)]
    internal class CreateSupportCommand : IExternalCommand
    {
        private const string SUPPORT_FAMILY_NAME = "TRAPEZE 1 TIER";
        private const string DEFAULT_LEVEL_NAME = "Level 1";
        private const int CONDUIT_ID = 126160;

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
                FamilyInstance createdSupport = null;

                using (Transaction trans = new Transaction(doc, "Create Supports Bulk"))
                {
                    trans.Start();

                    createdSupport = placementService.CreateSupport(symbol, new List<Element> { host }, level, rodOffsetInFeet: 1.0);

                    trans.Commit();
                }

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
