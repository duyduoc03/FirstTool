using Autodesk.Revit.DB;

namespace RevitTool.Models
{
    public class FamilyTypeModel
    {
        public required ElementId Id { get; set; }
        public required string Name { get; set; }
        public required FamilySymbol Symbol { get; set; }
    }
}