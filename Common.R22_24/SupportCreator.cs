using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.R22_24
{
    public class SupportCreator
    {
        private const double DEFAULT_ROD_OFFSET_IN_FEET = 0.4;

        private readonly Document _doc;
        private readonly ElementFinder _elementFinder;

        public SupportCreator(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
            _elementFinder = new ElementFinder(doc);
        }

        public List<FamilyInstance> CreateSupportsAlongPath(
            FamilySymbol supportSymbol,
            List<Element> elements,
            Level level,
            double stepInFeet = 1.0,
            double rodOffsetInFeet = DEFAULT_ROD_OFFSET_IN_FEET)
        {
            if (elements == null || !elements.Any()) return new List<FamilyInstance>();

            List<Curve> curves = _elementFinder.GetCurves(elements);
            if (!curves.Any()) throw new ArgumentException("No valid curves found.");

            XYZ centerPoint = SupportPlacementCalculator.GetWorkZoneCenter(curves, out double perpWidth, out double alongLength);
            if (centerPoint == null) return new List<FamilyInstance>();

            Line baseLine = curves[0] as Line
                ?? throw new ArgumentException("Reference element must be a line.");
            XYZ dir = (baseLine.GetEndPoint(1) - baseLine.GetEndPoint(0)).Normalize();

            double halfLength = alongLength * 0.5;
            var supports = new List<FamilyInstance>();

            for (double d = 0; d <= halfLength; d += stepInFeet)
            {
                CreateAtOffset(supportSymbol, elements, perpWidth, level, rodOffsetInFeet, centerPoint, dir, d, supports);

                if (d > 0)
                    CreateAtOffset(supportSymbol, elements, perpWidth, level, rodOffsetInFeet, centerPoint, dir, -d, supports);
            }

            return supports;
        }

        public FamilyInstance CreateSupport(
            FamilySymbol supportSymbol,
            List<Element> elements,
            Level level,
            double rodOffsetInFeet = DEFAULT_ROD_OFFSET_IN_FEET,
            XYZ customPoint = null)
        {
            if (elements == null || !elements.Any())
                throw new ArgumentException("Elements list cannot be null or empty.", nameof(elements));

            List<Curve> curves = _elementFinder.GetCurves(elements);

            if (!curves.Any()) throw new ArgumentException("No valid curves found.");

            XYZ centerPoint = SupportPlacementCalculator.GetWorkZoneCenter(curves, out double perpWidth, out _);

            if (centerPoint == null)
                throw new InvalidOperationException("Elements do not overlap along their common path.");

            XYZ placementPoint = customPoint ?? centerPoint;

            return CreateSupportAt(supportSymbol, elements, perpWidth, level, rodOffsetInFeet, placementPoint);
        }

        private void CreateAtOffset(
            FamilySymbol supportSymbol, List<Element> elements, double perpWidth, Level level,
            double rodOffsetInFeet, XYZ centerPoint, XYZ dir, double d, List<FamilyInstance> supports)
        {
            XYZ pointAtStep = centerPoint + (dir * d);
            supports.Add(CreateSupportAt(supportSymbol, elements, perpWidth, level, rodOffsetInFeet, pointAtStep));
        }

        private FamilyInstance CreateSupportAt(
            FamilySymbol supportSymbol, List<Element> elements, double perpWidth, Level level,
            double rodOffsetInFeet, XYZ placementPoint)
        {
            ValidatePlacementInputs(supportSymbol, level);
            EnsureSymbolIsActive(supportSymbol);

            Element hostElement = elements.First();

            FamilyInstance supportInstance = _doc.Create.NewFamilyInstance(
                placementPoint, supportSymbol, null, level, StructuralType.NonStructural);

            RotatePerpendicular(supportInstance, placementPoint, hostElement);
            SetWidth(supportInstance, perpWidth, rodOffsetInFeet);

            return supportInstance;
        }

        private void SetWidth(FamilyInstance support, double perpWidth, double rodOffsetInFeet)
        {
            support.LookupParameter("LENGTH")?.Set(perpWidth + (2 * rodOffsetInFeet));
        }

        private void RotatePerpendicular(FamilyInstance instance, XYZ point, Element hostElement)
        {
            XYZ dir = SupportPlacementCalculator.GetPathDirection(hostElement);

            if (!dir.IsAlmostEqualTo(XYZ.BasisZ) && !dir.IsAlmostEqualTo(-XYZ.BasisZ))
            {
                double angle = Math.Atan2(dir.Y, dir.X) + (Math.PI / 2.0);
                Line axis = Line.CreateBound(point, point + XYZ.BasisZ);
                ElementTransformUtils.RotateElement(_doc, instance.Id, axis, angle);
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