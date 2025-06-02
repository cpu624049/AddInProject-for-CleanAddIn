using Autodesk.Revit.UI;

namespace VBGroup._01_WPFDetailFilter.ExternalEvents
{
    public class DetailFilterEventHandler : IExternalEventHandler
    {
        // 이벤트 객체
        public ExternalEvent Event { get; }

        public DetailFilterEventHandler()
        {
            Event = ExternalEvent.Create(this);
        }

        public void Execute(UIApplication uiApp)
        {
            var doc = uiApp.ActiveUIDocument.Document;
            var ids = ViewModels.DetailFilterViewModel.CheckedElementIds;
            using (var tx = new Transaction(doc, "Detail Filter Event"))
            {
                tx.Start();
                var settings = new OverrideGraphicSettings();
                foreach (var id in ids)
                {
                    doc.ActiveView.SetElementOverrides(id, settings);
                }
                tx.Commit();
            }
        }

        public string GetName() => "Detail Filter Event Handler";
    }
}
