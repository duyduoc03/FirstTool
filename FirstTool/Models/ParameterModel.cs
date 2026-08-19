using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FirstTool.Models;

public partial class ParameterModel : ObservableObject
{
    public required ElementId Id { get; set; }
    public required string Name { get; set; }
    public required string GroupName { get; set; }
    public required StorageType StorageType { get; set; }
    public required bool IsReadOnly { get; set; }
    public string OriginalValue { get; set; } = string.Empty;

    [ObservableProperty]
    private string value = string.Empty;
}