using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RevitTool.Models;
using System;
using System.Collections.Generic;

namespace RevitTool.Services
{
    public class ColumnService
    {
        // Chọn 2 cột chính, dùng cho Tool 1
        public List<ColumnModel> PickTwoColumns(UIDocument uiDoc, Document doc)
        {
            var result = new List<ColumnModel>();

            Reference ref1 = uiDoc.Selection.PickObject(ObjectType.Element, "Chọn cột chính 1");
            Element el1 = doc.GetElement(ref1.ElementId);

            Reference ref2 = uiDoc.Selection.PickObject(ObjectType.Element, "Chọn cột chính 2");
            Element el2 = doc.GetElement(ref2.ElementId);

            if (el1 == null || el2 == null)
                throw new InvalidOperationException("Chưa chọn đủ 2 cột.");

            if (el1.Id == el2.Id)
                throw new InvalidOperationException("Không thể chọn cùng một cột.");

            result.Add(ToColumnModel(el1));
            result.Add(ToColumnModel(el2));

            return result;
        }

        // Chọn 1 cột, dùng cho Tool 2
        public ColumnModel PickOneColumn(UIDocument uiDoc, Document doc)
        {
            Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, "Chọn cột");
            Element el = doc.GetElement(reference.ElementId);

            if (el == null)
                throw new InvalidOperationException("Chưa chọn cột nào.");

            return ToColumnModel(el);
        }

        private ColumnModel ToColumnModel(Element el)
        {
            return new ColumnModel
            {
                Id = el.Id,
                Name = el.Name
            };
        }
    }
}