using Autodesk.Revit.UI;

namespace VBGroup.Commands._01_SonMultiDuplicateView
{
    public class UpdateCheckedListBoxHandler : IExternalEventHandler
    {
        private Form19 _form;

        public UpdateCheckedListBoxHandler(Form19 form)
        {
            _form = form;
        }

        public void Execute(UIApplication app)
        {
            // CheckedListBox 업데이트
            _form.LoadFloorPlans();
        }

        public string GetName()
        {
            return "Update CheckedListBox Handler";
        }
    }
}
