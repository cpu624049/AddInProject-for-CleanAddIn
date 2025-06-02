using db = Autodesk.Revit.DB;
using ui = Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Windows.Interop;
using VBGroup._01_WPFDetailFilter.ViewModels;
using VBGroup._01_WPFDetailFilter.Views;

namespace VBGroup._01_WPFDetailFilter.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class DetailFilterCommand : ui.IExternalCommand
    {
        public ui.Result Execute(
            ui.ExternalCommandData commandData,
            ref string message,
            db.ElementSet elements)
        {
            var uiApp = commandData.Application;
            // ViewModel에 UIDocument 전달
            var vm = new DetailFilterViewModel(uiApp.ActiveUIDocument);

            // WPF 창 인스턴스 생성 & DataContext 연결
            var window = new DetailFilterWindow
            {
                DataContext = vm
            };
            // 모델리스 창이 Revit 메인윈도우 위에 떠 있도록 설정
            new WindowInteropHelper(window)
            {
                Owner = uiApp.MainWindowHandle
            };

            // 모델리스로 보여주기
            window.Show();

            // Apply/Refresh 명령은 ViewModel 내부에서 ExternalEvent로 처리하므로
            // 커맨드 측에서는 더 이상 작업할 필요 없이 반환
            return ui.Result.Succeeded;
        }
    }
}
