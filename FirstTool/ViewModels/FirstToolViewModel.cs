using Autodesk.Revit.UI;
using FirstTool.Services;
using RevitTool.Models;
using RevitTool.Services;
using System.Collections.ObjectModel;

public sealed partial class FirstToolViewModel : ObservableObject
{
    private readonly RevitEventHandler revitEvent = new();
    private readonly ColumnService columnService = new();
    private readonly ColumnPlacementService placementService = new();
    private readonly ParameterService parameterService = new();
    private readonly Action bringWindowToFront;

    private Element? selectedColumnElement;

    [ObservableProperty] private ObservableCollection<ParameterModel> parameters = new();
    [ObservableProperty] private string statusText = string.Empty;

    public FirstToolViewModel(Action bringWindowToFront)
    {
        this.bringWindowToFront = bringWindowToFront;
    }

    [RelayCommand]
    private void Tool1()
    {
        revitEvent.Run(app =>
        {
            UIDocument uiDoc = app.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                var mainColumns = columnService.PickTwoColumns(uiDoc, doc);
                Element col1 = doc.GetElement(mainColumns[0].Id);
                Element col2 = doc.GetElement(mainColumns[1].Id);

                var availableTypes = placementService.GetAvailableColumnTypes(doc);
                if (availableTypes.Count == 0)
                {
                    TaskDialog.Show("Lỗi", "Không tìm thấy Family Type cột nào trong mô hình.");
                    return;
                }

                var inputDialog = new FirstTool.Views.ColumnPlacementInputView(availableTypes);
                if (inputDialog.ShowDialog() != true) return;

                int createdCount;
                using (Transaction tx = new Transaction(doc, "Tạo cột phụ"))
                {
                    tx.Start();
                    createdCount = placementService.PlaceColumnsBetween(
                        doc, col1, col2, inputDialog.SelectedType.Symbol, inputDialog.Spacing);
                    tx.Commit();
                }

                TaskDialog.Show("Kết quả", $"Đã tạo thành công {createdCount} cột phụ.");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }
            catch (Exception ex) { TaskDialog.Show("Lỗi", ex.Message); }
            finally { bringWindowToFront(); }
        });
    }

    [RelayCommand]
    private void Tool2()
    {
        revitEvent.Run(app =>
        {
            UIDocument uiDoc = app.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                ColumnModel picked = columnService.PickOneColumn(uiDoc, doc);
                selectedColumnElement = doc.GetElement(picked.Id);
                LoadParameters(doc);
                StatusText = $"Đang chỉnh sửa: {selectedColumnElement.Name}";
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }
            catch (Exception ex) { TaskDialog.Show("Lỗi", ex.Message); }
            finally { bringWindowToFront(); }
        });
    }

    [RelayCommand]
    private void Refresh()
    {
        revitEvent.Run(app =>
        {
            if (selectedColumnElement == null) return;
            LoadParameters(app.ActiveUIDocument.Document);
        });
    }

    [RelayCommand]
    private void Apply()
    {
        revitEvent.Run(app =>
        {
            Document doc = app.ActiveUIDocument.Document;
            if (selectedColumnElement == null)
            {
                TaskDialog.Show("Thông báo", "Chưa chọn cột nào để áp dụng.");
                return;
            }

            List<string> errors;
            using (Transaction tx = new Transaction(doc, "Cập nhật Parameter cột"))
            {
                tx.Start();
                errors = parameterService.ApplyParameters(doc, selectedColumnElement, Parameters.ToList());
                if (errors.Count > 0) tx.RollBack();
                else tx.Commit();
            }

            if (errors.Count > 0)
                TaskDialog.Show("Có lỗi khi cập nhật", string.Join("\n", errors));
            else
            {
                TaskDialog.Show("Thành công", "Đã cập nhật parameter thành công.");
                LoadParameters(doc);
            }
        });
    }

    private void LoadParameters(Document doc)
    {
        if (selectedColumnElement == null) return;
        Parameters = new ObservableCollection<ParameterModel>(
            parameterService.GetParameters(doc, selectedColumnElement));
    }
}