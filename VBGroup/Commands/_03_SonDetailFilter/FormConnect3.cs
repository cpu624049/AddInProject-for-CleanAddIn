using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;

namespace VBGroup.Commands._03_SonDetailFilter
{
    [Transaction(TransactionMode.Manual)]
    public class FormConnect3 : IExternalCommand // Detail Filter
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document; // ✅ 추가됨
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

            // ✅ 선택 정보 전달
            Form8 form = new Form8(doc, commandData, selectedIds);
            form.Show();

            return Result.Succeeded;
        }
    }
}
