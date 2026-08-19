using Autodesk.Revit.DB;
using FirstTool.Models;
using System.Globalization;

namespace FirstTool.Services;

public class ParameterService
{
    public List<ParameterModel> GetParameters(Document doc, Element element)
    {
        var result = new List<ParameterModel>();

        foreach (Parameter param in element.Parameters)
        {
            if (param?.Definition == null) continue;

            string displayValue = GetDisplayValue(doc, param);

            result.Add(new ParameterModel
            {
                Id = param.Id,
                Name = param.Definition.Name,
                GroupName = LabelUtils.GetLabelForGroup(param.Definition.GetGroupTypeId()),
                StorageType = param.StorageType,
                IsReadOnly = param.IsReadOnly,
                Value = displayValue,
                OriginalValue = displayValue
            });
        }

        return result;
    }

    public List<string> ApplyParameters(Document doc, Element element, List<ParameterModel> parameters)
    {
        var errors = new List<string>();

        foreach (var model in parameters)
        {
            model.HasError = false;
            model.ErrorMessage = string.Empty;

            if (model.IsReadOnly) continue;
            if (string.Equals(model.Value, model.OriginalValue, StringComparison.Ordinal)) continue;

            Parameter param = FindParameter(element, model.Id);
            if (param == null)
            {
                model.HasError = true;
                model.ErrorMessage = "Không tìm thấy parameter.";
                errors.Add($"{model.Name}: {model.ErrorMessage}");
                continue;
            }

            try
            {
                SetParameterValue(param, model.Value);
            }
            catch (Exception ex)
            {
                model.HasError = true;
                model.ErrorMessage = ex.Message;
                errors.Add($"{model.Name}: {ex.Message}");
            }
        }

        return errors;
    }

    private string GetDisplayValue(Document doc, Parameter param)
    {
        try
        {
            return param.StorageType switch
            {
                StorageType.String => param.AsString() ?? string.Empty,

                StorageType.Integer when param.Definition.GetDataType() == SpecTypeId.Boolean.YesNo =>
                    param.AsInteger() == 1 ? "Yes" : "No",

                StorageType.Integer => param.AsInteger().ToString(CultureInfo.InvariantCulture),

                StorageType.Double => UnitUtils
                    .ConvertFromInternalUnits(param.AsDouble(), param.GetUnitTypeId())
                    .ToString("F3", CultureInfo.InvariantCulture),

                StorageType.ElementId => GetElementIdDisplayValue(doc, param.AsElementId()),

                _ => param.AsValueString() ?? string.Empty
            };
        }
        catch
        {
            return "<Không đọc được giá trị>";
        }
    }

    private string GetElementIdDisplayValue(Document doc, ElementId id) =>
        id == ElementId.InvalidElementId ? string.Empty : doc.GetElement(id)?.Name ?? id.ToString();

    private Parameter FindParameter(Element element, ElementId paramId) =>
        element.Parameters.Cast<Parameter>().FirstOrDefault(p => p.Id == paramId);

    private void SetParameterValue(Parameter param, string rawValue)
    {
        switch (param.StorageType)
        {
            case StorageType.String:
                param.Set(rawValue);
                break;

            case StorageType.Integer when param.Definition.GetDataType() == SpecTypeId.Boolean.YesNo:
                bool boolVal = rawValue.Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase) || rawValue.Trim() == "1";
                param.Set(boolVal ? 1 : 0);
                break;

            case StorageType.Integer:
                if (!int.TryParse(rawValue, out int intVal))
                    throw new ArgumentException("Giá trị không phải số nguyên hợp lệ.");
                param.Set(intVal);
                break;

            case StorageType.Double:
                if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double displayVal))
                    throw new ArgumentException("Giá trị không phải số hợp lệ.");
                param.Set(UnitUtils.ConvertToInternalUnits(displayVal, param.GetUnitTypeId()));
                break;

            case StorageType.ElementId:
                throw new ArgumentException("Không hỗ trợ chỉnh sửa trực tiếp giá trị ElementId.");

            default:
                throw new ArgumentException("Kiểu dữ liệu không được hỗ trợ.");
        }
    }
}