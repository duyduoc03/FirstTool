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
        // Lấy điểm gốc (base point) thực tế của 1 cột, xử lý cả LocationPoint lẫn LocationCurve
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

        /// <summary>
        /// Tạo các cột phụ giữa 2 cột chính theo khoảng cách chỉ định.
        /// Trả về số lượng cột phụ đã tạo thành công.
        /// </summary>
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

            XYZ direction = (p2 - p1).Normalize();

            var (baseLevel, baseOffset, topId, topOffset) = GetColumnLevels(doc, mainColumn1);
            if (baseLevel == null)
                throw new InvalidOperationException("Không tìm thấy Base Level của cột chính.");

            if (!columnType.IsActive)
                columnType.Activate();

            int createdCount = 0;
            int segmentCount = (int)Math.Floor(totalLength / spacing);

            // Đặt cột phụ tại các điểm chia đều, không trùng vị trí 2 cột chính (bỏ qua điểm đầu/cuối)
            for (int i = 1; i < segmentCount; i++)
            {
                XYZ point = p1 + direction * (spacing * i);

                FamilyInstance newColumn = doc.Create.NewFamilyInstance(
                    point,
                    columnType,
                    baseLevel,
                    StructuralType.Column);

                // Gán lại Base/Top offset và Top level giống cột chính để đồng bộ chiều cao
                newColumn.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM)?.Set(baseOffset);
                if (topId != ElementId.InvalidElementId)
                {
                    newColumn.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?.Set(topId);
                    newColumn.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)?.Set(topOffset);
                }

                // Xoay cột phụ theo hướng của đoạn nối 2 cột chính (nếu không thẳng trục X)
                double angle = Math.Atan2(direction.Y, direction.X);
                if (Math.Abs(angle) > 1e-6)
                {
                    Line axis = Line.CreateBound(point, point + XYZ.BasisZ);
                    ElementTransformUtils.RotateElement(doc, newColumn.Id, axis, angle);
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