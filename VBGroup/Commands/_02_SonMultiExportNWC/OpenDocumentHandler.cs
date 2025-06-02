using Autodesk.Revit.UI;
using System.Diagnostics;
using System.IO;

public class OpenDocumentHandler : IExternalEventHandler
{
    private string _rvtFilePath;
    private UIApplication _uiApp;
    private TreeView _treeView;

    public void SetData(UIApplication uiApp, string rvtFilePath, TreeView treeView)
    {
        _uiApp = uiApp;
        _rvtFilePath = rvtFilePath;
        _treeView = treeView;
    }

    public void Execute(UIApplication app)
    {
        try
        {
            if (string.IsNullOrEmpty(_rvtFilePath) || !File.Exists(_rvtFilePath))
            {
                MessageBox.Show($"파일이 존재하지 않거나 경로가 올바르지 않습니다: {_rvtFilePath}");
                return;
            }

            Debug.WriteLine($"[DEBUG] ModelPath 변환 시작: {_rvtFilePath}");
            ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(_rvtFilePath);

            if (modelPath == null || string.IsNullOrEmpty(ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath)))
            {
                MessageBox.Show($"ModelPath 변환 실패: {_rvtFilePath}");
                return;
            }

            Debug.WriteLine($"[DEBUG] 변환된 ModelPath: {ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath)}");

            OpenOptions openOptions = new OpenOptions();
            Document doc = app.Application.OpenDocumentFile(modelPath, openOptions);

            if (doc == null)
            {
                MessageBox.Show($"Revit 파일을 열 수 없습니다: {_rvtFilePath}");
                return;
            }

            Debug.WriteLine("[DEBUG] Revit 파일 열기 성공");

            var view3Ds = new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .Where(v => !v.IsTemplate && v.CanBePrinted)
                .ToList();

            if (!view3Ds.Any())
            {
                MessageBox.Show("이 파일에는 3D View가 없습니다.");
                return;
            }

            // UI 스레드에서 TreeView 업데이트
            _treeView.Invoke((MethodInvoker)delegate
            {
                TreeNode rootNode = new TreeNode(Path.GetFileName(_rvtFilePath)) { Tag = _rvtFilePath };
                foreach (View3D view in view3Ds)
                {
                    TreeNode viewNode = new TreeNode(view.Name) { Tag = view.Id };
                    rootNode.Nodes.Add(viewNode);
                }
                _treeView.Nodes.Add(rootNode);
                rootNode.Expand();
            });

            // 문서 닫기 (Revit Link 파일이 아닐 경우)
            if (!doc.IsLinked)
            {
                doc.Close(false);
            }

            Debug.WriteLine("[DEBUG] 3D View 로드 완료");
        }
        catch (Autodesk.Revit.Exceptions.InternalException ex)
        {
            Debug.WriteLine($"[ERROR] Revit 내부 예외 발생: {ex.Message}");
            MessageBox.Show($"Revit 내부 오류 발생: {ex.Message}\nRevit을 관리자 권한으로 실행해 보세요.", "Revit 내부 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] 3D View를 로드하는 중 예외 발생: {ex.Message}");
            MessageBox.Show($"3D View를 로드하는 중 오류 발생: {ex.Message}");
        }
    }

    public string GetName()
    {
        return "OpenDocumentHandler";
    }
}
