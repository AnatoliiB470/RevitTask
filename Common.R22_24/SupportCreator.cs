using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Common.R22_24.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.R22_24
{
    public class SupportCreator
    {
        private const double DEFAULT_ROD_OFFSET_IN_FEET = 0.4;
        private const double MIN_WORK_ZONE_LENGTH_IN_FEET = 2.0;

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
            double stepInFeet,
            double minEdgeOffsetInFeet,
            double maxEdgeOffsetInFeet,
            double rodOffsetInFeet = DEFAULT_ROD_OFFSET_IN_FEET)
        {
            if (elements == null || !elements.Any()) return new List<FamilyInstance>();

            List<Curve> curves = _elementFinder.GetCurves(elements);

            if (!curves.Any()) throw new ArgumentException("No valid curves found.");

            if (!ConduitValidator.Validate(elements, curves))
                return new List<FamilyInstance>();

            XYZ zoneStartPoint = SupportPlacementCalculator.GetWorkZoneStart(curves, GetFirstConduitRadius(elements), out double perpWidth, out double alongLength);

            if (zoneStartPoint == null) return new List<FamilyInstance>();

            if (alongLength < MIN_WORK_ZONE_LENGTH_IN_FEET)
            {
                TaskDialog.Show("Support Creation", "The selected conduit path is too short. The work zone length must be at least 2 feet.");
                return new List<FamilyInstance>();
            }

            if (alongLength <= (minEdgeOffsetInFeet * 2)) return new List<FamilyInstance>();

            Curve baseCurve = curves[0];
            XYZ dir = (baseCurve.GetEndPoint(1) - baseCurve.GetEndPoint(0)).Normalize();

            var supports = new List<FamilyInstance>();

            double startDist = minEdgeOffsetInFeet;
            double endDist = alongLength - minEdgeOffsetInFeet;

            for (double currentDist = startDist; currentDist <= endDist; currentDist += stepInFeet)
            {
                XYZ placementPoint = zoneStartPoint + (dir * currentDist);
                supports.Add(CreateSupportAt(supportSymbol, elements, perpWidth, level, rodOffsetInFeet, placementPoint));
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

        private FamilyInstance CreateSupportAt(
            FamilySymbol supportSymbol, List<Element> elements, double perpWidth, Level level,
            double rodOffsetInFeet, XYZ placementPoint)
        {
            ValidatePlacementInputs(supportSymbol, level);
            EnsureSymbolIsActive(supportSymbol);

            Element hostElement = elements.First();

            FamilyInstance supportInstance = _doc.Create.NewFamilyInstance(
                placementPoint, supportSymbol, null, level, StructuralType.NonStructural);

            SetZValue(supportInstance, placementPoint.Z);

            if (elements != null && elements.Count > 0)
            {
                Parameter conduitCommentParam = elements[0].get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                if (conduitCommentParam != null && conduitCommentParam.HasValue)
                {
                    string commentValue = conduitCommentParam.AsString();

                    Parameter supportCommentParam = supportInstance.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                    if (supportCommentParam != null && !supportCommentParam.IsReadOnly)
                        supportCommentParam.Set(commentValue);
                }
            }

            RotatePerpendicular(supportInstance, placementPoint, hostElement);
            SetWidth(supportInstance, perpWidth, rodOffsetInFeet);

            return supportInstance;
        }

        private void SetWidth(FamilyInstance support, double perpWidth, double rodOffsetInFeet)
        {
            support.LookupParameter("LENGTH")?.Set(perpWidth + (2 * rodOffsetInFeet));
        }

        private void SetZValue(FamilyInstance support, double value)
        {
            support.LookupParameter("TOS-TIER1")?.Set(value);
        }

        private double GetFirstConduitRadius(List<Element> elements)
        {
            Element firstElement = elements.First();

            Parameter diamParam = firstElement.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM);

            if (diamParam != null && diamParam.HasValue)
                return diamParam.AsDouble() * 0.5; 

            return 0.0;
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