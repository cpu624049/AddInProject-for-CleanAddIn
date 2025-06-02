using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using revitTaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace VBGroup.Commands._01_SonMultiDuplicateView
{
    [Transaction(TransactionMode.Manual)]
    public class FormConnect20 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // UIDocument와 UIApplication 객체 가져오기
                UIApplication uiApp = commandData.Application;

                // Form19 인스턴스 생성 (UIApplication 전달)
                Form19 form = new Form19(uiApp);
                form.Show();  // 비모달로 폼을 띄움

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"오류 발생: {ex.Message}";
                revitTaskDialog.Show("Error", message);
                return Result.Failed;
            }
        }
    }
}
