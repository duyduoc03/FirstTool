using FirstTool.Helpers;
using FirstTool.ViewModels;
using RevitTool.Models;
using System.Windows;
using System.Windows.Controls;

namespace FirstTool.Views
{
    public sealed partial class FirstToolView : Window
    {
        public FirstToolView()
        {
            DataContext = new FirstToolViewModel(() => RevitWindowHelper.BringToFront(this));
            InitializeComponent();
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is ParameterModel model && model.IsReadOnly)
            {
                e.Cancel = true;
            }
        }
    }
}