using Autodesk.Revit.Attributes;
using FirstTool.Views;
using Nice3point.Revit.Toolkit.External;

namespace FirstTool.Commands
{
    /// <summary>
    ///     External command entry point.
    /// </summary>
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class StartupCommand : ExternalCommand
    {
        public override void Execute()
        {
            var view = new FirstToolView();
            view.Show();
        }
    }
}