using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using Common.R22_24;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

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

                FamilySymbol symbol = finder.GetSupportSymbol(SUPPORT_FAMILY_NAME);
                Level level = finder.GetActiveLevel(DEFAULT_LEVEL_NAME);

                var settingsControl = new SupportSettingsControl(doc);

                var settingsWindow = new Window
                {
                    Title = "Support Settings",
                    Content = settingsControl,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };

                bool? dialogResult = settingsWindow.ShowDialog();

                if (dialogResult != true)
                    return Result.Cancelled;

                double stepInFeet = settingsControl.StepInFeet;
                double minOffsetFeet = settingsControl.MinOffset;
                double maxOffsetFeet = settingsControl.MaxOffset;

                SupportCreator placementService = new SupportCreator(doc);

                using (Transaction trans = new Transaction(doc, "Create Supports Bulk"))
                {
                    trans.Start();

                    List<Element> elementList = conduits.Cast<Element>().ToList();

                    try
                    {
                        placementService.CreateSupportsAlongSegmentedPath(symbol, elementList, level, stepInFeet, minOffsetFeet, maxOffsetFeet);
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        return Result.Cancelled;
                    }
                    catch (ArgumentException ex)
                    {
                        TaskDialog.Show("Support Placement Error", ex.Message);
                        return Result.Failed;
                    }
                    catch (InvalidOperationException ex)
                    {
                        TaskDialog.Show("Support Placement Error", ex.Message);
                        return Result.Failed;
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Critical Error", $"{ex.Message}");
                        return Result.Failed;
                    }

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
