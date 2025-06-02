using System.Collections.ObjectModel;
using db = Autodesk.Revit.DB;

namespace VBGroup._01_WPFDetailFilter.ViewModels
{
    public class ElementNodeViewModel : ObservableObject
    {
        public string Name { get; }
        public db.ElementId? ElementId { get; }
        public ObservableCollection<ElementNodeViewModel> Children { get; }
            = new ObservableCollection<ElementNodeViewModel>();

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                SetProperty(ref _isChecked, value);
                // 하위 노드 동기화
                foreach (var child in Children)
                    child.IsChecked = value;
            }
        }

        public ElementNodeViewModel(string name, ElementNodeViewModel? parent, db.ElementId? id = null)
        {
            Name = name;
            ElementId = id;
        }

        public IEnumerable<ElementNodeViewModel> FlattenChecked()
        {
            // 재귀적으로 모든 Checked 노드 수집
            foreach (var child in Children.SelectMany(c => c.FlattenChecked()))
                yield return child;

            if (ElementId != null && IsChecked)
                yield return this;
        }
    }
}
