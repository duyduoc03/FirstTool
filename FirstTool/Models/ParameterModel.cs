using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RevitTool.Models
{
    public partial class ParameterModel : ObservableObject
    {
        public required ElementId Id { get; set; }
        public required string Name { get; set; }
        public required StorageType StorageType { get; set; }
        public required bool IsReadOnly { get; set; }

        [ObservableProperty]
        private string value = string.Empty;

        // Giá trị gốc để so sánh, tránh Set lại Parameter không đổi
        public string OriginalValue { get; set; } = string.Empty;
    }
}