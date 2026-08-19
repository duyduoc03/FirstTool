using Autodesk.Revit.DB;

namespace FirstTool.Models;

public class FamilyTypeModel
{
    public required ElementId Id { get; set; }
    public required string Name { get; set; }
    public required FamilySymbol Symbol { get; set; }
}