using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using db = Autodesk.Revit.DB;
using ui = Autodesk.Revit.UI;
using VBGroup._01_WPFDetailFilter.ExternalEvents;

namespace VBGroup._01_WPFDetailFilter.ViewModels
{
    /// <summary>
    /// Detail Filter 다이얼로그의 ViewModel.
    /// - Revit의 선택 객체 기준으로 트리뷰를 구성합니다.
    /// - Apply, Cancel, Refresh 명령과 체크선택 개수를 관리합니다.
    /// </summary>
    public class DetailFilterViewModel : ObservableObject
    {
        // Revit UI 문서 참조 (선택 정보를 얻기 위해)
        private readonly ui.UIDocument _uiDocument;

        // 트리뷰 바인딩용 루트 노드 컬렉션
        public ObservableCollection<ElementNodeViewModel> RootNodes { get; } = new ObservableCollection<ElementNodeViewModel>();

        // ExternalEventHandler 인스턴스는 싱글톤으로 유지
        public static DetailFilterEventHandler DetailFilterEventHandler { get; } = new DetailFilterEventHandler();

        // Apply 시 체크된 ID 보관
        public static ElementId[] CheckedElementIds { get; private set; } = new db.ElementId[0];

        // MVVM 명령 커맨드
        public ICommand ApplyCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RefreshCommand { get; }

        // 다이얼로그 결과 플래그
        public bool DialogResult { get; private set; }

        // "선택된 객체 없음" 여부 → 트리뷰 메시지 토글용
        public bool IsNothingSelected => !_uiDocument.Selection.GetElementIds().Any();

        // 체크된 노드(인스턴스) 개수
        public int CheckedCount => RootNodes.SelectMany(r => r.FlattenChecked()).Count();

        // 전체 선택된 객체 개수 (ID 개수)
        public int TotalSelectedCount => _uiDocument.Selection.GetElementIds().Count;

        /// <summary>
        /// 생성자: UIDocument 전달받아 초기화
        /// </summary>
        public DetailFilterViewModel(ui.UIDocument uiDoc)
        {
            _uiDocument = uiDoc;

            // 명령 바인딩
            ApplyCommand = new RelayCommand(OnApply);
            CancelCommand = new RelayCommand(OnCancel);
            RefreshCommand = new RelayCommand(RefreshTree);

            // 처음 로드 시 트리 갱신
            RefreshTree();
        }

        /// <summary>
        /// Revit에서 현재 선택된 객체를 기준으로 트리뷰(RootNodes)를 갱신합니다.
        /// </summary>
        private void RefreshTree()
        {
            // 1) 선택된 ElementId 목록
            var selectedIds = _uiDocument.Selection.GetElementIds();

            // 2) 트리뷰 초기화
            RootNodes.Clear();

            // 3) 선택된 객체가 없으면 "선택한 객체 없음" 메시지 노드 추가
            if (!selectedIds.Any())
            {
                RootNodes.Add(new ElementNodeViewModel(name: "선택한 객체 없음", parent: null));
            }
            else
            {
                var doc = _uiDocument.Document;

                // 4) FamilyInstance 타입만 필터
                var instances = selectedIds.Select(id => doc.GetElement(id) as db.FamilyInstance).Where(fi => fi != null);

                // 5) 카테고리-패밀리-타입 그룹화
                var byCategory = instances.GroupBy(fi => fi.Category?.Name ?? "미확인 카테고리");

                foreach (var catGroup in byCategory)
                {
                    var catNode = new ElementNodeViewModel(catGroup.Key, null);

                    var byFamily = catGroup
                        .GroupBy(fi => fi.Symbol.FamilyName);

                    foreach (var famGroup in byFamily)
                    {
                        var famNode = new ElementNodeViewModel(famGroup.Key, catNode);

                        var byType = famGroup
                            .GroupBy(fi => fi.Symbol.Name);

                        foreach (var typeGroup in byType)
                        {
                            var typeNode = new ElementNodeViewModel(typeGroup.Key, famNode);

                            // 6) 각 인스턴스 ID 노드 추가
                            foreach (var inst in typeGroup)
                            {
                                typeNode.Children.Add(
                                    new ElementNodeViewModel(
                                        name: inst.Id.ToString(),
                                        parent: typeNode,
                                        id: inst.Id));
                            }

                            famNode.Children.Add(typeNode);
                        }

                        catNode.Children.Add(famNode);
                    }

                    RootNodes.Add(catNode);
                }
            }

            // 7) 바인딩 프로퍼티 갱신 알림
            OnPropertyChanged(nameof(IsNothingSelected));
            OnPropertyChanged(nameof(CheckedCount));
            OnPropertyChanged(nameof(TotalSelectedCount));
        }

        /// <summary>
        /// Apply 버튼 클릭 시 실행.
        /// - 체크된 ID 목록 저장
        /// - 창 닫기 → ExternalEvent 발생 순으로 처리
        /// </summary>
        private void OnApply()
        {
            CheckedElementIds = GetCheckedElementIds();

            // 창 먼저 닫고
            DialogResult = true;
            CloseWindow();

            // Revit API 호출 이벤트 발생
            DetailFilterEventHandler.Event.Raise();
        }

        /// <summary>
        /// Cancel 버튼 클릭 시 실행: 창만 닫습니다.
        /// </summary>
        private void OnCancel()
        {
            DialogResult = false;
            CloseWindow();
        }

        /// <summary>
        /// 다이얼로그 창 닫기: DataContext 일치 윈도우를 찾아 Close()
        /// </summary>
        private void CloseWindow()
        {
            var win = System.Windows.Application.Current.Windows
                        .OfType<Window>()
                        .FirstOrDefault(w => w.DataContext == this);
            win?.Close();
        }

        /// <summary>
        /// 체크된 트리 노드(ElementNodeViewModel)의 ElementId 배열 반환
        /// </summary>
        public db.ElementId[] GetCheckedElementIds() =>
            RootNodes
                .SelectMany(r => r.FlattenChecked())
                .Select(vm => vm.ElementId!)
                .ToArray();
    }
}
