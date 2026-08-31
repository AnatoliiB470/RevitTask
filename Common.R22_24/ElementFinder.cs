using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
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

        public ElementFinder(Document doc) => _doc = doc ?? throw new ArgumentNullException(nameof(doc));

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
    }
}
