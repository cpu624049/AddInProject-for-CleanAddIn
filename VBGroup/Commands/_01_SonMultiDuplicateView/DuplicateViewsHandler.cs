using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using revitView = Autodesk.Revit.DB.View;

namespace VBGroup.Commands._01_SonMultiDuplicateView
{
    public class DuplicateViewsHandler : IExternalEventHandler
    {
        public Document Document { get; set; }
        public List<revitView> SelectedViews { get; set; }
        public ViewDuplicateOption DuplicateOption { get; set; }
        public bool ApplyNewProjectPath { get; set; } // checkBox7 상태

        public void Execute(UIApplication app)
        {
            Debug.WriteLine("[Debug] DuplicateViewsHandler.Execute 시작");

            if (Document == null || SelectedViews == null || SelectedViews.Count == 0)
            {
                TaskDialog.Show("Error", "Document 또는 SelectedViews가 설정되지 않았습니다.");
                return;
            }

            using (Transaction trans = new Transaction(Document, "Duplicate Views"))
            {
                try
                {
                    trans.Start();
                    Debug.WriteLine("[Debug] Transaction 시작");

                    foreach (var view in SelectedViews)
                    {
                        try
                        {
                            // 뷰 복제
                            ElementId newViewId = view.Duplicate(DuplicateOption);
                            var newView = Document.GetElement(newViewId) as revitView;

                            if (newView != null && ApplyNewProjectPath)
                            {
                                // "Project Path" 매개변수 값 설정
                                Parameter projectPathParam = newView.LookupParameter("Project Path");
                                if (projectPathParam != null && !projectPathParam.IsReadOnly)
                                {
                                    projectPathParam.Set("NEW");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Error] '{view.Name}' 복제 실패: {ex.Message}");
                        }
                    }

                    trans.Commit();
                    Debug.WriteLine("[Debug] Transaction 완료");

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Error] Transaction 오류 발생: {ex.Message}");
                    TaskDialog.Show("Error", $"Transaction 실행 중 오류가 발생했습니다:\n{ex.Message}");
                    trans.RollBack();
                }
            }
        }

        public string GetName()
        {
            return "Duplicate Views Handler";
        }
    }

}
