using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Common.R22_24.Models;
using Common.R22_24.Validators;
using System;
using System.Collections.Generic;
using System.Linq;

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
            if (!PrepareAndValidateCurves(level, elements, out var curves))
                return new List<FamilyInstance>();

            XYZ zoneStartPoint = SupportPlacementCalculator.GetWorkZoneStart(
                curves, GetFirstConduitRadius(elements), out double perpWidth, out double alongLength);

            if (zoneStartPoint == null)
                return new List<FamilyInstance>();

            List<double> placementDistances = GetPlacementDistances(alongLength, stepInFeet, minEdgeOffsetInFeet, maxEdgeOffsetInFeet);

            XYZ dir = SupportPlacementCalculator.GetPathDirection(elements[0]);
            var supports = new List<FamilyInstance>();

            foreach (double dist in placementDistances)
            {
                XYZ placementPoint = zoneStartPoint + (dir * dist);
                supports.Add(CreateSupportAt(supportSymbol, elements, perpWidth, level, rodOffsetInFeet, placementPoint));
            }

            return supports;
        }

        public FamilyInstance CreateSupport(FamilySymbol supportSymbol, List<Element> elements,
            Level level, double rodOffsetInFeet = DEFAULT_ROD_OFFSET_IN_FEET, XYZ customPoint = null)
        {
            if (!PrepareAndValidateCurves(level, elements, out var curves))
                return null;

            XYZ centerPoint = SupportPlacementCalculator.GetWorkZoneCenter(curves, out double perpWidth, out _);

            if (centerPoint == null)
                throw new InvalidOperationException("Elements do not overlap along their common path.");

            XYZ placementPoint = customPoint ?? centerPoint;

            return CreateSupportAt(supportSymbol, elements, perpWidth, level, rodOffsetInFeet, placementPoint);
        }

        public List<FamilyInstance> CreateSupportsAlongSegmentedPath(
    List<Element> elements,
    Level level,
    double stepInFeet,
    double minEdgeOffsetInFeet,
    double maxEdgeOffsetInFeet,
    double rodOffsetInFeet = DEFAULT_ROD_OFFSET_IN_FEET)
        {
            if (!PrepareAndValidateCurves(level, elements, out var curves))
                return new List<FamilyInstance>();

            double conduitRadius = GetFirstConduitRadius(elements);

            List<PackSegment> packSegments = SupportPlacementCalculator.CalculateGlobalSupportSegments(
                curves, conduitRadius, stepInFeet, minEdgeOffsetInFeet,
                maxEdgeOffsetInFeet, out XYZ zoneStartPoint, out XYZ dir);

            if (!packSegments.Any())
                return new List<FamilyInstance>();

            FamilySymbol trapezeSymbol = null;
            FamilySymbol jHookSymbol = null;

            if (packSegments.Any(seg => seg.ElementCount > 1))
                trapezeSymbol = GetTrapezeSymbol();

            if (packSegments.Any(seg => seg.ElementCount == 1))
                jHookSymbol = GetJHookSymbol(conduitRadius * 2.0);

            var supports = new List<FamilyInstance>();

            foreach (var seg in packSegments)
            {
                FamilySymbol symbolToUse = seg.ElementCount == 1 ? jHookSymbol : trapezeSymbol;

                FamilyInstance support = CreateSupportAt(symbolToUse, elements, seg.Width,
                    level, rodOffsetInFeet, seg.Position);

                supports.Add(support);
            }

            return supports;
        }

        private FamilyInstance CreateSupportAt(
            FamilySymbol supportSymbol,
            List<Element> elements,
            double perpWidth,
            Level level,
            double rodOffsetInFeet,
            XYZ placementPoint)
        {
            Element hostElement = elements.First();

            FamilyInstance supportInstance = _doc.Create.NewFamilyInstance(
                placementPoint, supportSymbol, level, StructuralType.NonStructural);

            SetZValue(supportInstance, placementPoint.Z);
            RotatePerpendicular(supportInstance, placementPoint, hostElement);
            SetWidth(supportInstance, perpWidth, rodOffsetInFeet);
            CopyComment(hostElement, supportInstance);

            return supportInstance;
        }

        #region Private Helper Methods
        private void CopyComment(Element element, FamilyInstance supportInstance)
        {
            if (element != null)
            {
                Parameter conduitCommentParam = element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                if (conduitCommentParam != null && conduitCommentParam.HasValue)
                {
                    string commentValue = conduitCommentParam.AsString();

                    Parameter supportCommentParam = supportInstance.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                    if (supportCommentParam != null && !supportCommentParam.IsReadOnly)
                        supportCommentParam.Set(commentValue);
                }
            }
        }

        private void SetWidth(FamilyInstance support, double perpWidth, double rodOffsetInFeet) => support.LookupParameter("LENGTH")?.Set(perpWidth + (2 * rodOffsetInFeet));

        private void SetZValue(FamilyInstance support, double value) => support.LookupParameter("TOS-TIER1")?.Set(value);

        private double GetFirstConduitRadius(List<Element> elements)
        {
            Element firstElement = elements.First();

            Parameter diamParam = firstElement.get_Parameter(BuiltInParameter.RBS_CONDUIT_OUTER_DIAM_PARAM);

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
        #endregion

        #region Validation and Preparation
        private void EnsureSymbolIsActive(FamilySymbol symbol)
        {
            if (!symbol.IsActive)
            {
                symbol.Activate();
                _doc.Regenerate();
            }
        }

        private void ValidateSymbol(FamilySymbol symbol, string paramName)
        {
            if (symbol == null)
                throw new ArgumentNullException(paramName);
        }

        private bool PrepareAndValidateCurves(Level level,
            List<Element> elements, out List<Curve> curves)
        {
            curves = null;

            if (level == null)
                throw new ArgumentNullException(nameof(level), "Level is null.");

            if (elements == null || !elements.Any())
                return false;

            curves = _elementFinder.GetCurves(elements);

            ConduitValidator.Validate(elements, curves);

            return true;
        }

        private List<double> GetPlacementDistances(
            double totalLength,
            double stepInFeet,
            double minEdgeOffsetInFeet,
            double maxEdgeOffsetInFeet)
        {
            if (totalLength < MIN_WORK_ZONE_LENGTH_IN_FEET)
                throw new InvalidOperationException(
                    $"The selected conduit path is too short. Minimum required is {MIN_WORK_ZONE_LENGTH_IN_FEET:F2}\'.");

            return SupportPlacementCalculator.CalculateSymmetricPlacementDistances(
                    totalLength, stepInFeet, minEdgeOffsetInFeet, maxEdgeOffsetInFeet);
        }

        public FamilySymbol GetTrapezeSymbol()
        {
            FamilySymbol trapezeSymbol = _elementFinder.GetSupportSymbol(CommonConstants.TRAPEZE_FAMILY_NAME);

            ValidateSymbol(trapezeSymbol, nameof(trapezeSymbol));
            EnsureSymbolIsActive(trapezeSymbol);

            return trapezeSymbol;
        }

        public FamilySymbol GetJHookSymbol(double diameter)
        {
            FamilySymbol jHookSymbol = _elementFinder.GetJHookSymbolByDiameter(CommonConstants.JHOOK_FAMILY_NAME, diameter);

            ValidateSymbol(jHookSymbol, nameof(jHookSymbol));
            EnsureSymbolIsActive(jHookSymbol);

            return jHookSymbol;
        }
        #endregion
    }
}