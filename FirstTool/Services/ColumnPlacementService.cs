using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using RevitTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitTool.Services
{
    public class ColumnPlacementService
    {
        private const int MaxColumnsPerRun = 500;

        private XYZ GetColumnBasePoint(Element column)
        {
            if (column.Location is LocationPoint lp)
                return lp.Point;

            if (column.Location is LocationCurve lc)
                return lc.Curve.GetEndPoint(0);

            throw new InvalidOperationException($"Không xác định được vị trí của cột {column.Id}.");
        }

        // Lấy cao độ Base/Top của cột chính để cột phụ có cùng chiều cao
        private (Level baseLevel, double baseOffset, ElementId topId, double topOffset) GetColumnLevels(Document doc, Element column)
        {
            Parameter baseLevelParam = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
            Parameter baseOffsetParam = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
            Parameter topLevelParam = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
            Parameter topOffsetParam = column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);

            if (baseLevelParam == null || topLevelParam == null)
                throw new InvalidOperationException("Cột được chọn không có thông tin Base/Top Level.");

            Level baseLevel = doc.GetElement(baseLevelParam.AsElementId()) as Level;
            double baseOffset = baseOffsetParam?.AsDouble() ?? 0;
            ElementId topId = topLevelParam.AsElementId();
            double topOffset = topOffsetParam?.AsDouble() ?? 0;

            return (baseLevel, baseOffset, topId, topOffset);
        }

        private double GetColumnRotation(Element column)
        {
            if (column.Location is LocationPoint lp)
                return lp.Rotation;

            return 0;
        }

        public int PlaceColumnsBetween(Document doc, Element mainColumn1, Element mainColumn2, FamilySymbol columnType, double spacing)
        {
            if (mainColumn1 == null || mainColumn2 == null)
                throw new ArgumentException("Chưa chọn đủ 2 cột chính.");

            if (mainColumn1.Id == mainColumn2.Id)
                throw new ArgumentException("Hai cột chính không được trùng nhau.");

            if (columnType == null)
                throw new ArgumentException("Chưa chọn Family Type cho cột phụ.");

            if (spacing <= 0)
                throw new ArgumentException("Khoảng cách bố trí phải lớn hơn 0.");

            XYZ p1 = GetColumnBasePoint(mainColumn1);
            XYZ p2 = GetColumnBasePoint(mainColumn2);

            double totalLength = p1.DistanceTo(p2);
            if (totalLength < 1e-6)
                throw new InvalidOperationException("Hai cột chính đang ở cùng một vị trí.");

            int segmentCount = (int)Math.Floor(totalLength / spacing);
            int columnsToCreate = Math.Max(0, segmentCount - 1);

            if (columnsToCreate > MaxColumnsPerRun)
            {
                double lengthMm = UnitUtils.ConvertFromInternalUnits(totalLength, UnitTypeId.Millimeters);
                throw new InvalidOperationException(
                    $"Khoảng cách bố trí quá nhỏ so với chiều dài đoạn nối ({lengthMm:F0} mm).\n" +
                    $"Với khoảng cách hiện tại sẽ tạo {columnsToCreate} cột, vượt quá giới hạn cho phép ({MaxColumnsPerRun} cột).\n" +
                    $"Vui lòng kiểm tra lại đơn vị hoặc tăng khoảng cách.");
            }

            XYZ direction = (p2 - p1).Normalize();

            var (baseLevel, baseOffset, topId, topOffset) = GetColumnLevels(doc, mainColumn1);
            if (baseLevel == null)
                throw new InvalidOperationException("Không tìm thấy Base Level của cột chính.");

            // Lấy góc xoay thực tế từ cột chính 1 — áp dụng cho toàn bộ cột phụ
            double rotation = GetColumnRotation(mainColumn1);

            if (!columnType.IsActive)
                columnType.Activate();

            int createdCount = 0;

            for (int i = 1; i < segmentCount; i++)
            {
                XYZ point = p1 + direction * (spacing * i);

                FamilyInstance newColumn = doc.Create.NewFamilyInstance(
                    point, columnType, baseLevel, StructuralType.Column);

                newColumn.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM)?.Set(baseOffset);
                if (topId != ElementId.InvalidElementId)
                {
                    newColumn.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?.Set(topId);
                    newColumn.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)?.Set(topOffset);
                }

                // Xoay cột phụ theo đúng góc xoay của cột chính
                if (Math.Abs(rotation) > 1e-6)
                {
                    Line axis = Line.CreateBound(point, point + XYZ.BasisZ);
                    ElementTransformUtils.RotateElement(doc, newColumn.Id, axis, rotation);
                }

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
    }
}