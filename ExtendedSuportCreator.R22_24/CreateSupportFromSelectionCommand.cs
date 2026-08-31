using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using Common.R22_24;
using System;
using System.Collections.Generic;

namespace ExtendedSuportCreator.R22_24
{
    [Transaction(TransactionMode.Manual)]
    public class CreateSupportFromSelectionCommand : IExternalCommand
    {
        private const string SUPPORT_FAMILY_NAME = "TRAPEZE 1 TIER";
        private const string DEFAULT_LEVEL_NAME = "Level 1";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            var doc = uiDoc.Document;

            try
            {
                ElementFinder finder = new ElementFinder(uiDoc);
                List<Conduit> conduits = finder.GetSelectedConduits();

                if (conduits.Count == 0)
                    return Result.Cancelled;

                List<Curve> curves = finder.GetCurves(conduits);
                XYZ placementPoint = SupportPlacementCalculator.GetCommonPlacementPoint(curves, offsetInFeet: 1.0);

                if (placementPoint == null)
                {
                    message = "Selected conduits do not have a common overlap range.";
                    return Result.Failed;
                }

                FamilySymbol symbol = finder.GetSupportSymbol(SUPPORT_FAMILY_NAME);
                Level level = finder.GetActiveLevel(DEFAULT_LEVEL_NAME);

                SupportCreator placementService = new SupportCreator(doc);

                using (Transaction trans = new Transaction(doc, "Create Supports Bulk"))
                {
                    trans.Start();

                    placementService.CreateSupport(symbol, placementPoint, conduits[0], level);

                    trans.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
