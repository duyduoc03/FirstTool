using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using FirstTool.Models;

namespace FirstTool.Services;

public class ColumnService
{
    public List<ColumnModel> PickTwoColumns(UIDocument uiDoc, Document doc)
    {
        Element el1 = PickElement(uiDoc, doc, "Chọn cột chính 1");
        Element el2 = PickElement(uiDoc, doc, "Chọn cột chính 2");

        if (el1.Id == el2.Id)
            throw new InvalidOperationException("Không thể chọn cùng một cột.");

        return [ToColumnModel(el1), ToColumnModel(el2)];
    }

    public ColumnModel PickOneColumn(UIDocument uiDoc, Document doc)
    {
        return ToColumnModel(PickElement(uiDoc, doc, "Chọn cột"));
    }

    private Element PickElement(UIDocument uiDoc, Document doc, string prompt)
    {
        Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, prompt);
        Element element = doc.GetElement(reference.ElementId);

        if (element == null)
            throw new InvalidOperationException("Không tìm thấy phần tử đã chọn.");

        return element;
    }

    private ColumnModel ToColumnModel(Element element) => new()
    {
        Id = element.Id,
        Name = element.Name
    };
}