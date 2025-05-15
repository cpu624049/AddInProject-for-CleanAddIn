using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using VBGroup.Commands.SonDetailFilter;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;

public class DeleteElementsHandler : IExternalEventHandler
{
    private Document _doc;
    private List<ElementId> _elementIdsToDelete;
    private Action _onCompleted;  // 작업 완료 후 호출할 콜백
    private Form8 _form;

    public DeleteElementsHandler(Form8 form)
    {
        _form = form;
    }


    public void Execute(UIApplication app)
    {
        // Revit 트랜잭션 시작
        using (Transaction transaction = new Transaction(_doc, "Delete Elements"))
        {
            transaction.Start();

            try
            {
                // 선택된 요소들을 Revit 문서에서 삭제
                foreach (ElementId elementId in _elementIdsToDelete)
                {
                    Element element = _doc.GetElement(elementId);
                    if (element != null)
                    {
                        _doc.Delete(elementId);
                    }
                }

                transaction.Commit();

                // 작업 완료 후 콜백 호출
                _onCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                transaction.RollBack();
                TaskDialog.Show("Error", $"Failed to delete elements: {ex.Message}");
            }

        }
        _form.UpdateLabel2Count();
    }

    public string GetName()
    {
        return "DeleteElementsHandler";
    }

    public void SetParameters(Document doc, List<ElementId> elementIds, Action onCompleted)
    {
        _doc = doc;
        _elementIdsToDelete = elementIds;
        _onCompleted = onCompleted;  // 작업 완료 후 호출할 콜백 설정
    }
}
