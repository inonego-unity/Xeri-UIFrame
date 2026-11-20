/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterCompositionPage.cs
수정일 : 2026-11-17

# 설명
레이아웃 에셋의 레이어·배치 표와, 로드된 씬의 Runtime·Host 구성을 목록·상세 화면으로 다룬다.

# 특이사항, 제약사항
레이어·배치는 Xeri Core Collection Editor의 필드 열과 채움 모드로 편집한다.
씬 조회는 읽기 전용이며 미로드 씬을 열지 않는다. 생성형 레이어의 Host 부재는 오류로 보지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.UI.Frame.Editor
{
    internal sealed class ControlCenterCompositionPage : ControlCenterPage
    {

    #region 내부 데이터

        private sealed class SceneNode
        {
            internal string Title;
            internal ControlCenterSceneEntry Entry;
        }

    #endregion

    #region 필드

        private readonly ToolbarToggle layoutsToggle;
        private readonly ToolbarToggle scenesToggle;

        private readonly VisualElement layoutSplit;
        private readonly ToolbarSearchField layoutSearch;
        private readonly ListView layoutList;
        private readonly Label layoutListEmpty;
        private readonly Label layoutTitle;
        private readonly Label layoutMeta;
        private readonly Button layoutReveal;
        private const float placementTableRatio = 0.42f;
        private readonly TwoPaneSplitView layoutTables;
        private bool tablesRatioApplied;
        private readonly VisualElement layersHost;
        private readonly VisualElement placementsHost;
        private readonly VisualElement layoutEmpty;
        private readonly List<PresentationLayout> visibleLayouts = new();
        private SerializedObject layoutObject;

        private readonly VisualElement sceneSplit;
        private readonly Label sceneCount;
        private readonly TreeView sceneTree;
        private readonly Label sceneListEmpty;
        private readonly Label sceneTitle;
        private readonly Label sceneMeta;
        private readonly Button sceneReveal;
        private readonly ScrollView sceneBody;
        private ControlCenterSceneEntry selectedScene;
        private VisualElement sceneRows;
        private Component inspectedComponent;

        internal override ControlCenterTab Tab => ControlCenterTab.Composition;
        internal override string Title => "구성";
        internal override string IconName => "layers";

    #endregion

    #region 생성자

        internal ControlCenterCompositionPage(ControlCenterContext context)
            : base(context, "Composition")
        {
            layoutsToggle = ControlCenterElements.Require<ToolbarToggle>(Root, "composition-layouts");
            scenesToggle = ControlCenterElements.Require<ToolbarToggle>(Root, "composition-scenes");
            layoutsToggle.RegisterValueChangedCallback(_ => ShowScenes(false));
            scenesToggle.RegisterValueChangedCallback(_ => ShowScenes(true));

            layoutSplit = RegisterResponsiveSplit(ControlCenterElements.Require<TwoPaneSplitView>(Root, "layout-split"));
            layoutSearch = ControlCenterElements.Require<ToolbarSearchField>(Root, "layout-search");
            layoutList = ControlCenterElements.Require<ListView>(Root, "layout-list");
            layoutListEmpty = ControlCenterElements.Require<Label>(Root, "layout-list-empty");
            layoutTitle = ControlCenterElements.Require<Label>(Root, "layout-title");
            layoutMeta = ControlCenterElements.Require<Label>(Root, "layout-meta");
            layoutReveal = ControlCenterElements.Require<Button>(Root, "layout-reveal");
            layoutTables = ControlCenterElements.Require<TwoPaneSplitView>(Root, "layout-tables");
            layoutTables.RegisterCallback<GeometryChangedEvent>(HandleTablesGeometryChanged);
            layersHost = ControlCenterElements.Require<VisualElement>(Root, "layout-layers");
            placementsHost = ControlCenterElements.Require<VisualElement>(Root, "layout-placements");
            layoutEmpty = ControlCenterElements.Require<VisualElement>(Root, "layout-empty");
            ConfigureLayoutList();

            sceneSplit = RegisterResponsiveSplit(ControlCenterElements.Require<TwoPaneSplitView>(Root, "scene-split"));
            sceneCount = ControlCenterElements.Require<Label>(Root, "scene-count");
            sceneTree = ControlCenterElements.Require<TreeView>(Root, "scene-tree");
            sceneListEmpty = ControlCenterElements.Require<Label>(Root, "scene-list-empty");
            sceneTitle = ControlCenterElements.Require<Label>(Root, "scene-title");
            sceneMeta = ControlCenterElements.Require<Label>(Root, "scene-meta");
            sceneReveal = ControlCenterElements.Require<Button>(Root, "scene-reveal");
            sceneBody = ControlCenterElements.Require<ScrollView>(Root, "scene-body");
            ConfigureSceneTree();

            Context.SceneEntryRequested += SelectSceneEntry;
        }

    #endregion

    #region 표시

        protected override void Refresh(ControlCenterChange change)
        {
            var showScenes = Context.State.ShowScenes;
            layoutsToggle.SetValueWithoutNotify(!showScenes);
            scenesToggle.SetValueWithoutNotify(showScenes);
            layoutSplit.style.display = showScenes ? DisplayStyle.None : DisplayStyle.Flex;
            sceneSplit.style.display = showScenes ? DisplayStyle.Flex : DisplayStyle.None;

            if ((change & (ControlCenterChange.Catalog | ControlCenterChange.Selection)) != 0)
            {
                RefreshLayoutList();
                RenderLayout();
            }

            if ((change & (ControlCenterChange.Scenes | ControlCenterChange.PlayMode)) != 0)
            {
                RefreshSceneTree();
            }
        }

        private void ShowScenes(bool value)
        {
            Context.State.ShowScenes = value;
            Refresh(ControlCenterChange.None);
        }

    #endregion

    #region 레이아웃

        private void ConfigureLayoutList()
        {
            var create = ControlCenterElements.Require<ToolbarButton>(Root, "layout-create");
            ControlCenterElements.SetIcon(create, "plus");
            create.clicked += Context.CreateLayout;
            layoutSearch.RegisterValueChangedCallback(_ => RefreshLayoutList());
            layoutReveal.clicked += () => ControlCenterContext.Reveal(Context.Layout);

            layoutList.itemsSource = visibleLayouts;
            layoutList.makeItem = MakeLayoutItem;
            layoutList.bindItem = BindLayoutItem;
            layoutList.selectionChanged += items =>
            {
                foreach (var item in items)
                {
                    Context.SelectLayout(item as PresentationLayout);
                    return;
                }
            };
        }

        private static VisualElement MakeLayoutItem()
        {
            // 첫 줄은 이름과 표식, 둘째 줄은 구성 수와 위치를 표시한다.
            var item = new VisualElement();
            item.AddToClassList("xeri-cc__list-item");
            item.AddToClassList("xeri-cc__list-item--two-line");

            var heading = new VisualElement();
            heading.AddToClassList("xeri-cc__list-item-heading");
            item.Add(heading);

            var name = new Label
            {
                name = "name"
            };
            name.AddToClassList("xeri-cc__list-item-name");
            heading.Add(name);

            var badges = new VisualElement
            {
                name = "badges"
            };
            badges.AddToClassList("xeri-cc__list-item-badges");
            heading.Add(badges);

            var meta = new Label
            {
                name = "meta"
            };
            meta.AddToClassList("xeri-cc__list-item-detail");
            item.Add(meta);
            return item;
        }

        private void BindLayoutItem(VisualElement element, int index)
        {
            var layout = visibleLayouts[index];
            element.Q<Label>("name").text = layout.name;
            element.Q<Label>("meta").text = "레이어 " + layout.Layers.Count + " · 배치 " + layout.Placements.Count
                + " · " + ControlCenterAssets.DescribeLocation(layout);
            element.tooltip = AssetDatabase.GetAssetPath(layout);

            var badges = element.Q<VisualElement>("badges");
            badges.Clear();
            if (IsDefaultLayout(layout))
            {
                badges.Add(ControlCenterElements.CreateBadge("기본", accent: true));
            }

            if (ControlCenterAssets.IsPackageAsset(layout))
            {
                badges.Add(ControlCenterElements.CreateBadge("패키지"));
            }
        }

        private bool IsDefaultLayout(PresentationLayout layout)
        {
            foreach (var settings in Context.SettingsAssets)
            {
                if (settings != null && settings.DefaultLayout == layout)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshLayoutList()
        {
            visibleLayouts.Clear();
            var filter = layoutSearch.value;
            foreach (var layout in Context.Layouts)
            {
                if (string.IsNullOrWhiteSpace(filter)
                    || layout.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    visibleLayouts.Add(layout);
                }
            }

            layoutList.RefreshItems();
            var index = visibleLayouts.IndexOf(Context.Layout);
            var selectedIndexes = new List<int>();
            if (index >= 0)
            {
                selectedIndexes.Add(index);
            }

            layoutList.SetSelectionWithoutNotify(selectedIndexes);
            layoutListEmpty.text = Context.Layouts.Count == 0 ? "레이아웃 에셋이 없습니다." : "검색 결과가 없습니다.";
            layoutListEmpty.style.display = visibleLayouts.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 레이아웃이 바뀐 경우에만 레이어·배치 표를 다시 바인딩한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RenderLayout()
        {
            var layout = Context.Layout;
            layoutReveal.SetEnabled(layout != null);
            if (layout == null)
            {
                ClearLayoutTables();
                layoutTitle.text = "레이아웃 선택 안 됨";
                layoutMeta.text = string.Empty;
                layoutTables.style.display = DisplayStyle.None;
                layoutEmpty.style.display = DisplayStyle.Flex;
                layoutEmpty.Clear();
                layoutEmpty.Add
                (
                    ControlCenterElements.CreateNotice
                    (
                        Context.Layouts.Count == 0 ? "프로젝트에 레이아웃 에셋이 없습니다." : "목록에서 레이아웃을 선택하세요.",
                        ("새 레이아웃 만들기", Context.CreateLayout)
                    )
                );
                return;
            }

            layoutTitle.text = layout.name;
            layoutMeta.text = DescribeLayoutUsage(layout);
            layoutMeta.tooltip = AssetDatabase.GetAssetPath(layout);
            layoutTables.style.display = DisplayStyle.Flex;
            layoutEmpty.style.display = DisplayStyle.None;
            if (layoutObject != null && layoutObject.targetObject == layout)
            {
                return;
            }

            ClearLayoutTables();
            layoutObject = new SerializedObject(layout);
            ControlCenterElements.AddProperty(layersHost, layoutObject, "layers", "레이어");
            ControlCenterElements.AddProperty(placementsHost, layoutObject, "placements", "배치");
        }

        private string DescribeLayoutUsage(PresentationLayout layout)
        {
            var users = new List<string>();
            foreach (var settings in Context.SettingsAssets)
            {
                if (settings != null && settings.DefaultLayout == layout)
                {
                    users.Add(settings.name);
                }
            }

            var usage = users.Count > 0 ? "기본 레이아웃: " + string.Join(", ", users) : "설정 참조 없음";
            return usage + " · " + ControlCenterAssets.DescribeLocation(layout);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 첫 레이아웃에서 배치 표 높이를 전체의 일정 비율로 나눈다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleTablesGeometryChanged(GeometryChangedEvent evt)
        {
            if (tablesRatioApplied || evt.newRect.height <= 0f)
            {
                return;
            }

            tablesRatioApplied = true;
            layoutTables.fixedPane.style.height = Mathf.Round(evt.newRect.height * placementTableRatio);
        }

        private void ClearLayoutTables()
        {
            layersHost.Unbind();
            layersHost.Clear();
            placementsHost.Unbind();
            placementsHost.Clear();
            layoutObject?.Dispose();
            layoutObject = null;
        }

    #endregion

    #region 씬과 호스트

        private void ConfigureSceneTree()
        {
            sceneReveal.clicked += () => ControlCenterContext.Reveal(selectedScene?.Component);
            sceneTree.makeItem = () =>
            {
                var row = new VisualElement();
                row.AddToClassList("xeri-cc__list-item");

                var name = new Label
                {
                    name = "name"
                };
                name.AddToClassList("xeri-cc__list-item-name");
                row.Add(name);

                var meta = new Label
                {
                    name = "meta"
                };
                meta.AddToClassList("xeri-cc__list-item-meta");
                row.Add(meta);
                return row;
            };
            sceneTree.bindItem = (element, index) =>
            {
                var node = sceneTree.GetItemDataForIndex<SceneNode>(index);
                element.Q<Label>("name").text = node.Title;
                var meta = element.Q<Label>("meta");
                meta.text = node.Entry?.StatusText ?? string.Empty;
                meta.EnableInClassList("xeri-cc__list-item-meta--error", node.Entry?.Status == ControlCenterSceneStatus.Invalid);
            };
            sceneTree.selectionChanged += items =>
            {
                foreach (var item in items)
                {
                    if (item is SceneNode node && node.Entry != null)
                    {
                        selectedScene = node.Entry;
                        RenderSceneDetail();
                    }

                    return;
                }
            };
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 씬별로 Runtime, 레이어 Host, 배치 Host를 묶은 트리를 다시 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private void RefreshSceneTree()
        {
            var entries = Context.SceneEntries;
            var scenes = new Dictionary<string, List<ControlCenterSceneEntry>>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (!scenes.TryGetValue(entry.SceneName, out var list))
                {
                    list = new List<ControlCenterSceneEntry>();
                    scenes.Add(entry.SceneName, list);
                }

                list.Add(entry);
            }

            // 로드된 씬을 위에 두고 활성 씬을 가장 먼저 보여 준다. 프리팹 편집 단계는 마지막이다.
            var orderedScenes = new List<KeyValuePair<string, List<ControlCenterSceneEntry>>>(scenes);
            var activeScene = SceneManager.GetActiveScene().name;
            orderedScenes.Sort
            (
                (left, right) =>
                {
                    var leftRank = GetSceneRank(left.Key, activeScene);
                    var rightRank = GetSceneRank(right.Key, activeScene);
                    return leftRank != rightRank
                        ? leftRank.CompareTo(rightRank)
                        : string.Compare(left.Key, right.Key, StringComparison.Ordinal);
                }
            );

            var id = 0;
            var roots = new List<TreeViewItemData<SceneNode>>();
            foreach (var pair in orderedScenes)
            {
                pair.Value.Sort
                (
                    (left, right) => left.Kind != right.Kind
                        ? left.Kind.CompareTo(right.Kind)
                        : string.Compare(left.Component.name, right.Component.name, StringComparison.Ordinal)
                );

                var children = new List<TreeViewItemData<SceneNode>>();
                foreach (var entry in pair.Value)
                {
                    var title = entry.Component.name + " · " + entry.KindText;
                    var node = new SceneNode
                    {
                        Title = title,
                        Entry = entry
                    };
                    children.Add(new TreeViewItemData<SceneNode>(++id, node));
                }

                var sceneTitle = string.IsNullOrEmpty(pair.Key) ? "프리팹 편집" : pair.Key;
                var sceneNode = new SceneNode
                {
                    Title = sceneTitle
                };
                roots.Add(new TreeViewItemData<SceneNode>(++id, sceneNode, children));
            }

            sceneTree.SetRootItems(roots);
            sceneTree.Rebuild();
            sceneTree.ExpandAll();
            sceneCount.text = "구성 요소 " + entries.Count + "개";
            sceneListEmpty.style.display = entries.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            // 다시 수집한 목록에서 같은 구성 요소를 찾아 선택을 유지한다.
            var component = selectedScene?.Component;
            selectedScene = null;
            foreach (var entry in entries)
            {
                if (component != null && entry.Component == component)
                {
                    selectedScene = entry;
                }
            }

            SelectInTree(selectedScene);
            RenderSceneDetail();
        }

        private static int GetSceneRank(string sceneName, string activeScene)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return 2;
            }

            return sceneName == activeScene ? 0 : 1;
        }

        private void SelectSceneEntry(ControlCenterSceneEntry entry)
        {
            selectedScene = entry;
            SelectInTree(entry);
            RenderSceneDetail();
        }

        private void SelectInTree(ControlCenterSceneEntry entry)
        {
            for (var index = 0; index < sceneTree.GetTreeCount(); index++)
            {
                var node = sceneTree.GetItemDataForIndex<SceneNode>(index);
                if (entry != null && node.Entry == entry)
                {
                    var selectedIndexes = new[]
                    {
                        index
                    };
                    sceneTree.SetSelectionWithoutNotify(selectedIndexes);
                    sceneTree.ScrollToItem(index);
                    return;
                }
            }

            sceneTree.ClearSelection();
        }

        private void RenderSceneDetail()
        {
            var entry = selectedScene;
            var component = entry?.Component;
            sceneReveal.SetEnabled(component != null);

            // 같은 구성 요소면 Inspector를 유지해 편집 중인 입력과 포커스를 잃지 않는다.
            // 파괴된 구성 요소도 다른 대상으로 보도록 참조로 비교한다.
            if (sceneRows == null || !ReferenceEquals(component, inspectedComponent))
            {
                sceneBody.Unbind();
                sceneBody.Clear();
                inspectedComponent = component;
                sceneRows = new VisualElement();
                sceneRows.AddToClassList("xeri-cc__rows");
                sceneBody.Add(sceneRows);
                if (component != null)
                {
                    var inspector = new InspectorElement(component);
                    inspector.AddToClassList("xeri-cc__inspector");
                    sceneBody.Add(inspector);
                }
            }

            sceneRows.Clear();
            if (component == null)
            {
                sceneTitle.text = "구성 요소 선택 안 됨";
                sceneMeta.text = string.Empty;
                if (Context.SceneEntries.Count > 0)
                {
                    sceneRows.Add(ControlCenterElements.CreateNotice("목록에서 Runtime이나 Host를 선택하세요."));
                }

                return;
            }

            sceneTitle.text = component.name;
            sceneMeta.text = entry.KindText + " · " + (string.IsNullOrEmpty(entry.SceneName) ? "프리팹 편집" : entry.SceneName);
            sceneRows.Add(ControlCenterElements.CreateRow("상태", entry.StatusText));
            if (entry.Kind != ControlCenterSceneKind.Runtime)
            {
                var idName = entry.Kind == ControlCenterSceneKind.Layer ? "레이어 ID" : "표현 ID";
                sceneRows.Add(ControlCenterElements.CreateRow(idName, entry.ID));
            }

            ControlCenterElements.StripeRows(sceneRows);
        }

    #endregion

    }
}
