/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterLivePage.cs
수정일 : 2026-11-17

# 설명
Play Mode의 Runtime 진단 Snapshot을 컨텍스트·표현 세션 목록, 선택 항목 상세와 최근 사건으로 표시한다.

# 특이사항, 제약사항
Snapshot 내용이 바뀐 경우에만 목록을 갱신하고 선택 위치를 유지한다.
Runtime 변경 명령은 XeriUIDiagnostics의 공개 API로만 보낸다.
컨텍스트와 표현 세션은 일대일로 대응하지 않으므로 서로 다른 항목으로 나열한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.UI.Editor
{
    internal sealed class ControlCenterLivePage : ControlCenterPage
    {

    #region 내부 데이터

        private sealed class LiveNode
        {
            internal string Label;
            internal bool IsHeader;
            internal int Depth;
            internal XeriUIDebugContext Context;
            internal XeriUIDebugPresentation Presentation;
        }

    #endregion

    #region 필드

        private const double refreshInterval = 0.25d;
        private const int eventLimit = 50;

        private readonly Label summary;
        private readonly ToolbarButton closeTop;
        private readonly VisualElement content;
        private readonly ListView nodes;
        private readonly Label title;
        private readonly Label meta;
        private readonly ScrollView body;
        private readonly ListView events;
        private readonly VisualElement empty;
        private readonly List<LiveNode> nodeItems = new();
        private readonly List<XeriUIDebugEvent> eventItems = new();
        private string signature;
        private double nextRefresh;
        private int selectedNode;

        internal override ControlCenterTab Tab => ControlCenterTab.Live;
        internal override string Title => "실행";
        internal override string IconName => "activity";

    #endregion

    #region 생성자

        internal ControlCenterLivePage(ControlCenterContext context)
            : base(context, "Live")
        {
            summary = ControlCenterElements.Require<Label>(Root, "live-summary");
            closeTop = ControlCenterElements.Require<ToolbarButton>(Root, "live-close-top");
            closeTop.clicked += () =>
            {
                if (XeriUIDiagnostics.TryCloseTopScreen())
                {
                    signature = null;
                }
            };
            content = ControlCenterElements.Require<TwoPaneSplitView>(Root, "live-vertical");
            RegisterResponsiveSplit(ControlCenterElements.Require<TwoPaneSplitView>(Root, "live-split"));
            title = ControlCenterElements.Require<Label>(Root, "live-title");
            meta = ControlCenterElements.Require<Label>(Root, "live-meta");
            body = ControlCenterElements.Require<ScrollView>(Root, "live-body");
            empty = ControlCenterElements.Require<VisualElement>(Root, "live-empty");

            nodes = ControlCenterElements.Require<ListView>(Root, "live-nodes");
            nodes.itemsSource = nodeItems;
            nodes.makeItem = () =>
            {
                var label = new Label();
                label.AddToClassList("xeri-cc__nav-item");
                return label;
            };
            nodes.bindItem = (element, index) =>
            {
                var node = nodeItems[index];
                var label = (Label)element;
                label.text = node.Label;
                label.style.paddingLeft = 8f + node.Depth * 14f;
                label.EnableInClassList("xeri-cc__nav-item--header", node.IsHeader);
                label.EnableInClassList("xeri-cc__nav-item--active", node.Context?.IsEffective == true);
            };
            nodes.selectionChanged += _ =>
            {
                selectedNode = nodes.selectedIndex;
                RenderDetail();
            };

            events = ControlCenterElements.Require<ListView>(Root, "live-events");
            events.itemsSource = eventItems;
            events.makeItem = () =>
            {
                var label = new Label();
                label.AddToClassList("xeri-cc__log-item");
                return label;
            };
            events.bindItem = (element, index) =>
            {
                var item = eventItems[index];
                ((Label)element).text = item.Time + "  " + item.Kind + "  " + item.Message;
                element.tooltip = item.Message;
            };
        }

    #endregion

    #region 표시

        protected override void Refresh(ControlCenterChange change)
        {
            signature = null;
            Render();
        }

        internal override void Tick()
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextRefresh)
            {
                return;
            }

            nextRefresh = EditorApplication.timeSinceStartup + refreshInterval;
            Render();
        }

        private void Render()
        {
            if (!EditorApplication.isPlaying)
            {
                signature = null;
                ShowEmpty("플레이 모드에서 컨텍스트, 표현 세션과 최근 사건을 표시합니다.", ("플레이 모드 시작", EditorApplication.EnterPlaymode));
                return;
            }

            var snapshot = XeriUIDiagnostics.Capture();
            var recent = XeriUIDiagnostics.ReadEvents();
            var next = BuildSignature(snapshot, recent);
            if (next == signature)
            {
                return;
            }

            signature = next;
            if (!snapshot.IsAvailable)
            {
                ShowEmpty(snapshot.State);
                return;
            }

            empty.style.display = DisplayStyle.None;
            content.style.display = DisplayStyle.Flex;
            closeTop.SetEnabled(HasClosableScreen(snapshot));
            summary.text = "설정 " + snapshot.SettingsName + " · 레이아웃 " + snapshot.LayoutName
                + " · 입력 " + snapshot.LastInputDevice;

            BuildNodes(snapshot);
            nodes.RefreshItems();
            selectedNode = Math.Clamp(selectedNode, 0, Math.Max(0, nodeItems.Count - 1));
            if (nodeItems.Count > 0 && nodeItems[selectedNode].IsHeader && selectedNode + 1 < nodeItems.Count)
            {
                selectedNode++;
            }

            var selectedIndexes = new List<int>();
            if (nodeItems.Count > 0)
            {
                selectedIndexes.Add(selectedNode);
            }

            nodes.SetSelectionWithoutNotify(selectedIndexes);
            RenderDetail();

            // 최근 사건은 최신 항목이 위에 오도록 제한된 개수만 표시한다.
            eventItems.Clear();
            for (var index = recent.Count - 1; index >= 0 && eventItems.Count < eventLimit; index--)
            {
                eventItems.Add(recent[index]);
            }

            events.RefreshItems();
        }

        private void ShowEmpty(string message, params (string, Action)[] actions)
        {
            content.style.display = DisplayStyle.None;
            empty.style.display = DisplayStyle.Flex;
            empty.Clear();
            empty.Add(ControlCenterElements.CreateNotice(message, actions));
            summary.text = EditorApplication.isPlaying ? "실행 중" : "편집 중";
            closeTop.SetEnabled(false);
        }

        private static bool HasClosableScreen(XeriUIDebugSnapshot snapshot)
        {
            foreach (var context in snapshot.Contexts)
            {
                if (context.IsEffective && context.Screens.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildNodes(XeriUIDebugSnapshot snapshot)
        {
            nodeItems.Clear();
            nodeItems.Add
            (
                new LiveNode
                {
                    Label = "컨텍스트 " + snapshot.Contexts.Count,
                    IsHeader = true
                }
            );
            for (var index = 0; index < snapshot.Contexts.Count; index++)
            {
                var context = snapshot.Contexts[index];
                var label = "컨텍스트 " + (index + 1) + (context.IsEffective ? " · 입력 권한" : string.Empty);
                nodeItems.Add
                (
                    new LiveNode
                    {
                        Label = label,
                        Depth = context.Depth + 1,
                        Context = context
                    }
                );
            }

            nodeItems.Add
            (
                new LiveNode
                {
                    Label = "표현 세션 " + snapshot.Presentations.Count,
                    IsHeader = true
                }
            );
            for (var index = 0; index < snapshot.Presentations.Count; index++)
            {
                var presentation = snapshot.Presentations[index];
                var label = "세션 " + (index + 1) + " · " + presentation.LayoutName;
                nodeItems.Add
                (
                    new LiveNode
                    {
                        Label = label,
                        Depth = presentation.Depth + 1,
                        Presentation = presentation
                    }
                );
            }
        }

        private void RenderDetail()
        {
            body.Clear();
            var node = selectedNode >= 0 && selectedNode < nodeItems.Count ? nodeItems[selectedNode] : null;
            if (node?.Context != null)
            {
                RenderContext(node.Label, node.Context);
            }
            else if (node?.Presentation != null)
            {
                RenderPresentation(node.Label, node.Presentation);
            }
            else
            {
                title.text = "항목 선택 안 됨";
                meta.text = string.Empty;
            }
        }

        private void RenderContext(string label, XeriUIDebugContext context)
        {
            title.text = label;
            meta.text = context.IsEffective ? "현재 입력 권한" : "입력 권한 없음";

            var rows = new VisualElement();
            rows.AddToClassList("xeri-cc__rows");
            rows.Add(ControlCenterElements.CreateRow("레이아웃", context.LayoutName));
            rows.Add(ControlCenterElements.CreateRow("활성 입력 경로", context.IsOnActivePath ? "포함" : "제외"));
            rows.Add(ControlCenterElements.CreateRow("포커스 권한", context.HasFocusAuthority ? "보유" : "없음"));
            rows.Add(ControlCenterElements.CreateRow("모달", context.ModalCount + "개"));
            rows.Add(ControlCenterElements.CreateRow("화면 스택", context.Screens.Count + "개"));

            // 화면 스택은 최상단부터 나열한다.
            for (var index = context.Screens.Count - 1; index >= 0; index--)
            {
                rows.Add(ControlCenterElements.CreateRow(index == context.Screens.Count - 1 ? "최상단" : "  " + (index + 1), context.Screens[index]));
            }

            ControlCenterElements.StripeRows(rows);
            body.Add(rows);
        }

        private void RenderPresentation(string label, XeriUIDebugPresentation presentation)
        {
            title.text = label;
            meta.text = "레이어 " + presentation.Layers.Count + " · 배치 " + presentation.Placements.Count;

            var rows = new VisualElement();
            rows.AddToClassList("xeri-cc__rows");
            rows.Add(ControlCenterElements.CreateRow("레이아웃", presentation.LayoutName));
            foreach (var layer in presentation.Layers)
            {
                rows.Add(ControlCenterElements.CreateRow("레이어", layer));
            }

            foreach (var placement in presentation.Placements)
            {
                rows.Add(ControlCenterElements.CreateRow("배치", placement));
            }

            ControlCenterElements.StripeRows(rows);
            body.Add(rows);
        }

    #endregion

    #region 상태 비교

        private static string BuildSignature
        (
            XeriUIDebugSnapshot snapshot, IReadOnlyList<XeriUIDebugEvent> recent
        )
        {
            var text = new StringBuilder();
            text.Append(snapshot.IsAvailable).Append('|').Append(snapshot.State).Append('|')
                .Append(snapshot.SettingsName).Append('|').Append(snapshot.LayoutName).Append('|')
                .Append(snapshot.LastInputDevice).Append('|').Append(snapshot.ChildPresentationCount);

            foreach (var context in snapshot.Contexts)
            {
                text.Append('|').Append(context.Depth).Append(context.IsEffective)
                    .Append(context.IsOnActivePath).Append(context.HasFocusAuthority)
                    .Append(context.LayoutName).Append(context.ModalCount);
                foreach (var screen in context.Screens)
                {
                    text.Append('|').Append(screen);
                }
            }

            foreach (var presentation in snapshot.Presentations)
            {
                text.Append('|').Append(presentation.Depth).Append(presentation.LayoutName)
                    .Append(presentation.Layers.Count).Append(presentation.Placements.Count);
            }

            text.Append('|').Append(recent.Count);
            if (recent.Count > 0)
            {
                var last = recent[recent.Count - 1];
                text.Append('|').Append(last.Time).Append(last.Message);
            }

            return text.ToString();
        }

    #endregion

    }
}
