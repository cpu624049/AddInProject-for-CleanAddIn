using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace VBGroup.Commands._01_SonMultiDuplicateView
{
    public partial class Form19 : System.Windows.Forms.Form
    {
        private Document _doc;
        private ExternalEvent _duplicateViewsEvent;
        private DuplicateViewsHandler _duplicateHandler;
        private ExternalEvent _updateEvent;
        private UpdateCheckedListBoxHandler _updateHandler;
        private List<Autodesk.Revit.DB.View> _floorPlans;
        private UIApplication _uiApp;
        private TreeNode lastCheckedNode = null;
        private bool isShiftSelecting = false;


        public Form19(UIApplication uiApp)
        {
            InitializeComponent();
            _uiApp = uiApp;
            _doc = uiApp.ActiveUIDocument.Document;

            // 외부 이벤트 핸들러 초기화
            _duplicateHandler = new DuplicateViewsHandler();
            _duplicateViewsEvent = ExternalEvent.Create(_duplicateHandler);

            _updateHandler = new UpdateCheckedListBoxHandler(this);
            _updateEvent = ExternalEvent.Create(_updateHandler);

            treeView1.AfterCheck += treeView1_AfterCheck;

            // Floor Plans 로드
            LoadFloorPlans();

            // DocumentChanged 이벤트 등록
            _uiApp.Application.DocumentChanged += OnDocumentChanged;

            // 버튼 클릭 이벤트 연결
            button1.Click += button1_Click;

            treeView1.MouseDown += treeView1_MouseDown;

        }

        public void LoadFloorPlans()
        {
            treeView1.Nodes.Clear(); // TreeView 초기화

            var allowedViewTypes = new List<ViewType>
            {
                ViewType.FloorPlan,
                ViewType.CeilingPlan,
                ViewType.ThreeD,
                ViewType.Elevation
            };

            // 뷰 수집 및 필터링
            _floorPlans = new FilteredElementCollector(_doc)
                .OfClass(typeof(Autodesk.Revit.DB.View))
                .Cast<Autodesk.Revit.DB.View>()
                .Where(v => !v.IsTemplate && allowedViewTypes.Contains(v.ViewType))
                .OrderBy(v => GetLevelOrder(v)) // Level 기반 정렬 추가
                .ThenBy(v => v.Name, new NaturalStringComparer()) // 같은 레벨일 경우 이름 정렬
                .ToList();

            var groupedViews = _floorPlans.GroupBy(v =>
            {
                var projectPathParam = v.LookupParameter("Project Path");
                return projectPathParam != null && projectPathParam.HasValue
                    ? projectPathParam.AsString()
                    : "???"; // 매개변수가 없는 경우 기본 그룹
            });

            foreach (var group in groupedViews)
            {
                var groupNode = new TreeNode(group.Key);

                foreach (var view in group)
                {
                    var viewNode = new TreeNode(view.Name)
                    {
                        Tag = view
                    };
                    groupNode.Nodes.Add(viewNode);
                }

                treeView1.Nodes.Add(groupNode);
            }
        }

        private int GetLevelOrder(Autodesk.Revit.DB.View view)
        {
            if (view is ViewPlan viewPlan)
            {
                Level level = viewPlan.GenLevel;
                if (level != null)
                {
                    return level.Elevation > 0 ? (int)level.Elevation : (int)level.Elevation - 1000;
                }
            }
            return 99999; // 레벨이 없는 경우 가장 마지막에 정렬
        }



        private void OnDocumentChanged(object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
        {
            // 변경된 요소 확인
            var modified = e.GetModifiedElementIds();
            var deleted = e.GetDeletedElementIds();
            var added = e.GetAddedElementIds();

            // 변경이 발생하면 TreeView 업데이트 요청
            if (modified.Any() || deleted.Any() || added.Any())
            {
                _updateEvent.Raise();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // 복제 옵션 확인
            int selectedOptionsCount = 0;
            if (checkBox1.Checked) selectedOptionsCount++;
            if (checkBox2.Checked) selectedOptionsCount++;
            if (checkBox3.Checked) selectedOptionsCount++;

            string errorMessage = null;

            if (selectedOptionsCount > 1)
            {
                errorMessage = "복제 옵션은 한 가지만 선택할 수 있습니다.";
            }
            else if (selectedOptionsCount == 0)
            {
                errorMessage = "복제 옵션을 하나 선택해야 합니다.";
            }

            if (errorMessage != null)
            {
                TaskDialog.Show("Error", errorMessage);
                return;
            }

            // 복제 옵션 설정
            ViewDuplicateOption duplicateOption = ViewDuplicateOption.Duplicate; // 기본 값
            if (checkBox2.Checked) duplicateOption = ViewDuplicateOption.WithDetailing;
            if (checkBox3.Checked) duplicateOption = ViewDuplicateOption.AsDependent;

            // TreeView에서 선택된 하위 뷰 노드만 가져오기
            var selectedViews = new List<Autodesk.Revit.DB.View>();
            foreach (TreeNode groupNode in treeView1.Nodes)
            {
                foreach (TreeNode viewNode in groupNode.Nodes)
                {
                    // 하위 노드만 처리 (상위 노드는 Tag에 View 객체가 없으므로 제외)
                    if (viewNode.Checked && viewNode.Tag is Autodesk.Revit.DB.View view)
                    {
                        selectedViews.Add(view);
                    }
                }
            }

            if (!selectedViews.Any())
            {
                TaskDialog.Show("Error", "복제할 뷰를 선택하세요.");
                return;
            }

            // 핸들러 데이터 설정
            _duplicateHandler.Document = _doc;
            _duplicateHandler.SelectedViews = selectedViews;
            _duplicateHandler.DuplicateOption = duplicateOption;
            _duplicateHandler.ApplyNewProjectPath = checkBox7.Checked;

            // 외부 이벤트 실행
            _duplicateViewsEvent.Raise();
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action != TreeViewAction.ByMouse && e.Action != TreeViewAction.ByKeyboard)
                return;

            // 부모 노드 체크 시 자식 노드 전체 체크/체크 해제
            if (e.Node.Nodes.Count > 0)
            {
                foreach (TreeNode child in e.Node.Nodes)
                {
                    child.Checked = e.Node.Checked;
                }
            }
        }

        private void treeView1_MouseDown(object sender, MouseEventArgs e)
        {
            TreeNode clickedNode = treeView1.GetNodeAt(e.Location);

            if (clickedNode != null)
            {
                // Shift 키가 눌려있는 경우, 다중 선택 모드 활성화
                if (ModifierKeys.HasFlag(Keys.Shift) && lastCheckedNode != null)
                {
                    isShiftSelecting = true;
                    List<TreeNode> rangeNodes = GetNodesInRange(treeView1.Nodes, lastCheckedNode, clickedNode);

                    // 모든 노드 선택 및 체크 상태 변경
                    bool targetState = !clickedNode.Checked;
                    foreach (var node in rangeNodes)
                    {
                        node.Checked = targetState;
                    }

                    isShiftSelecting = false;
                }
                else
                {
                    // 일반 클릭 처리
                    clickedNode.Checked = !clickedNode.Checked;
                }

                lastCheckedNode = clickedNode;
            }
        }

        private List<TreeNode> GetNodesInRange(TreeNodeCollection nodes, TreeNode startNode, TreeNode endNode)
        {
            List<TreeNode> nodesInRange = new List<TreeNode>();
            bool inRange = false;

            foreach (TreeNode node in nodes)
            {
                if (node == startNode || node == endNode)
                {
                    nodesInRange.Add(node);
                    inRange = !inRange;

                    if (!inRange) break; // 시작 및 종료 노드가 같다면 종료
                }
                else if (inRange)
                {
                    nodesInRange.Add(node);
                }

                // 하위 노드 재귀 탐색
                nodesInRange.AddRange(GetNodesInRange(node.Nodes, startNode, endNode));
            }

            return nodesInRange;
        }

        private void ResetHighlight(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.BackColor = System.Drawing.Color.Empty; // 기본 배경색으로 초기화
                ResetHighlight(node.Nodes); // 하위 노드에 대해 재귀 호출
            }
        }

        private void HighlightNodes(List<TreeNode> nodes, bool highlight)
        {
            foreach (var node in nodes)
            {
                if (highlight)
                {
                    node.BackColor = System.Drawing.Color.LightBlue; // 선택된 노드의 배경색
                }
                else
                {
                    node.BackColor = System.Drawing.Color.Empty; // 색상을 기본값으로 초기화
                }
            }
        }




    }
}
