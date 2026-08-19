using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using FirstTool.Models;

namespace FirstTool.Services;

public class ColumnPlacementService
{
    private const int MaxColumnsPerRun = 500;

    public int PlaceColumnsBetween(
        Document doc, Element mainColumn1, Element mainColumn2, FamilySymbol columnType,
        PlacementMode mode, double spacing, int columnCount)
    {
        if (mainColumn1 == null || mainColumn2 == null)
            throw new ArgumentException("Chưa chọn đủ 2 cột chính.");

        if (mainColumn1.Id == mainColumn2.Id)
            throw new ArgumentException("Hai cột chính không được trùng nhau.");

        if (columnType == null)
            throw new ArgumentException("Chưa chọn Family Type cho cột phụ.");

        XYZ p1 = GetColumnBasePoint(mainColumn1);
        XYZ p2 = GetColumnBasePoint(mainColumn2);

        double totalLength = p1.DistanceTo(p2);
        if (totalLength < 1e-6)
            throw new InvalidOperationException("Hai cột chính đang ở cùng một vị trí.");

        List<double> fractions = mode == PlacementMode.BySpacing
            ? BuildFractionsBySpacing(totalLength, spacing)
            : BuildFractionsByCount(columnCount);

        if (fractions.Count > MaxColumnsPerRun)
            throw new InvalidOperationException(
                $"Số lượng cột phụ ({fractions.Count}) vượt quá giới hạn cho phép ({MaxColumnsPerRun}).\n" +
                "Vui lòng kiểm tra lại khoảng cách hoặc số lượng đã nhập.");

        XYZ direction = (p2 - p1).Normalize();
        var (baseLevel, baseOffset, topId, topOffset) = GetColumnLevels(doc, mainColumn1);

        if (baseLevel == null)
            throw new InvalidOperationException("Không tìm thấy Base Level của cột chính.");

        double rotation1 = GetColumnRotation(mainColumn1);
        double rotation2 = GetColumnRotation(mainColumn2);

        if (!columnType.IsActive)
            columnType.Activate();

        int createdCount = 0;

        foreach (double t in fractions)
        {
            XYZ point = p1 + direction * (totalLength * t);
            double rotation = InterpolateAngle(rotation1, rotation2, t);

            CreateColumnAt(doc, point, columnType, baseLevel, baseOffset, topId, topOffset, rotation);
            createdCount++;
        }

        return createdCount;
    }

    public List<FamilyTypeModel> GetAvailableColumnTypes(Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_StructuralColumns)
            .Cast<FamilySymbol>()
            .Select(fs => new FamilyTypeModel
            {
                Id = fs.Id,
                Name = $"{fs.FamilyName} : {fs.Name}",
                Symbol = fs
            })
            .ToList();
    }

    private List<double> BuildFractionsBySpacing(double totalLength, double spacing)
    {
        if (spacing <= 0)
            throw new ArgumentException("Khoảng cách bố trí phải lớn hơn 0.");

        int segmentCount = (int)Math.Floor(totalLength / spacing);
        var fractions = new List<double>();

        for (int i = 1; i < segmentCount; i++)
            fractions.Add(spacing * i / totalLength);

        return fractions;
    }

    private List<double> BuildFractionsByCount(int columnCount)
    {
        if (columnCount <= 0)
            throw new ArgumentException("Số lượng cột phụ phải lớn hơn 0.");

        var fractions = new List<double>();
        int divisions = columnCount + 1;

        for (int i = 1; i <= columnCount; i++)
            fractions.Add((double)i / divisions);

        return fractions;
    }

    // Nội suy góc xoay theo đường ngắn nhất, tránh lệch pha 2π gây xoay sai hướng
    private double InterpolateAngle(double from, double to, double t)
    {
        double delta = to - from;
        while (delta > Math.PI) delta -= 2 * Math.PI;
        while (delta < -Math.PI) delta += 2 * Math.PI;
        return from + delta * t;
    }

    private void CreateColumnAt(Document doc, XYZ point, FamilySymbol columnType, Level baseLevel,
        double baseOffset, ElementId topId, double topOffset, double rotation)
    {
        FamilyInstance newColumn = doc.Create.NewFamilyInstance(point, columnType, baseLevel, StructuralType.Column);

        newColumn.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM)?.Set(baseOffset);
        if (topId != ElementId.InvalidElementId)
        {
            newColumn.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?.Set(topId);
            newColumn.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)?.Set(topOffset);
        }

        if (Math.Abs(rotation) > 1e-6)
        {
            Line axis = Line.CreateBound(point, point + XYZ.BasisZ);
            ElementTransformUtils.RotateElement(doc, newColumn.Id, axis, rotation);
        }
    }

    private XYZ GetColumnBasePoint(Element column) => column.Location switch
    {
        LocationPoint lp => lp.Point,
        LocationCurve lc => lc.Curve.GetEndPoint(0),
        _ => throw new InvalidOperationException($"Không xác định được vị trí của cột {column.Id}.")
    };

    private double GetColumnRotation(Element column) =>
        column.Location is LocationPoint lp ? lp.Rotation : 0;

    private (Level baseLevel, double baseOffset, ElementId topId, double topOffset) GetColumnLevels(Document doc, Element column)
    {
        Parameter baseLevelParam = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
        Parameter baseOffsetParam = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
        Parameter topLevelParam = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
        Parameter topOffsetParam = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);

        if (baseLevelParam == null || topLevelParam == null)
            throw new InvalidOperationException("Cột được chọn không có thông tin Base/Top Level.");

        return (
            doc.GetElement(baseLevelParam.AsElementId()) as Level,
            baseOffsetParam?.AsDouble() ?? 0,
            topLevelParam.AsElementId(),
            topOffsetParam?.AsDouble() ?? 0);
    }
}