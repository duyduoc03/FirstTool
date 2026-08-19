using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using FirstTool.Models;

namespace FirstTool.Services;

public class ColumnService
{
    public List<ColumnModel> PickTwoColumns(UIDocument uiDoc, Document doc)
    {
        Element el1 = PickElement(uiDoc, doc, "Chọn cột chính 1");

        Element el2;
        while (true)
        {
            el2 = PickElement(uiDoc, doc, "Chọn cột chính 2");

            if (el1.Id != el2.Id)
                break;

            TaskDialog.Show(
                "Thông báo",
                "Cột 2 trùng với cột 1.\nVui lòng chọn lại cột chính 2.");
        }

        return [ToColumnModel(el1), ToColumnModel(el2)];
    }

    public ColumnModel PickOneColumn(UIDocument uiDoc, Document doc)
    {
        return ToColumnModel(PickElement(uiDoc, doc, "Chọn cột"));
    }

    private Element PickElement(UIDocument uiDoc, Document doc, string prompt)
    {
        var filter = new StructuralColumnSelectionFilter();
        Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, filter, prompt);
        Element element = doc.GetElement(reference.ElementId);

        if (element == null)
            throw new InvalidOperationException("Không tìm thấy phần tử đã chọn.");

        if (!IsStructuralColumn(element))
            throw new InvalidOperationException(
                "Đối tượng được chọn không phải cột kết cấu.\nVui lòng chọn lại cột.");

        return element;
    }

    private static bool IsStructuralColumn(Element element)
    {
        if (element is FamilyInstance fi && fi.StructuralType == StructuralType.Column)
            return true;

        return element.Category?.Id.Value == (long)BuiltInCategory.OST_StructuralColumns;
    }

    private ColumnModel ToColumnModel(Element element) => new()
    {
        Id = element.Id,
        Name = element.Name
    };
}

public class StructuralColumnSelectionFilter : ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        if (elem is not FamilyInstance fi)
            return false;

        return fi.StructuralType == StructuralType.Column
               || elem.Category?.Id.Value == (long)BuiltInCategory.OST_StructuralColumns;
    }

    public bool AllowReference(Reference reference, XYZ position) => false;
}