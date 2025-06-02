using System.IO;
using Autodesk.Revit.UI;
using System.Diagnostics;
using revitTaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace VBGroup.Commands._02_SonMultiExportNWC
{
    public partial class Form25 : System.Windows.Forms.Form
    {
        private List<string> selectedRvtFilePaths = new List<string>();  // 선택한 RVT 파일 경로 목록
        private Document _doc;  // 현재 문서 저장 (필드 이름 변경)
        private UIApplication _uiApp;  // Revit UIApplication 객체 저장

        public Form25(Document doc, UIApplication uiApp)
        {
            InitializeComponent();
            this._doc = doc;
            this._uiApp = uiApp;

            textBox3.Text = "Navisworks";
        }

        public void Form1_Load(object sender, EventArgs e)
        {
            textBox3.Text = "Navisworks";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Revit Files (*.rvt)|*.rvt";
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filePath in openFileDialog.FileNames)
                    {
                        if (!selectedRvtFilePaths.Contains(filePath))
                        {
                            selectedRvtFilePaths.Add(filePath);
                            listView1.Items.Add(Path.GetFileName(filePath));
                        }
                    }

                    // 🔹 선택된 파일 경로들을 textBox1에 표시 (줄바꿈으로 구분)
                    textBox1.Text = string.Join(Environment.NewLine, selectedRvtFilePaths);
                }
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (selectedRvtFilePaths == null || selectedRvtFilePaths.Count == 0)
            {
                revitTaskDialog.Show("Error", "RVT 파일을 먼저 선택하세요.");
                WriteDebug("[DEBUG] 선택된 RVT 파일 없음");
                return;
            }

            string saveDirectory = textBox2.Text;
            if (string.IsNullOrEmpty(saveDirectory))
            {
                revitTaskDialog.Show("Error", "NWC 저장 경로를 선택하세요.");
                WriteDebug("[DEBUG] NWC 저장 경로 없음");
                return;
            }

            string viewFilterText = textBox3.Text?.Trim();  // ← 사용자가 입력한 뷰 이름 필터
            if (string.IsNullOrEmpty(viewFilterText))
            {
                revitTaskDialog.Show("Error", "뷰 이름 필터를 입력하세요.");
                WriteDebug("[DEBUG] 뷰 이름 필터 없음");
                return;
            }

            foreach (string rvtFilePath in selectedRvtFilePaths)
            {
                ExportToNWC(rvtFilePath, saveDirectory, viewFilterText);
            }
            MessageBox.Show("Success!", "Result");
        }


        private void ExportToNWC(string rvtFilePath, string saveDirectory, string viewFilterText)
        {
            ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(rvtFilePath);

            OpenOptions openOpts = new OpenOptions()
            {
                DetachFromCentralOption = DetachFromCentralOption.DetachAndPreserveWorksets,
                Audit = false
            };
            openOpts.SetOpenWorksetsConfiguration(new WorksetConfiguration(WorksetConfigurationOption.OpenAllWorksets));

            Document doc = null;

            try
            {
                doc = _uiApp.Application.OpenDocumentFile(modelPath, openOpts);

                // 사용자가 입력한 문자열이 포함된 모든 3D 뷰 필터
                List<View3D> exportViews = new FilteredElementCollector(doc)
                    .OfClass(typeof(View3D))
                    .Cast<View3D>()
                    .Where(v => !v.IsTemplate && v.Name.Contains(viewFilterText))
                    .ToList();

                if (!exportViews.Any())
                {
                    revitTaskDialog.Show("Error", $"{rvtFilePath}: '{viewFilterText}' 관련 뷰가 없습니다.");
                    WriteDebug($"[ERROR] 뷰 없음: {rvtFilePath}, 필터: {viewFilterText}");
                    return;
                }

                foreach (var exportView in exportViews)
                {
                    // ✅ 파일 이름에서 뷰 이름 제외
                    string nwcFileName = $"{Path.GetFileNameWithoutExtension(rvtFilePath)}_{exportView.Name}.nwc";


                    NavisworksExportOptions nwcOptions = new NavisworksExportOptions()
                    {
                        ExportScope = NavisworksExportScope.View,
                        ViewId = exportView.Id
                    };

                    doc.Export(saveDirectory, nwcFileName, nwcOptions);

                    WriteDebug($"[SUCCESS] NWC Export 완료: {nwcFileName}");
                }
            }
            catch (Exception ex)
            {
                WriteDebug($"[ERROR] NWC Export 실패 ({Path.GetFileName(rvtFilePath)}): {ex.Message}");
                revitTaskDialog.Show("Export Error", $"{Path.GetFileName(rvtFilePath)} Export 실패: {ex.Message}");
            }
            finally
            {
                if (doc != null && doc.IsValidObject) doc.Close(false);
            }
        }


        private void WriteDebug(string message)
        {
            Debug.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
            //string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NWCExportDebugLog.txt");
            //File.AppendAllText(logFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}{Environment.NewLine}");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
            {
                if (folderBrowser.ShowDialog() == DialogResult.OK)
                {
                    textBox2.Text = folderBrowser.SelectedPath;
                }
            }
        }
    }
}
