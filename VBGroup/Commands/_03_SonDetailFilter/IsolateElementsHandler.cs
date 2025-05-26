using Autodesk.Revit.UI;

public class IsolateElementsHandler : IExternalEventHandler
{
    private Document _doc;
    private List<ElementId> _elementIds;
    private bool _isolate;

    public void SetParameters(Document doc, List<ElementId> elementIds, bool isolate)
    {
        _doc = doc;
        _elementIds = elementIds;
        _isolate = isolate;
    }

    public void Execute(UIApplication app)
    {
        if (_doc != null && _doc.ActiveView != null)
        {
            using (Transaction tx = new Transaction(_doc, "Isolate/Reset Elements"))
            {
                tx.Start();
                var view = _doc.ActiveView as Autodesk.Revit.DB.View;
                if (_isolate)
                {
                    view.IsolateElementsTemporary(_elementIds);
                }
                else
                {
                    view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                }
                tx.Commit();
            }
        }
    }

    public string GetName()
    {
        return "Isolate/Reset Elements Handler";
    }
}
