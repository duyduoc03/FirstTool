using RevitTool.Models;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

namespace FirstTool.Views
{
    public partial class ColumnPlacementInputView : Window
    {
        public List<FamilyTypeModel> FamilyTypes { get; }
        public FamilyTypeModel? SelectedFamilyType { get; set; }
        public string SpacingInput { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public FamilyTypeModel SelectedType => SelectedFamilyType!;
        public double Spacing { get; private set; }

        public ColumnPlacementInputView(List<FamilyTypeModel> availableTypes)
        {
            InitializeComponent();
            FamilyTypes = availableTypes;
            DataContext = this;

            if (FamilyTypes.Count > 0)
                SelectedFamilyType = FamilyTypes[0];
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Đồng bộ giá trị từ control vì binding không dùng INotifyPropertyChanged đầy đủ
            SelectedFamilyType = FamilyTypeComboBox.SelectedItem as FamilyTypeModel;
            SpacingInput = SpacingTextBox.Text;

            if (SelectedFamilyType == null)
            {
                ErrorText.Text = "Vui lòng chọn Family Type.";
                return;
            }

            if (!double.TryParse(SpacingInput, NumberStyles.Float, CultureInfo.InvariantCulture, out double spacingMm)
                || spacingMm <= 0)
            {
                ErrorMessage = "Khoảng cách không hợp lệ. Vui lòng nhập số lớn hơn 0.";
                return;
            }

            // mm -> feet (đơn vị nội bộ của Revit)
            Spacing = Autodesk.Revit.DB.UnitUtils.ConvertToInternalUnits(
                spacingMm, Autodesk.Revit.DB.UnitTypeId.Millimeters);

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}