/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterDiagnosticsPage.cs
수정일 : 2026-11-17

# 설명
검사 범위, 심각도 필터와 검색, 진단 목록과 선택 항목 상세 및 대상 이동을 제공한다.

# 특이사항, 제약사항
검사 전·재검사 필요·검사 실패 상태에서는 이전 개수를 표시하지 않아 오류 0건을 통과로 오인하지 않게 한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI.Editor;

namespace inonego.Xeri.UI.Frame.Editor
{
    internal sealed class ControlCenterDiagnosticsPage : ControlCenterPage
    {

    #region 필드

        private readonly ToolbarButton filterAll;
        private readonly ToolbarButton filterError;
        private readonly ToolbarButton filterWarning;
        private readonly ToolbarButton filterInfo;
        private readonly Label filterAllText;
        private readonly Label filterErrorText;
        private readonly Label filterWarningText;
        private readonly Label filterInfoText;
        private readonly ToolbarSearchField search;
        private readonly Label scope;
        private readonly ListView list;
        private readonly VisualElement empty;
        private readonly Label title;
        private readonly Label code;
        private readonly ScrollView body;
        private readonly List<ControlCenterIssue> visibleIssues = new();
        private DiagnosticSeverity? severityFilter;
        private ControlCenterIssue selectedIssue;

        internal override ControlCenterTab Tab => ControlCenterTab.Diagnostics;
        internal override string Title => "진단";
        internal override string IconName => "scan-search";

    #endregion

    #region 생성자

        internal ControlCenterDiagnosticsPage(ControlCenterContext context)
            : base(context, "Diagnostics")
        {
            ControlCenterElements.Require<ToolbarButton>(Root, "diagnostics-run").clicked += Context.RunValidation;
            // 전체는 필터 아이콘, 심각도별 필터는 Core SeverityIndicator 아이콘을 쓴다.
            filterAll = ControlCenterElements.Require<ToolbarButton>(Root, "filter-all");
            filterError = ControlCenterElements.Require<ToolbarButton>(Root, "filter-error");
            filterWarning = ControlCenterElements.Require<ToolbarButton>(Root, "filter-warning");
            filterInfo = ControlCenterElements.Require<ToolbarButton>(Root, "filter-info");
            var allIcon = new Image { vectorImage = XeriIcon.Load("list-filter") };
            filterAllText = CreateFilterContent(filterAll, allIcon, () => SetFilter(null));
            filterErrorText = CreateFilterContent
            (
                filterError, new SeverityIndicator(DiagnosticSeverity.Error), () => SetFilter(DiagnosticSeverity.Error)
            );
            filterWarningText = CreateFilterContent
            (
                filterWarning, new SeverityIndicator(DiagnosticSeverity.Warning), () => SetFilter(DiagnosticSeverity.Warning)
            );
            filterInfoText = CreateFilterContent
            (
                filterInfo, new SeverityIndicator(DiagnosticSeverity.Info), () => SetFilter(DiagnosticSeverity.Info)
            );

            search = ControlCenterElements.Require<ToolbarSearchField>(Root, "diagnostics-search");
            search.RegisterValueChangedCallback(_ => RenderList());
            scope = ControlCenterElements.Require<Label>(Root, "diagnostics-scope");
            empty = ControlCenterElements.Require<VisualElement>(Root, "diagnostics-empty");
            title = ControlCenterElements.Require<Label>(Root, "diagnostics-title");
            code = ControlCenterElements.Require<Label>(Root, "diagnostics-code");
            body = ControlCenterElements.Require<ScrollView>(Root, "diagnostics-body");
            RegisterResponsiveSplit(ControlCenterElements.Require<TwoPaneSplitView>(Root, "diagnostics-split"));

            list = ControlCenterElements.Require<ListView>(Root, "diagnostics-list");
            list.itemsSource = visibleIssues;
            list.makeItem = () => new ControlCenterIssueRow();
            list.bindItem = (element, index) => ((ControlCenterIssueRow)element).Bind(visibleIssues[index]);
            list.selectionChanged += items =>
            {
                foreach (var item in items)
                {
                    selectedIssue = item as ControlCenterIssue;
                    RenderDetail();
                    return;
                }
            };
            list.itemsChosen += items =>
            {
                foreach (var item in items)
                {
                    Context.Navigate((item as ControlCenterIssue)?.Target);
                    return;
                }
            };
        }

    #endregion

    #region 표시

        protected override void Refresh(ControlCenterChange change)
        {
            if ((change & (ControlCenterChange.Validation | ControlCenterChange.Selection | ControlCenterChange.Scenes)) == 0)
            {
                return;
            }

            RenderFilters();
            RenderList();
        }

        private static Label CreateFilterContent(ToolbarButton button, VisualElement icon, Action select)
        {
            icon.AddToClassList("xeri-cc__filter-icon");
            var text = new Label();
            text.AddToClassList("xeri-cc__filter-text");
            button.Add(icon);
            button.Add(text);
            button.clicked += select;
            return text;
        }

        private void SetFilter(DiagnosticSeverity? severity)
        {
            severityFilter = severity;
            RenderFilters();
            RenderList();
        }

        private void RenderFilters()
        {
            // 검사 전에도 0으로 표시하고, 결과가 유효하지 않으면 이전 개수를 보이지 않는다.
            var done = Context.ValidationState == ControlCenterValidationState.Done;
            int Count(int value) => done ? value : 0;
            filterAllText.text = "전체 " + Count(Context.Issues.Count);
            filterErrorText.text = "오류 " + Count(Context.CountIssues(DiagnosticSeverity.Error));
            filterWarningText.text = "경고 " + Count(Context.CountIssues(DiagnosticSeverity.Warning));
            filterInfoText.text = "정보 " + Count(Context.CountIssues(DiagnosticSeverity.Info));
            filterAll.EnableInClassList("xeri-cc__subtab--active", severityFilter == null);
            filterError.EnableInClassList("xeri-cc__subtab--active", severityFilter == DiagnosticSeverity.Error);
            filterWarning.EnableInClassList("xeri-cc__subtab--active", severityFilter == DiagnosticSeverity.Warning);
            filterInfo.EnableInClassList("xeri-cc__subtab--active", severityFilter == DiagnosticSeverity.Info);

            scope.text = Context.ValidationState switch
            {
                ControlCenterValidationState.NotRun => "검사 전 · 검사를 누르면 선택한 설정, 레이아웃과 로드된 씬을 확인합니다.",
                ControlCenterValidationState.Stale => "재검사 필요 · 검사 후 대상이 변경됐습니다.",
                ControlCenterValidationState.Failed => "검사 실패 · " + Context.ValidationFailure,
                _ => "검사 범위 · " + Context.ValidationScope,
            };
        }

        private void RenderList()
        {
            visibleIssues.Clear();
            empty.Clear();
            if (Context.ValidationState == ControlCenterValidationState.Done)
            {
                var text = search.value;
                foreach (var issue in Context.Issues)
                {
                    if (severityFilter.HasValue && issue.Severity != severityFilter.Value)
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(text) && !Matches(issue, text))
                    {
                        continue;
                    }

                    visibleIssues.Add(issue);
                }
            }

            list.RefreshItems();
            if (!visibleIssues.Contains(selectedIssue))
            {
                selectedIssue = visibleIssues.Count > 0 ? visibleIssues[0] : null;
            }

            var index = visibleIssues.IndexOf(selectedIssue);
            var selectedIndexes = new List<int>();
            if (index >= 0)
            {
                selectedIndexes.Add(index);
            }

            list.SetSelectionWithoutNotify(selectedIndexes);
            var message = DescribeEmpty();
            empty.style.display = message == null ? DisplayStyle.None : DisplayStyle.Flex;
            list.style.display = message == null ? DisplayStyle.Flex : DisplayStyle.None;
            if (message != null)
            {
                empty.Add(ControlCenterElements.CreateNotice(message, ("검사", Context.RunValidation)));
            }

            RenderDetail();
        }

        private string DescribeEmpty()
        {
            if (Context.ValidationState != ControlCenterValidationState.Done)
            {
                return Context.ValidationState == ControlCenterValidationState.Failed
                    ? "검사를 완료하지 못했습니다."
                    : "검사 결과가 없습니다.";
            }

            if (visibleIssues.Count > 0)
            {
                return null;
            }

            if (Context.Issues.Count > 0)
            {
                return "조건에 맞는 항목이 없습니다.";
            }

            return Context.HasValidationTarget ? "문제가 없습니다." : "검사할 설정, 레이아웃이나 씬 구성 요소가 없습니다.";
        }

        private static bool Matches(ControlCenterIssue issue, string text)
        {
            bool Has(string value) => value != null && value.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
            return Has(issue.Title) || Has(issue.Detail) || Has(issue.Code) || Has(issue.Target?.name);
        }

        private void RenderDetail()
        {
            body.Clear();
            var issue = selectedIssue;
            if (issue == null)
            {
                title.text = "항목 선택 안 됨";
                code.text = string.Empty;
                return;
            }

            title.text = issue.Title;
            code.text = ControlCenterElements.SeverityText(issue.Severity) + " · " + issue.Code;

            var rows = new VisualElement();
            rows.AddToClassList("xeri-cc__rows");
            rows.Add(ControlCenterElements.CreateRow("내용", issue.Detail));
            rows.Add
            (
                issue.Target == null
                    ? ControlCenterElements.CreateRow("대상", "없음")
                    : ControlCenterElements.CreateLinkRow
                    (
                        "대상", issue.Target.name, () => Context.Navigate(issue.Target),
                        issue.Target is Component ? null : AssetDatabase.GetAssetPath(issue.Target)
                    )
            );
            ControlCenterElements.StripeRows(rows);
            body.Add(rows);
        }

    #endregion

    }
}
