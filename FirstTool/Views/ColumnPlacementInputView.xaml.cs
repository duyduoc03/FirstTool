using Autodesk.Revit.UI;
using FirstTool.Models;
using System.Globalization;
using System.Windows;

namespace FirstTool.Views;

public partial class ColumnPlacementInputView : Window
{
    public List<FamilyTypeModel> FamilyTypes { get; }
    public FamilyTypeModel? SelectedFamilyType { get; set; }
    public FamilyTypeModel SelectedType => SelectedFamilyType!;

    public PlacementMode Mode { get; private set; }
    public double Spacing { get; private set; }
    public int ColumnCount { get; private set; }

    public ColumnPlacementInputView(List<FamilyTypeModel> availableTypes)
    {
        InitializeComponent();
        FamilyTypes = availableTypes;
        DataContext = this;

        if (FamilyTypes.Count > 0)
            SelectedFamilyType = FamilyTypes[0];
    }

    private void ModeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (SpacingTextBox == null || CountTextBox == null) return;

        SpacingTextBox.IsEnabled = BySpacingRadio.IsChecked == true;
        CountTextBox.IsEnabled = ByCountRadio.IsChecked == true;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedFamilyType = FamilyTypeComboBox.SelectedItem as FamilyTypeModel;

        if (SelectedFamilyType == null)
        {
            TaskDialog.Show("Lỗi", "Vui lòng chọn Family Type.");
            return;
        }

        if (BySpacingRadio.IsChecked == true)
        {
            if (!double.TryParse(SpacingTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double spacingMm)
                || spacingMm <= 0)
            {
                TaskDialog.Show("Lỗi", "Khoảng cách không hợp lệ. Vui lòng nhập số lớn hơn 0.");
                return;
            }

            Mode = PlacementMode.BySpacing;
            Spacing = UnitUtils.ConvertToInternalUnits(spacingMm, UnitTypeId.Millimeters);
        }
        else
        {
            if (!int.TryParse(CountTextBox.Text, out int count) || count <= 0)
            {
                TaskDialog.Show("Lỗi", "Số lượng cột phụ không hợp lệ. Vui lòng nhập số nguyên lớn hơn 0.");
                return;
            }

            Mode = PlacementMode.ByCount;
            ColumnCount = count;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}