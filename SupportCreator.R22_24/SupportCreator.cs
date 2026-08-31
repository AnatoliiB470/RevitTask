using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;

namespace SupportCreator
{
    internal class SupportCreator
    {
        private readonly Document _doc;

        public SupportCreator(Document doc) => _doc = doc ?? throw new ArgumentNullException(nameof(doc));

        public FamilyInstance CreateSupport(FamilySymbol supportSymbol, XYZ locationFeet, Element hostElement, Level level)
        {
            ValidatePlacementInputs(supportSymbol, level);

            using (Transaction trans = new Transaction(_doc, "Create Support"))
            {
                trans.Start();

                EnsureSymbolIsActive(supportSymbol);

                FamilyInstance supportInstance = _doc.Create.NewFamilyInstance(
                    locationFeet,
                    supportSymbol,
                    hostElement,
                    level,
                    StructuralType.NonStructural
                );

                trans.Commit();
                return supportInstance;
            }
        }

        private void EnsureSymbolIsActive(FamilySymbol symbol)
        {
            if (!symbol.IsActive)
            {
                symbol.Activate();
                _doc.Regenerate();
            }
        }

        private void ValidatePlacementInputs(FamilySymbol supportSymbol, Level level)
        {
            if (supportSymbol == null)
                throw new ArgumentNullException(nameof(supportSymbol), "FamilySymbol is null.");

            if (level == null)
                throw new ArgumentNullException(nameof(level), "Level is null.");
        }
    }
}