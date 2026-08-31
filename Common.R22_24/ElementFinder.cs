using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Common.R22_24.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.R22_24
{
    public class ElementFinder
    {
        private readonly Document _doc;
        private readonly UIDocument _uiDoc;

        public ElementFinder(Document doc) => _doc = doc ?? throw new ArgumentNullException(nameof(doc));
        public ElementFinder(UIDocument uidoc)
        {
            _uiDoc = uidoc ?? throw new ArgumentNullException(nameof(uidoc));
            _doc = uidoc.Document;
        }

        public FamilySymbol GetSupportSymbol(string familyName)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_ElectricalFixtures)
                .Cast<FamilySymbol>()
                .FirstOrDefault(x => x.Family.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase)
                                  && x.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase));
        }

        public Level GetActiveLevel(string levelName)
        {
            View activeView = _doc.ActiveView;
            if (activeView.GenLevel != null)
                return activeView.GenLevel;

            return new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase))
                ?? new FilteredElementCollector(_doc).OfClass(typeof(Level)).FirstElement() as Level;
        }

        public Element GetConduitById(int elementId)
        {
            ElementId id = new ElementId(elementId);
            Element element = _doc.GetElement(id);
            return (element is Conduit) ? element : null;
        }

        public List<Conduit> GetSelectedConduits()
        {
            List<Conduit> selectedConduits = new List<Conduit>();

            ICollection<ElementId> currentSelection = _uiDoc.Selection.GetElementIds();

            if (currentSelection.Count > 0)
            {
                selectedConduits = currentSelection
                    .Select(id => _doc.GetElement(id))
                    .OfType<Conduit>()
                    .ToList();

                if (selectedConduits.Count > 0)
                    return selectedConduits;
            }

            try
            {
                IList<Reference> references = _uiDoc.Selection.PickObjects(
                    ObjectType.Element,
                    new ConduitSelectionFilter(),
                    "Select Conduits (press Finish when done)"
                );

                selectedConduits = references
                    .Select(r => _doc.GetElement(r.ElementId))
                    .OfType<Conduit>()
                    .GroupBy(c => c.Id)
                    .Select(g => g.First())
                    .ToList();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return new List<Conduit>();
            }

            return selectedConduits;
        }

        public Level GetActiveLevel(object dEFAULT_LEVEL_NAME)
        {
            throw new NotImplementedException();
        }
    }
}
