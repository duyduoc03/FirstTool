using Autodesk.Revit.UI;
using FirstTool.Models;
using FirstTool.Services;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace FirstTool.ViewModels;

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
    [ObservableProperty] private Brush statusColor = Brushes.Gray;

    public FirstToolViewModel(Action bringWindowToFront)
    {
        this.bringWindowToFront = bringWindowToFront;
    }

    [RelayCommand]
    private void Tool1() => RunOnRevit(app =>
    {
        UIDocument uiDoc = app.ActiveUIDocument;
        Document doc = uiDoc.Document;

        var mainColumns = columnService.PickTwoColumns(uiDoc, doc);
        Element col1 = doc.GetElement(mainColumns[0].Id);
        Element col2 = doc.GetElement(mainColumns[1].Id);

        var availableTypes = placementService.GetAvailableColumnTypes(doc);
        if (availableTypes.Count == 0)
        {
            TaskDialog.Show("Lỗi", "Không tìm thấy Family Type cột nào trong mô hình.");
            return;
        }

        var inputDialog = new Views.ColumnPlacementInputView(availableTypes);
        if (inputDialog.ShowDialog() != true) return;

        int createdCount;
        using (Transaction tx = new Transaction(doc, "Tạo cột phụ"))
        {
            tx.Start();
            createdCount = placementService.PlaceColumnsBetween(
                doc, col1, col2, inputDialog.SelectedType.Symbol,
                inputDialog.Mode, inputDialog.Spacing, inputDialog.ColumnCount);
            tx.Commit();
        }

        TaskDialog.Show("Kết quả", $"Đã tạo thành công {createdCount} cột phụ.");
    });

    [RelayCommand]
    private void Tool2() => RunOnRevit(app =>
    {
        UIDocument uiDoc = app.ActiveUIDocument;
        Document doc = uiDoc.Document;

        ColumnModel picked = columnService.PickOneColumn(uiDoc, doc);
        selectedColumnElement = doc.GetElement(picked.Id);
        LoadParameters(doc);
        StatusText = $"Đang chỉnh sửa: {selectedColumnElement.Name}";
    }, bringToFrontAfter: true);

    [RelayCommand]
    private void Refresh() => RunOnRevit(app =>
    {
        if (selectedColumnElement == null) return;
        LoadParameters(app.ActiveUIDocument.Document);
    });

    [RelayCommand]
    private void Apply() => RunOnRevit(app =>
    {
        Document doc = app.ActiveUIDocument.Document;
        if (selectedColumnElement == null)
        {
            StatusText = "Chưa chọn cột nào để áp dụng.";
            StatusColor = Brushes.Red;
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
        {
            StatusText = string.Join("\n", errors);
            StatusColor = Brushes.Red;
            return;
        }

        StatusText = "Đã cập nhật parameter thành công.";
        StatusColor = Brushes.Green;
        TaskDialog.Show("Thành công", "Đã cập nhật parameter thành công.");
        LoadParameters(doc);
    });

    private void LoadParameters(Document doc)
    {
        if (selectedColumnElement == null) return;
        Parameters = new ObservableCollection<ParameterModel>(
            parameterService.GetParameters(doc, selectedColumnElement));
    }

    private void RunOnRevit(Action<UIApplication> action, bool bringToFrontAfter = false)
    {
        revitEvent.Run(app =>
        {
            try
            {
                action(app);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { }
            catch (Exception ex) { TaskDialog.Show("Lỗi", ex.Message); }
            finally
            {
                if (bringToFrontAfter) bringWindowToFront();
            }
        });
    }
}