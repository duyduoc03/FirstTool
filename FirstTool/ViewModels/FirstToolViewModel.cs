using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevitTool.Models;
using RevitTool.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;

namespace FirstTool.ViewModels
{
    public sealed partial class FirstToolViewModel : ObservableObject
    {
        private readonly ColumnService columnService = new();
        private readonly ColumnPlacementService placementService = new();
        private readonly ParameterService parameterService = new();

        private Element? selectedColumnElement;

        [ObservableProperty]
        private ObservableCollection<ParameterModel> parameters = new();

        [ObservableProperty]
        private string statusText = string.Empty;

        // ---------- TOOL 1: Bố trí cột phụ ----------
        [RelayCommand]
        private void Tool1()
        {
            Document doc = Context.ActiveDocument;
            UIDocument uiDoc = Context.ActiveUIDocument;
            if (doc == null || uiDoc == null) return;

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

                var inputDialog = new Views.ColumnPlacementInputView(availableTypes);
                if (inputDialog.ShowDialog() != true) return; // Cancel

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
            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { /* Esc khi pick */ }
            catch (Exception ex) { TaskDialog.Show("Lỗi", ex.Message); }
        }

        // ---------- TOOL 2: Quản lý Parameter ----------
        [RelayCommand]
        private void Tool2()
        {
            Document doc = Context.ActiveDocument;
            UIDocument uiDoc = Context.ActiveUIDocument;
            if (doc == null || uiDoc == null) return;

            try
            {
                ColumnModel picked = columnService.PickOneColumn(uiDoc, doc);
                selectedColumnElement = doc.GetElement(picked.Id);
                LoadParameters(doc);
                StatusText = $"Đang chỉnh sửa: {selectedColumnElement.Name}";
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { /* Esc khi pick */ }
            catch (Exception ex) { TaskDialog.Show("Lỗi", ex.Message); }
        }

        [RelayCommand]
        private void Refresh()
        {
            Document doc = Context.ActiveDocument;
            if (doc == null || selectedColumnElement == null) return;
            LoadParameters(doc);
        }

        [RelayCommand]
        private void Apply()
        {
            Document doc = Context.ActiveDocument;
            if (doc == null || selectedColumnElement == null)
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
        }

        private void LoadParameters(Document doc)
        {
            if (selectedColumnElement == null) return;
            Parameters = new ObservableCollection<ParameterModel>(
                parameterService.GetParameters(doc, selectedColumnElement));
        }
    }
}