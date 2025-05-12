using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using Color = System.Drawing.Color;
using View = Autodesk.Revit.DB.View; // ❗ 필요



namespace VBGroup.Commands.SonDetailFilter
{
    public partial class Form8 : System.Windows.Forms.Form
    {
        private Document _doc;
        private ExternalCommandData _commandData;
        private IsolateElementsHandler _handler; // 멤버 변수 추가
        private ExternalEvent _externalEvent; // 멤버 변수 추가
        private DeleteElementsHandler _deleteHandler; // 멤버 변수 추가
        private ExternalEvent _deleteExternalEvent; // 멤버 변수 추가
        private Dictionary<TreeNode, List<Element>> _categoryElementsMap = new Dictionary<TreeNode, List<Element>>();
        private bool _radioButton1Selected = true;
        private List<TreeNode> _selectedNodes = new List<TreeNode>();
        private bool _shiftPressed = false;
        private TreeNode _lastClickedNode = null;
        private ApplyGraphicsOverrideHandler _graphicsHandler;
        private ExternalEvent _graphicsEvent;
        private ICollection<ElementId> _preselectedIds;

        public Form8(Document doc, ExternalCommandData commandData, ICollection<ElementId> preselectedIds)
        {
            _doc = doc;
            _commandData = commandData;
            _preselectedIds = preselectedIds;
            InitializeComponent();
            treeView1.AfterCheck += treeView1_AfterCheck;


            this.button2.Click += new System.EventHandler(this.button2_Click);
            this.button4.Click += new System.EventHandler(this.button4_Click);
            this.button6.Click += new System.EventHandler(this.button6_Click);

            this.Load += new System.EventHandler(this.Form8_Load);

            this.radioButton1.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            this.radioButton2.CheckedChanged += new System.EventHandler(this.radioButton2_CheckedChanged);

            _handler = new IsolateElementsHandler();
            _externalEvent = ExternalEvent.Create(_handler);
            _deleteHandler = new DeleteElementsHandler(this);
            _deleteExternalEvent = ExternalEvent.Create(_deleteHandler);

            treeView1.CheckBoxes = true;
            treeView1.HideSelection = false;

            // 이벤트 연결
            treeView1.KeyDown += TreeView1_KeyDown;
            treeView1.KeyUp += TreeView1_KeyUp;
            treeView1.NodeMouseClick += TreeView1_NodeMouseClick;
            treeView1.AfterCheck += TreeView1_AfterCheck;

            label2.Text = "선택된 항목 수:";

            _graphicsHandler = new ApplyGraphicsOverrideHandler();
            _graphicsEvent = ExternalEvent.Create(_graphicsHandler);

            LoadSurfacePatternsAndColors(); // 색상 콤보박스 초기화
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged; // ✅ 이벤트 연결

            trackBar1.Scroll += trackBar1_Scroll;

            trackBar1.Minimum = 0;
            trackBar1.Maximum = 100;
            trackBar1.TickFrequency = 10;
            trackBar1.Value = 0; // 기본값: 불투명



        }

        private void Form8_Load(object sender, EventArgs e)
        {
            LoadSurfacePatternsAndColors();
            label2.Text = "선택된 항목 수:";

            treeView1.Nodes.Clear();

            if (_preselectedIds != null && _preselectedIds.Count > 0)
            {
                radioButton2.Checked = true;
                LoadSelectedElementsTreeView(_preselectedIds); // 선택된 요소만 표시
            }
            else
            {
                radioButton1.Checked = true;
                LoadTreeView(); // 전체 뷰 요소 표시
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // TreeView 업데이트를 위해 기존 노드를 모두 제거하고 다시 로드
            treeView1.Nodes.Clear();
            if (_radioButton1Selected)
            {
                LoadTreeView();
            }
            else
            {
                LoadSelectedElementsTreeView();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // 선택된 노드를 기반으로 요소를 격리
            List<ElementId> elementIds = new List<ElementId>();
            foreach (TreeNode node in GetCheckedNodes(treeView1.Nodes))
            {
                if (node.Tag is Element element)
                {
                    elementIds.Add(element.Id);
                }
                else
                {
                    // 노드가 카테고리 노드인 경우, 자식 노드들을 포함
                    elementIds.AddRange(GetChildElementIds(node));
                }
            }

            if (elementIds.Count > 0)
            {
                _handler.SetParameters(_doc, elementIds, true);
                _externalEvent.Raise();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            // 선택된 요소들을 담을 리스트
            List<Element> elementsToDelete = new List<Element>();

            // 트리뷰의 최상위 노드를 탐색하여 개별 TreeNode에 대해 CollectElementsToDelete 호출
            foreach (TreeNode node in treeView1.Nodes)
            {
                // 상위 노드가 체크되지 않더라도 하위 노드를 재귀적으로 탐색
                CollectElementsToDelete(node, elementsToDelete);
            }

            if (elementsToDelete.Count > 0)
            {
                // 삭제할 요소의 유효성 검사
                List<ElementId> elementIdsToDelete = elementsToDelete
                    .Where(el => el != null && el.IsValidObject)  // 유효한 요소만 필터링
                    .Select(el => el.Id)
                    .ToList();

                if (elementIdsToDelete.Count > 0)
                {
                    // Revit 요소 삭제
                    try
                    {
                        _deleteHandler.SetParameters(_doc, elementIdsToDelete, () =>
                        {
                            // 요소 삭제 후 TreeView에서 삭제된 요소 업데이트
                            RemoveDeletedNodes(treeView1.Nodes, elementsToDelete);  // 삭제된 노드 제거
                            UpdateCategoryNodeCount();  // 카테고리별 요소 수 업데이트
                        });

                        _deleteExternalEvent.Raise();
                    }
                    catch (Autodesk.Revit.Exceptions.InvalidObjectException ex)
                    {
                        TaskDialog.Show("Error", $"Failed to delete elements: {ex.Message}");
                        return;
                    }
                }
                else
                {
                    // 삭제할 유효한 요소가 없는 경우
                    MessageBox.Show("No valid elements found for deletion.");
                }
            }
            else
            {
                // 삭제할 요소가 없는 경우 메시지 표시
                MessageBox.Show("No elements selected for deletion.");
            }
            UpdateSelectedCountLabel();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            // TreeView의 모든 노드를 재귀적으로 탐색하여 체크박스 해제
            UncheckAllNodes(treeView1.Nodes);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            // 체크된 모든 ElementId를 저장할 리스트
            List<ElementId> selectedElementIds = new List<ElementId>();

            Debug.WriteLine("Starting to collect checked nodes...");

            // 트리뷰에서 체크된 노드들을 수집
            CollectCheckedNodesRecursively(treeView1.Nodes, selectedElementIds);

            Debug.WriteLine($"Total Elements Selected: {selectedElementIds.Count}");

            // 선택된 요소가 있는지 확인
            if (selectedElementIds.Count > 0)
            {
                UIDocument uiDoc = new UIDocument(_doc);
                Debug.WriteLine("Elements found. Selecting elements in Revit...");
                uiDoc.Selection.SetElementIds(selectedElementIds);  // Revit에서 요소 선택
                uiDoc.ShowElements(selectedElementIds);  // 선택된 요소 표시
            }
            else
            {
                // 선택된 요소가 없을 때 경고 메시지 표시
                MessageBox.Show("TreeView에서 선택된 요소가 없습니다.");
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            treeView1.ExpandAll();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            treeView1.CollapseAll();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            foreach (TreeNode topNode in treeView1.Nodes)
            {
                ExpandCheckedNodes(topNode);
            }
        }
        private void ExpandCheckedNodes(TreeNode node)
        {
            if (node.Checked)
            {
                node.ExpandAll();
            }

            foreach (TreeNode child in node.Nodes)
            {
                ExpandCheckedNodes(child);
            }
        }


        private void button12_Click(object sender, EventArgs e)
        {
            foreach (TreeNode topNode in treeView1.Nodes)
            {
                CollapseCheckedNodes(topNode);
            }
        }
        private void CollapseCheckedNodes(TreeNode node)
        {
            if (node.Checked)
            {
                node.Collapse();
            }

            foreach (TreeNode child in node.Nodes)
            {
                CollapseCheckedNodes(child);
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            // 1. 현재 뷰에 보이는 모든 요소 수집
            FilteredElementCollector collector = new FilteredElementCollector(_doc, _doc.ActiveView.Id);
            List<ElementId> allIdsInView = collector
                .WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .Select(el => el.Id)
                .ToList();

            if (allIdsInView.Count == 0)
            {
                MessageBox.Show("현재 뷰에 리셋할 요소가 없습니다.");
                return;
            }

            // 2. 핸들러로 전체 리셋 요청
            _graphicsHandler.IsResetColor = true;
            _graphicsHandler.TransparencyValue = 0;
            _graphicsHandler.ColorToApply = null;
            _graphicsHandler.TargetElementIds = allIdsInView;
            _graphicsHandler.Doc = _doc;
            _graphicsHandler.View = _doc.ActiveView;

            _graphicsEvent.Raise();

            // 3. ✅ UI 상태도 초기화
            comboBox1.SelectedIndex = 0;   // 색 없음
            trackBar1.Value = 0;           // 투명도 초기화
        }





        private List<ElementId> GetCheckedElementIds()
        {
            List<ElementId> ids = new List<ElementId>();

            foreach (TreeNode node in treeView1.Nodes)
            {
                CollectCheckedElementIdsRecursive(node, ids);
            }

            return ids;
        }

        private void CollectCheckedElementIdsRecursive(TreeNode node, List<ElementId> ids)
        {
            if (node.Checked && node.Tag is Element element)
            {
                ids.Add(element.Id);
            }

            foreach (TreeNode child in node.Nodes)
            {
                CollectCheckedElementIdsRecursive(child, ids);
            }
        }

        private void CollectCheckedNodesRecursively(TreeNodeCollection nodes, List<ElementId> selectedElementIds)
        {
            foreach (TreeNode node in nodes)
            {

                // 체크된 노드만 처리
                if (node.Checked)
                {
                    if (node.Tag is Element element)
                    {
                        selectedElementIds.Add(element.Id);
                    }
                }

                // 하위 노드를 재귀적으로 탐색
                if (node.Nodes.Count > 0)
                {
                    CollectCheckedNodesRecursively(node.Nodes, selectedElementIds);
                }
            }
        }

        private void UncheckAllNodes(TreeNodeCollection nodes)
        {
            // 모든 노드를 반복하며 체크박스를 해제
            foreach (TreeNode node in nodes)
            {
                // 현재 노드의 체크박스를 해제
                node.Checked = false;

                // 자식 노드가 있는 경우 재귀적으로 호출
                if (node.Nodes.Count > 0)
                {
                    UncheckAllNodes(node.Nodes);
                }
            }
        }

        private void CollectElementsToDelete(TreeNode node, List<Element> elementsToDelete)
        {
            // 하위 노드를 재귀적으로 탐색하여 삭제할 요소를 수집
            foreach (TreeNode childNode in node.Nodes)
            {
                // 노드가 Element인 경우
                if (childNode.Tag is Element element && childNode.Checked)
                {
                    elementsToDelete.Add(element);
                }

                // 하위 노드가 있는 경우 재귀적으로 탐색
                if (childNode.Nodes.Count > 0)
                {
                    CollectElementsToDelete(childNode, elementsToDelete);
                }
            }
        }

        private void UpdateTreeView(List<Element> deletedElements)
        {
            foreach (var deletedElement in deletedElements)
            {
                // 각 삭제된 요소가 속한 노드를 찾아서 삭제
                TreeNode categoryNode = null;
                foreach (var kvp in _categoryElementsMap)
                {
                    if (kvp.Value.Contains(deletedElement))
                    {
                        categoryNode = kvp.Key;
                        kvp.Value.Remove(deletedElement);  // 삭제된 요소 제거
                        break;
                    }
                }

                // 해당 카테고리의 요소 개수를 업데이트
                if (categoryNode != null)
                {
                    int remainingCount = _categoryElementsMap[categoryNode].Count;
                    categoryNode.Text = $"{categoryNode.Text.Split('(')[0].Trim()} ({remainingCount})";

                    // 하위 노드들도 삭제된 요소에 맞게 업데이트
                    RemoveDeletedNodes(categoryNode.Nodes, deletedElements);
                }
            }
        }

        private void UpdateChildNodeCounts(TreeNode parentNode)
        {
            foreach (TreeNode childNode in parentNode.Nodes)
            {
                // 하위 노드의 카운트를 0으로 설정
                childNode.Text = $"{childNode.Text.Split('(')[0].Trim()} (0)";

                // 재귀적으로 모든 하위 노드를 탐색하여 업데이트
                UpdateChildNodeCounts(childNode);
            }
        }

        private void RemoveDeletedNodes(TreeNodeCollection nodes, List<Element> deletedElements)
        {
            List<TreeNode> nodesToRemove = new List<TreeNode>();

            foreach (TreeNode node in nodes)
            {
                if (node.Tag is Element element && deletedElements.Contains(element))
                {
                    nodesToRemove.Add(node);
                }
                else if (node.Nodes.Count > 0)
                {
                    RemoveDeletedNodes(node.Nodes, deletedElements);

                    // ✅ 하위 노드 제거 후 자식이 없으면 자신도 제거 대상
                    if (node.Nodes.Count == 0 && node.Tag == null)
                    {
                        nodesToRemove.Add(node);
                    }
                }
            }

            foreach (TreeNode node in nodesToRemove)
            {
                nodes.Remove(node);
            }
        }

        private void RemoveDeletedNodesRecursive(TreeNode parentNode, List<Element> deletedElements)
        {
            List<TreeNode> nodesToRemove = new List<TreeNode>();

            foreach (TreeNode node in parentNode.Nodes)
            {
                if (node.Tag is Element element && deletedElements.Contains(element))
                {
                    nodesToRemove.Add(node);
                }
                else
                {
                    RemoveDeletedNodesRecursive(node, deletedElements); // 재귀적으로 하위 노드 탐색
                }
            }

            foreach (TreeNode nodeToRemove in nodesToRemove)
            {
                parentNode.Nodes.Remove(nodeToRemove);
            }
        }

        private void UpdateCategoryNodeCount()
        {
            foreach (TreeNode categoryNode in treeView1.Nodes)
            {
                int totalElementsInCategory = 0;

                foreach (TreeNode familyNode in categoryNode.Nodes)
                {
                    int totalElementsInFamily = 0;

                    foreach (TreeNode typeNode in familyNode.Nodes)
                    {
                        totalElementsInFamily += typeNode.Nodes.Count;  // 타입 노드 아래의 모든 요소 수 합계
                    }

                    familyNode.Text = $"{familyNode.Text.Split('(')[0].Trim()} ({totalElementsInFamily})";
                    totalElementsInCategory += totalElementsInFamily;
                }

                categoryNode.Text = $"{categoryNode.Text.Split('(')[0].Trim()} ({totalElementsInCategory})";
            }
        }

        private void UpdateTreeNodes(TreeNodeCollection nodes)
        {
            List<TreeNode> nodesToRemove = new List<TreeNode>();

            foreach (TreeNode node in nodes)
            {
                if (node.Checked)
                {
                    nodesToRemove.Add(node);
                }
                else if (node.Nodes.Count > 0)
                {
                    UpdateTreeNodes(node.Nodes);
                }
            }

            foreach (TreeNode nodeToRemove in nodesToRemove)
            {
                nodes.Remove(nodeToRemove);
            }
        }

        private IEnumerable<TreeNode> GetCheckedNodes(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Checked)
                {
                    yield return node;
                }

                foreach (TreeNode childNode in GetCheckedNodes(node.Nodes))
                {
                    yield return childNode;
                }
            }
        }

        private List<ElementId> GetChildElementIds(TreeNode parentNode)
        {
            List<ElementId> elementIds = new List<ElementId>();
            foreach (TreeNode node in parentNode.Nodes)
            {
                if (node.Tag is Element element)
                {
                    elementIds.Add(element.Id);
                }
                // 재귀적으로 자식 노드들도 탐색
                elementIds.AddRange(GetChildElementIds(node));
            }
            return elementIds;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // 필요한 경우 트리뷰 선택 이벤트를 처리합니다.
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                _radioButton1Selected = true;
                radioButton2.Checked = false;

                // TreeView 업데이트를 위해 기존 노드를 모두 제거하고 다시 로드
                treeView1.Nodes.Clear();
                LoadTreeView();
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                _radioButton1Selected = false;
                radioButton1.Checked = false;

                // TreeView 업데이트를 위해 기존 노드를 모두 제거하고 선택된 요소만 로드
                treeView1.Nodes.Clear();
                LoadSelectedElementsTreeView();
            }
        }

        private void LoadSelectedElementsTreeView()
        {
            UIDocument uiDoc = new UIDocument(_doc);
            ICollection<ElementId> selectedElementIds = uiDoc.Selection.GetElementIds();
            List<Element> selectedElements = selectedElementIds.Select(id => _doc.GetElement(id)).ToList();

            // 카테고리 트리뷰를 위한 노드 생성
            TreeNode categoriesNode = new TreeNode("Categories");
            treeView1.Nodes.Add(categoriesNode);

            // selectedElements를 LoadCategoryNodes로 전달
            LoadCategoryNodes(categoriesNode, selectedElements);
        }

        private void LoadTreeView()
        {
            treeView1.CheckBoxes = true;

            // "Categories" 최상위 노드
            TreeNode categoriesNode = new TreeNode("Categories");
            treeView1.Nodes.Add(categoriesNode);

            // 현재 뷰에 보이는 요소들만 수집
            FilteredElementCollector collector = new FilteredElementCollector(_doc, _doc.ActiveView.Id);
            List<Element> elements = collector.WhereElementIsNotElementType().ToList();

            // 카테고리 데이터를 로드 (수집한 elements를 전달)
            LoadCategoryNodes(categoriesNode, elements);

            // "Workset" 최상위 노드
            TreeNode worksetNode = new TreeNode("Worksets");
            treeView1.Nodes.Add(worksetNode);

            // 현재 뷰에 보이는 Workset에 속하는 요소들만 수집
            LoadWorksetNodes(worksetNode, elements);  // elements 리스트를 전달

            categoriesNode.Expand();
            worksetNode.Expand();
        }

        private void LoadCategoryNodes(TreeNode parentNode, List<Element> elements)
        {
            // 일반 카테고리별 요소 저장
            Dictionary<string, Dictionary<string, Dictionary<string, List<Element>>>> categoryFamilyTypeElements =
                new Dictionary<string, Dictionary<string, Dictionary<string, List<Element>>>>();

            // Parts 전용 저장 (Original Category 기준으로 묶기 위해)
            Dictionary<string, List<Element>> partsElements = new Dictionary<string, List<Element>>();

            foreach (var element in elements)
            {
                Category category = element.Category;
                if (category != null)
                {
                    // 🔹 Original Category 가져오기
                    string originalCategoryName = "Unknown";
                    Parameter originalCategoryParam = element.get_Parameter(BuiltInParameter.DPART_ORIGINAL_CATEGORY_ID);
                    if (originalCategoryParam != null)
                    {
                        ElementId originalCategoryId = originalCategoryParam.AsElementId();
                        if (originalCategoryId != ElementId.InvalidElementId)
                        {
                            foreach (Category cat in _doc.Settings.Categories)
                            {
                                if (cat.Id == originalCategoryId)
                                {
                                    originalCategoryName = cat.Name;
                                    break;
                                }
                            }
                        }
                    }

                    // Family 및 Type 정보 가져오기
                    ElementType elementType = _doc.GetElement(element.GetTypeId()) as ElementType;
                    string familyName = "Unknown Family";
                    string typeName = "Unknown Type";
                    string categoryName = category.Name; // 기존 Category.Name 사용

                    if (element is FamilyInstance familyInstance)
                    {
                        familyName = familyInstance.Symbol.Family.Name;
                        typeName = familyInstance.Symbol.Name;
                    }
                    else if (elementType != null)
                    {
                        familyName = elementType.FamilyName ?? "Unknown Family";
                        typeName = elementType.Name ?? "Unknown Type";
                    }
                    else
                    {
                        familyName = element.Name;
                        typeName = element.Name;
                    }

                    // 🔹 Parts 요소라면 별도 저장 (해당 카테고리의 하위로 추가하지 않음)
                    if (category.Name == "Parts")
                    {
                        if (!partsElements.ContainsKey(originalCategoryName))
                            partsElements[originalCategoryName] = new List<Element>();

                        partsElements[originalCategoryName].Add(element);
                        continue; // 기존 카테고리 저장 로직을 건너뜀
                    }

                    // 일반 카테고리 저장
                    if (!categoryFamilyTypeElements.ContainsKey(categoryName))
                        categoryFamilyTypeElements[categoryName] = new Dictionary<string, Dictionary<string, List<Element>>>();

                    if (!categoryFamilyTypeElements[categoryName].ContainsKey(familyName))
                        categoryFamilyTypeElements[categoryName][familyName] = new Dictionary<string, List<Element>>();

                    if (!categoryFamilyTypeElements[categoryName][familyName].ContainsKey(typeName))
                        categoryFamilyTypeElements[categoryName][familyName][typeName] = new List<Element>();

                    categoryFamilyTypeElements[categoryName][familyName][typeName].Add(element);
                }
            }

            // 🔹 일반 카테고리 트리뷰 노드 추가 (Category & Material 정보 없이)
            foreach (var categoryElement in categoryFamilyTypeElements)
            {
                string categoryName = categoryElement.Key;
                TreeNode categoryNode = new TreeNode($"{categoryName} ({categoryElement.Value.Sum(f => f.Value.Sum(t => t.Value.Count))})");
                parentNode.Nodes.Add(categoryNode);

                foreach (var familyElement in categoryElement.Value)
                {
                    string familyName = familyElement.Key;
                    TreeNode familyNode = new TreeNode($"{familyName} ({familyElement.Value.Sum(t => t.Value.Count)})");
                    categoryNode.Nodes.Add(familyNode);

                    foreach (var typeElement in familyElement.Value)
                    {
                        string typeName = typeElement.Key;
                        List<Element> elementsInType = typeElement.Value;

                        TreeNode typeNode = new TreeNode($"{typeName} ({elementsInType.Count})");

                        familyNode.Nodes.Add(typeNode);

                        foreach (var element in elementsInType)
                        {
                            string elementId = element.Id.ToString();
                            TreeNode elementNode = new TreeNode($"ID: {elementId}") // ❌ Category & Material 정보 제거
                            {
                                Tag = element
                            };
                            typeNode.Nodes.Add(elementNode);
                        }
                    }
                }
            }

            // 🔹 Parts 전용 트리뷰 노드 추가 (Category & Material 정보 포함)
            if (partsElements.Count > 0)
            {
                TreeNode partsNode = new TreeNode($"Parts ({partsElements.Sum(kv => kv.Value.Count)})");
                parentNode.Nodes.Add(partsNode);

                foreach (var partCategory in partsElements)
                {
                    string partCategoryName = partCategory.Key;
                    TreeNode partCategoryNode = new TreeNode($"{partCategoryName} ({partCategory.Value.Count})");
                    partsNode.Nodes.Add(partCategoryNode);

                    foreach (var element in partCategory.Value)
                    {
                        string elementId = element.Id.ToString();
                        string material = element.LookupParameter("Material")?.AsValueString() ?? "N/A";

                        // ✅ Parts 요소만 (Category & Material) 추가
                        TreeNode elementNode = new TreeNode($"ID: {elementId}, Material: {material})")
                        {
                            Tag = element
                        };
                        partCategoryNode.Nodes.Add(elementNode);
                    }
                }
            }

            parentNode.Expand();
        }

        private void LoadWorksetNodes(TreeNode worksetNode, List<Element> elements)
        {
            // Workset별로 요소 저장
            Dictionary<string, Dictionary<string, Dictionary<string, List<Element>>>> worksetCategoryFamilyTypeElements =
                new Dictionary<string, Dictionary<string, Dictionary<string, List<Element>>>>();

            // Workset별 Parts 전용 저장 (Original Category 기준으로 묶기 위해)
            Dictionary<string, Dictionary<string, List<Element>>> worksetPartsElements =
                new Dictionary<string, Dictionary<string, List<Element>>>();

            foreach (var element in elements)
            {
                // 🔹 1. View 관련 요소 완전히 제거
                if (element is Autodesk.Revit.DB.View ||
                    element is ViewSection ||
                    element is View3D ||
                    element is ViewPlan ||
                    element is ViewSheet ||
                    element is ViewDrafting ||
                    element is ViewSchedule)
                {
                    continue;
                }

                // 🔹 2. BuiltInCategory 기반 필터링 (OST_Views, OST_Sheets)
                var viewCatId = new ElementId(BuiltInCategory.OST_Views);
                var sheetCatId = new ElementId(BuiltInCategory.OST_Sheets);
                if (element.Category?.Id == viewCatId || element.Category?.Id == sheetCatId)
                {
                    continue;
                }

                // 🔹 3. Unknown Category 필터링
                if (element.Category == null || element.Category.Name == "Unknown Category")
                {
                    continue;
                }

                // 🔹 4. 특정 카테고리 필터링
                if (element.Category?.Name != null &&
                    (element.Category.Name.Contains("View") ||
                     element.Category.Name.Contains("Elevation") ||
                     element.Category.Name.Contains("Section") ||
                     element.Category.Name.Contains("3D") ||
                     element.Category.Name.Contains("Sheet") ||
                     element.Category.Name.Contains("Grid") ||
                     element.Category.Name.Contains("Stair Path") ||
                     element.Category.Name.Contains("dwg") ||
                     element.Category.Name.Contains("Level") ||
                     element.Category.Name.Contains("Camera") ||
                     element.Category.Name.Contains("Dimension") ||
                     element.Category.Name.Contains("Unknown Family") ||
                     element.Category.Name.Contains("Plan")))
                {
                    continue;
                }

                // 활성 뷰에 해당하는 요소만 필터링
                if (element.OwnerViewId != ElementId.InvalidElementId && element.OwnerViewId != _doc.ActiveView.Id)
                {
                    continue;
                }

                WorksetId worksetId = element.WorksetId;
                if (worksetId != null)
                {
                    Workset workset = _doc.GetWorksetTable().GetWorkset(worksetId);
                    string worksetName = workset.Name;
                    Category category = element.Category;
                    string categoryName = category?.Name ?? "Unknown Category";

                    // 🔹 Original Category 가져오기
                    string originalCategoryName = "Unknown";
                    Parameter originalCategoryParam = element.get_Parameter(BuiltInParameter.DPART_ORIGINAL_CATEGORY_ID);
                    if (originalCategoryParam != null)
                    {
                        ElementId originalCategoryId = originalCategoryParam.AsElementId();
                        if (originalCategoryId != ElementId.InvalidElementId)
                        {
                            foreach (Category cat in _doc.Settings.Categories)
                            {
                                if (cat.Id == originalCategoryId)
                                {
                                    originalCategoryName = cat.Name;
                                    break;
                                }
                            }
                        }
                    }

                    // Family 및 Type 정보 가져오기
                    ElementType elementType = _doc.GetElement(element.GetTypeId()) as ElementType;
                    string familyName = "Unknown Family";
                    string typeName = "Unknown Type";

                    if (element is FamilyInstance familyInstance)
                    {
                        familyName = familyInstance.Symbol.Family.Name;
                        typeName = familyInstance.Symbol.Name;
                    }
                    else if (elementType != null)
                    {
                        familyName = elementType.FamilyName ?? "Unknown Family";
                        typeName = elementType.Name ?? "Unknown Type";
                    }
                    else
                    {
                        familyName = element.Name;
                        typeName = element.Name;
                    }

                    // 🔹 Parts 요소라면 Workset별로 별도 저장
                    if (categoryName == "Parts")
                    {
                        if (!worksetPartsElements.ContainsKey(worksetName))
                            worksetPartsElements[worksetName] = new Dictionary<string, List<Element>>();

                        if (!worksetPartsElements[worksetName].ContainsKey(originalCategoryName))
                            worksetPartsElements[worksetName][originalCategoryName] = new List<Element>();

                        worksetPartsElements[worksetName][originalCategoryName].Add(element);
                        continue; // 기존 Workset 저장 로직을 건너뜀
                    }

                    // 일반 Workset 요소 저장
                    if (!worksetCategoryFamilyTypeElements.ContainsKey(worksetName))
                        worksetCategoryFamilyTypeElements[worksetName] = new Dictionary<string, Dictionary<string, List<Element>>>();

                    if (!worksetCategoryFamilyTypeElements[worksetName].ContainsKey(categoryName))
                        worksetCategoryFamilyTypeElements[worksetName][categoryName] = new Dictionary<string, List<Element>>();

                    if (!worksetCategoryFamilyTypeElements[worksetName][categoryName].ContainsKey(familyName))
                        worksetCategoryFamilyTypeElements[worksetName][categoryName][familyName] = new List<Element>();

                    worksetCategoryFamilyTypeElements[worksetName][categoryName][familyName].Add(element);
                }
            }

            // 🔹 일반 Workset 요소 트리뷰 노드 추가
            foreach (var worksetElement in worksetCategoryFamilyTypeElements)
            {
                string worksetName = worksetElement.Key;
                TreeNode worksetNodeItem = new TreeNode($"{worksetName} ({worksetElement.Value.Sum(c => c.Value.Sum(f => f.Value.Count))})");
                worksetNode.Nodes.Add(worksetNodeItem);

                foreach (var categoryElement in worksetElement.Value)
                {
                    string categoryName = categoryElement.Key;
                    TreeNode categoryNode = new TreeNode($"{categoryName} ({categoryElement.Value.Sum(f => f.Value.Count)})");
                    worksetNodeItem.Nodes.Add(categoryNode);

                    foreach (var familyElement in categoryElement.Value)
                    {
                        string familyName = familyElement.Key;
                        TreeNode familyNode = new TreeNode($"{familyName} ({familyElement.Value.Count})");
                        categoryNode.Nodes.Add(familyNode);

                        foreach (var element in familyElement.Value)
                        {
                            string elementId = element.Id.ToString();
                            TreeNode elementNode = new TreeNode($"ID: {elementId}") // ❌ Category & Material 정보 제거
                            {
                                Tag = element
                            };
                            familyNode.Nodes.Add(elementNode);
                        }
                    }
                }

                // 🔹 해당 Workset의 Parts 추가
                if (worksetPartsElements.ContainsKey(worksetName))
                {
                    TreeNode partsNode = new TreeNode($"Parts ({worksetPartsElements[worksetName].Sum(kv => kv.Value.Count)})");
                    worksetNodeItem.Nodes.Add(partsNode);

                    foreach (var partCategory in worksetPartsElements[worksetName])
                    {
                        string partCategoryName = partCategory.Key;
                        TreeNode partCategoryNode = new TreeNode($"{partCategoryName} ({partCategory.Value.Count})");
                        partsNode.Nodes.Add(partCategoryNode);

                        foreach (var element in partCategory.Value)
                        {
                            string elementId = element.Id.ToString();
                            string material = element.LookupParameter("Material")?.AsValueString() ?? "N/A";

                            // ✅ Parts 요소만 (Category & Material) 추가
                            TreeNode elementNode = new TreeNode($"ID: {elementId} (Category: {partCategoryName}, Material: {material})")
                            {
                                Tag = element
                            };
                            partCategoryNode.Nodes.Add(elementNode);
                        }
                    }
                }
            }

            worksetNode.Expand();
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // 이벤트 중첩 방지
            treeView1.AfterCheck -= treeView1_AfterCheck;

            try
            {
                // 하위 노드 체크 상태 설정
                SetChildNodesCheckState(e.Node, e.Node.Checked);

                // 상위 노드 체크 상태 설정 (선택 사항)
                //SetParentNodesCheckState(e.Node, e.Node.Checked);
            }
            finally
            {
                // 이벤트 다시 등록
                treeView1.AfterCheck += treeView1_AfterCheck;
            }
        }

        private void SetChildNodesCheckState(TreeNode parentNode, bool isChecked)
        {
            foreach (TreeNode childNode in parentNode.Nodes)
            {
                childNode.Checked = isChecked;
                SetChildNodesCheckState(childNode, isChecked);  // 하위 노드도 동일하게 처리
            }
        }

        private void SetParentNodesCheckState(TreeNode childNode, bool isChecked)
        {
            if (childNode.Parent != null)
            {
                childNode.Parent.Checked = isChecked;
                SetParentNodesCheckState(childNode.Parent, isChecked);  // 상위 노드도 동일하게 처리
            }
        }

        private void UpdateSelectedCountLabel()
        {
            int count = treeView1
                .Nodes
                .Cast<TreeNode>()
                .SelectMany(GetCheckedNodesRecursive)
                .Count();

            label2.Text = $"선택된 항목 수: \n{count}";
        }

        public void UpdateLabel2Count()
        {
            int count = treeView1
                .Nodes
                .Cast<TreeNode>()
                .SelectMany(GetCheckedNodesRecursive)
                .Count();

            label2.Text = $"선택된 항목 수: \n{count}";
        }

        private IEnumerable<TreeNode> GetCheckedNodesRecursive(TreeNode node)
        {
            // ✅ 최하위 노드만 포함
            if (node.Checked && node.Nodes.Count == 0)
                yield return node;

            foreach (TreeNode child in node.Nodes)
            {
                foreach (var checkedChild in GetCheckedNodesRecursive(child))
                    yield return checkedChild;
            }
        }

        private IEnumerable<TreeNode> GetCheckedNodesRecursive(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                foreach (var n in GetCheckedNodesRecursive(node))
                    yield return n;
            }
        }

        private void CollectCheckedLeafElementIds(TreeNodeCollection nodes, List<ElementId> result)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Checked && node.Nodes.Count == 0)
                {
                    if (node.Tag is Element el)
                    {
                        result.Add(el.Id);
                        Debug.WriteLine($"✔ 수집된 ID: {el.Id}");
                    }
                    else
                    {
                        Debug.WriteLine($"❌ Tag에 Element가 없음 → {node.Text}");
                    }
                }

                if (node.Nodes.Count > 0)
                    CollectCheckedLeafElementIds(node.Nodes, result);
            }
        }





        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>


        // Shift 키 누름 감지
        private void TreeView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey)
                _shiftPressed = true;
        }

        // Shift 키 떼면 해제
        private void TreeView1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey)
                _shiftPressed = false;
        }

        // 노드 클릭 시 다중 선택 기능
        private void TreeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            treeView1.SelectedNode = e.Node;

            // ✅ 클릭할 때마다 항상 모든 기존 선택 노드 배경색 초기화
            foreach (TreeNode node in _selectedNodes)
            {
                node.BackColor = System.Drawing.Color.Empty;
            }

            if (_shiftPressed && _lastClickedNode != null && e.Node.Parent == _lastClickedNode.Parent)
            {
                // ✅ Shift 누른 상태 → 범위 선택
                TreeNodeCollection siblings = e.Node.Parent?.Nodes ?? treeView1.Nodes;

                int start = siblings.IndexOf(_lastClickedNode);
                int end = siblings.IndexOf(e.Node);

                int min = Math.Min(start, end);
                int max = Math.Max(start, end);

                _selectedNodes.Clear();
                for (int i = min; i <= max; i++)
                {
                    _selectedNodes.Add(siblings[i]);
                }

                // ✅ Shift로 선택한 경우에만 LightGray 적용
                foreach (TreeNode node in _selectedNodes)
                {
                    node.BackColor = System.Drawing.Color.LightGray;
                }
            }
            else
            {
                // ✅ 일반 클릭은 배경색 없이 단일 선택만
                _selectedNodes.Clear();
                _selectedNodes.Add(e.Node);
                _lastClickedNode = e.Node;
                // 배경색 지정 없음 (아무것도 칠하지 않음)
            }
        }

        // 체크박스 동기화
        private void TreeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            treeView1.AfterCheck -= TreeView1_AfterCheck;

            try
            {
                if (_selectedNodes.Count > 1)
                {
                    foreach (TreeNode node in _selectedNodes)
                    {
                        if (node != e.Node)
                            node.Checked = e.Node.Checked;
                    }
                }

                // ✅ 체크 상태 바뀔 때마다 카운트 갱신
                UpdateSelectedCountLabel();
            }
            finally
            {
                treeView1.AfterCheck += TreeView1_AfterCheck;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>


        private void LoadSurfacePatternsAndColors()
        {
            comboBox1.Items.Clear();

            // ✅ "색 없음" 포함
            string[] colorNames = { "색 없음", "Red", "Green", "Blue", "Yellow", "Gray", "Black", "White", "Cyan", "Magenta", "Orange" };
            comboBox1.Items.AddRange(colorNames);

            comboBox1.DrawMode = DrawMode.OwnerDrawFixed;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;

            comboBox1.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                if (e.Index < 0) return;

                string colorName = comboBox1.Items[e.Index].ToString();

                if (colorName == "색 없음")
                {
                    // ✅ 회색 글씨로 "<색 없음>" 표시
                    e.Graphics.DrawString("<색 없음>", e.Font, Brushes.Gray, e.Bounds.Left + 2, e.Bounds.Top + 2);
                }
                else
                {
                    Color color = Color.FromName(colorName);
                    using (SolidBrush brush = new SolidBrush(color))
                    {
                        e.Graphics.FillRectangle(brush, e.Bounds.Left + 2, e.Bounds.Top + 2, 20, e.Bounds.Height - 4);
                    }

                    e.Graphics.DrawString(colorName, e.Font, Brushes.Black, e.Bounds.Left + 25, e.Bounds.Top + 2);
                }
            };

            comboBox1.SelectedIndex = 0; // 기본값: 색 없음
        }

        public class ComboBoxItem
        {
            public string Text { get; set; }
            public object Tag { get; set; }

            public override string ToString()
            {
                return Text; // ComboBox에 표시될 항목 이름
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null) return;

            string colorName = comboBox1.SelectedItem.ToString();

            if (colorName == "색 없음")
            {
                _graphicsHandler.IsResetColor = true;
            }
            else
            {
                _graphicsHandler.IsResetColor = false;
                System.Drawing.Color sysColor = System.Drawing.Color.FromName(colorName);
                _graphicsHandler.ColorToApply = new Autodesk.Revit.DB.Color(sysColor.R, sysColor.G, sysColor.B);
            }

            _graphicsHandler.Doc = _doc;
            _graphicsHandler.View = _doc.ActiveView;
            _graphicsHandler.TargetElementIds = GetCheckedElementIds(); // 기존 메소드

            _graphicsEvent.Raise();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            int transparency = trackBar1.Value; // 그대로 사용: 0~100

            _graphicsHandler.TransparencyValue = transparency;
            _graphicsHandler.IsResetColor = false;

            _graphicsHandler.Doc = _doc;
            _graphicsHandler.View = _doc.ActiveView;
            _graphicsHandler.TargetElementIds = GetCheckedElementIds();

            _graphicsEvent.Raise(); // 실시간 반영

        }

        private void LoadSelectedElementsTreeView(ICollection<ElementId> selectedElementIds)
        {
            List<Element> selectedElements = selectedElementIds
                .Select(id => _doc.GetElement(id))
                .Where(e => e != null)
                .ToList();

            TreeNode categoriesNode = new TreeNode("Categories");
            treeView1.Nodes.Add(categoriesNode);

            LoadCategoryNodes(categoriesNode, selectedElements);
        }


    }
}
