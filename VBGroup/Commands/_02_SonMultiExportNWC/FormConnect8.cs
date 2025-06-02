using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;

namespace VBGroup.Commands._02_SonMultiExportNWC
{
    [Transaction(TransactionMode.Manual)]
    public class FormConnect8 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // UIApplication 및 Document 객체 가져오기
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            // Form1 클래스의 인스턴스 생성 및 표시
            Form25 form = new Form25(doc, uiApp);  // 올바른 매개변수 전달
            form.Show();

            // 명령 성공
            return Result.Succeeded;
        }

    }
}
