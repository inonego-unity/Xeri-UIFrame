/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterOverviewPage.cs
수정일 : 2026-11-17

# 설명
선택한 설정을 기준으로 레이아웃·입력·초기화·렌더링·씬 연결 상태와 최근 진단을 한 화면에 요약한다.

# 특이사항, 제약사항
값은 실제 조회 결과만 표시하며, 각 행에서 해당 편집 화면이나 대상으로 이동한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.UI.Editor
{
    internal sealed class ControlCenterOverviewPage : ControlCenterPage
    {

    #region 필드

        private const int issueDisplayLimit = 200;
        private readonly VisualElement configRows;
        private readonly VisualElement diagnosticRows;

        internal override ControlCenterTab Tab => ControlCenterTab.Overview;
        internal override string Title => "개요";
        internal override string IconName => "layout-dashboard";

    #endregion

    #region 생성자

        internal ControlCenterOverviewPage(ControlCenterContext context)
            : base(context, "Overview")
        {
            configRows = ControlCenterElements.Require<VisualElement>(Root, "config-rows");
            diagnosticRows = ControlCenterElements.Require<VisualElement>(Root, "diagnostic-rows");
            ControlCenterElements.Require<Button>(Root, "overview-validate").clicked += Context.RunValidation;
            ControlCenterElements.Require<Button>(Root, "overview-open-diagnostics").clicked +=
                () => Context.ShowTab(ControlCenterTab.Diagnostics);
        }

    #endregion

    #region 표시

        protected override void Refresh(ControlCenterChange change)
        {
            RenderConfiguration();
            RenderDiagnostics();
            ControlCenterElements.StripeRows(configRows);
            ControlCenterElements.StripeRows(diagnosticRows);
        }

        private void RenderConfiguration()
        {
            configRows.Clear();
            AddSceneRow();
            var settings = Context.Settings;
            if (settings == null)
            {
                configRows.Add
                (
                    ControlCenterElements.CreateNotice
                    (
                        Context.SettingsAssets.Count == 0
                            ? "프로젝트에 UI 설정 에셋이 없습니다."
                            : "상단의 설정 메뉴에서 관리할 설정 에셋을 선택하세요.",
                        ("설정 열기", () => Context.ShowTab(ControlCenterTab.Settings))
                    )
                );
                return;
            }

            configRows.Add
            (
                ControlCenterElements.CreateLinkRow
                (
                    "설정", settings.name, () => ControlCenterContext.Reveal(settings),
                    AssetDatabase.GetAssetPath(settings)
                )
            );

            var layout = settings.DefaultLayout;
            configRows.Add
            (
                layout == null
                    ? ControlCenterElements.CreateRow("기본 레이아웃", "선택 안 됨")
                    : ControlCenterElements.CreateLinkRow
                    (
                        "기본 레이아웃",
                        layout.name + " · 레이어 " + layout.Layers.Count + " · 배치 " + layout.Placements.Count,
                        () => Context.Navigate(layout)
                    )
            );

            configRows.Add
            (
                ControlCenterElements.CreateLinkRow
                (
                    "연결 대상", settings.DestinationDefinitions.Count + "개",
                    () => OpenSettings(ControlCenterSettingsPage.DestinationsCategory)
                )
            );
            configRows.Add
            (
                ControlCenterElements.CreateLinkRow
                (
                    "입력", DescribeInput(settings),
                    () => OpenSettings(ControlCenterSettingsPage.InputCategory)
                )
            );

            var modules = ControlCenterAssets.FindBootstrapperModules(settings);
            var unregistered = 0;
            foreach (var module in modules)
            {
                if (!ControlCenterAssets.IsBootstrapperRegistered(module))
                {
                    unregistered++;
                }
            }

            var moduleText = modules.Count == 0 ? "연결된 초기화 모듈 없음" : "초기화 모듈 " + modules.Count + "개";
            if (unregistered > 0)
            {
                moduleText += " · 미등록 " + unregistered + "개";
            }

            configRows.Add
            (
                ControlCenterElements.CreateLinkRow
                (
                    "초기화", moduleText,
                    () => OpenSettings(ControlCenterSettingsPage.BootstrapperCategory)
                )
            );

            var renderers = ControlCenterRendering.Find();
            var rendererNames = new List<string>();
            foreach (var renderer in renderers)
            {
                rendererNames.Add(renderer.DisplayName);
            }

            configRows.Add
            (
                ControlCenterElements.CreateLinkRow
                (
                    "렌더링 출력",
                    rendererNames.Count == 0 ? "해당 파이프라인 없음" : string.Join(", ", rendererNames),
                    () => OpenSettings(ControlCenterSettingsPage.RenderingCategory)
                )
            );
        }

        private void AddSceneRow()
        {
            var runtimes = 0;
            var layers = 0;
            var placements = 0;
            foreach (var entry in Context.SceneEntries)
            {
                switch (entry.Kind)
                {
                    case ControlCenterSceneKind.Runtime: runtimes++; break;
                    case ControlCenterSceneKind.Layer: layers++; break;
                    default: placements++; break;
                }
            }

            configRows.Add
            (
                ControlCenterElements.CreateLinkRow
                (
                    "로드된 씬",
                    "런타임 " + runtimes + " · 레이어 호스트 " + layers + " · 배치 호스트 " + placements,
                    () => Context.ShowComposition(true)
                )
            );
        }

        private static string DescribeInput(UISettingsAsset settings)
        {
            var ui = string.IsNullOrWhiteSpace(settings.UIActionMap) ? "UI 맵 없음" : "UI 맵 " + settings.UIActionMap;
            var gameplay = string.IsNullOrWhiteSpace(settings.GameplayActionMap)
                ? "게임 맵 없음" : "게임 맵 " + settings.GameplayActionMap;
            return ui + " · " + gameplay + " · 해제 입력 " + settings.ReleaseActionNames.Count + "개";
        }

        private void OpenSettings(int category)
        {
            Context.ShowSettingsCategory(category);
        }

        private void RenderDiagnostics()
        {
            diagnosticRows.Clear();
            switch (Context.ValidationState)
            {
                case ControlCenterValidationState.NotRun:
                    {
                        diagnosticRows.Add(ControlCenterElements.CreateRow("상태", "검사 전"));
                        return;
                    }

                case ControlCenterValidationState.Failed:
                    {
                        diagnosticRows.Add(ControlCenterElements.CreateRow("상태", "검사 실패"));
                        diagnosticRows.Add(ControlCenterElements.CreateRow("원인", Context.ValidationFailure));
                        return;
                    }

                case ControlCenterValidationState.Stale:
                    {
                        diagnosticRows.Add(ControlCenterElements.CreateRow("상태", "재검사 필요"));
                        return;
                    }
            }

            diagnosticRows.Add
            (
                ControlCenterElements.CreateRow
                (
                    "결과",
                    "오류 " + Context.CountIssues(DiagnosticSeverity.Error)
                        + " · 경고 " + Context.CountIssues(DiagnosticSeverity.Warning)
                        + " · 정보 " + Context.CountIssues(DiagnosticSeverity.Info)
                )
            );

            // 심각도 순으로 정렬된 결과를 카드 안에서 스크롤로 보여준다.
            var count = Math.Min(issueDisplayLimit, Context.Issues.Count);
            for (var index = 0; index < count; index++)
            {
                var issue = Context.Issues[index];
                var row = new ControlCenterIssueRow();
                row.Bind(issue);
                row.AddToClassList("xeri-cc__issue--clickable");
                row.RegisterCallback<ClickEvent>(_ => Context.Navigate(issue.Target));
                diagnosticRows.Add(row);
            }
        }

    #endregion

    }
}
