using Autodesk.Revit.DB;
using RevitTool.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace RevitTool.Services
{
    public class ParameterService
    {
        public List<ParameterModel> GetParameters(Document doc, Element element)
        {
            var result = new List<ParameterModel>();

            foreach (Parameter param in element.Parameters)
            {
                if (param?.Definition == null)
                    continue;

                string displayValue = GetDisplayValue(doc, param);

                result.Add(new ParameterModel
                {
                    Id = param.Id,
                    Name = param.Definition.Name,
                    StorageType = param.StorageType,
                    IsReadOnly = param.IsReadOnly,
                    Value = displayValue,
                    OriginalValue = displayValue
                });
            }

            return result;
        }

        private string GetDisplayValue(Document doc, Parameter param)
        {
            try
            {
                switch (param.StorageType)
                {
                    case StorageType.String:
                        return param.AsString() ?? string.Empty;

                    case StorageType.Integer:
                        if (param.Definition.GetDataType() == SpecTypeId.Boolean.YesNo)
                            return param.AsInteger() == 1 ? "Yes" : "No";
                        return param.AsInteger().ToString(CultureInfo.InvariantCulture);

                    case StorageType.Double:
                        double internalVal = param.AsDouble();
                        ForgeTypeId unitTypeId = param.GetUnitTypeId();
                        double displayVal = UnitUtils.ConvertFromInternalUnits(internalVal, unitTypeId);
                        return displayVal.ToString("F3", CultureInfo.InvariantCulture);

                    case StorageType.ElementId:
                        ElementId id = param.AsElementId();
                        if (id == ElementId.InvalidElementId) return string.Empty;
                        return doc.GetElement(id)?.Name ?? id.ToString();

                    default:
                        return param.AsValueString() ?? string.Empty;
                }
            }
            catch
            {
                return "<Không đọc được giá trị>";
            }
        }

        /// <summary>
        /// Áp dụng các thay đổi vào element. Trả về danh sách lỗi (nếu có), 1 dòng lỗi/parameter.
        /// </summary>
        public List<string> ApplyParameters(Document doc, Element element, List<ParameterModel> parameters)
        {
            var errors = new List<string>();

            foreach (var model in parameters)
            {
                if (model.IsReadOnly) continue;
                if (string.Equals(model.Value, model.OriginalValue, StringComparison.Ordinal)) continue;

                Parameter param = FindParameter(element, model.Id);
                if (param == null)
                {
                    errors.Add($"{model.Name}: không tìm thấy parameter.");
                    continue;
                }

                try
                {
                    SetParameterValue(param, model.Value);
                }
                catch (Exception ex)
                {
                    errors.Add($"{model.Name}: {ex.Message}");
                }
            }

            return errors;
        }

        private Parameter FindParameter(Element element, ElementId paramId)
        {
            foreach (Parameter p in element.Parameters)
                if (p.Id == paramId) return p;
            return null;
        }

        private void SetParameterValue(Parameter param, string rawValue)
        {
            switch (param.StorageType)
            {
                case StorageType.String:
                    param.Set(rawValue);
                    break;

                case StorageType.Integer:
                    if (param.Definition.GetDataType() == SpecTypeId.Boolean.YesNo)
                    {
                        bool boolVal = rawValue.Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase)
                            || rawValue.Trim() == "1";
                        param.Set(boolVal ? 1 : 0);
                    }
                    else
                    {
                        if (!int.TryParse(rawValue, out int intVal))
                            throw new ArgumentException("Giá trị không phải số nguyên hợp lệ.");
                        param.Set(intVal);
                    }
                    break;

                case StorageType.Double:
                    if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double displayVal))
                        throw new ArgumentException("Giá trị không phải số hợp lệ.");
                    ForgeTypeId unitTypeId = param.GetUnitTypeId();
                    param.Set(UnitUtils.ConvertToInternalUnits(displayVal, unitTypeId));
                    break;

                case StorageType.ElementId:
                    throw new ArgumentException("Không hỗ trợ chỉnh sửa trực tiếp giá trị ElementId.");

                default:
                    throw new ArgumentException("Kiểu dữ liệu không được hỗ trợ.");
            }
        }
    }
}