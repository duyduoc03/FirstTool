using FirstTool.Commands;
using Nice3point.Revit.Toolkit.External;

namespace FirstTool
{
    /// <summary>
    ///     Application entry point
    /// </summary>
    [UsedImplicitly]
    public class Application : ExternalApplication
    {
        public override void OnStartup()
        {
            CreateRibbon();
        }

        private void CreateRibbon()
        {
            var panel = Application.CreatePanel("Commands", "FirstTool");

            panel.AddPushButton<StartupCommand>("Execute")
                .SetImage("/FirstTool;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/FirstTool;component/Resources/Icons/RibbonIcon32.png");
        }
    }
}