using Autodesk.Revit.DB;

namespace FirstTool.Models;

public class ColumnModel
{
    public required ElementId Id { get; set; }
    public required string Name { get; set; }
}