/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterValidation.cs
수정일 : 2026-11-17

# 설명
레이아웃, 설정과 로드된 씬 Host의 정적 진단을 수집한다.

# 특이사항, 제약사항
레이어 ID·순서의 유일성은 레이아웃 내부에서만 검사하고, Root 연결 규칙은 설정의 기본 레이아웃에만 적용한다.
검사는 읽기 전용이며 에셋이나 씬을 수정하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.UI.Frame.Editor
{
    internal sealed class ControlCenterIssue
    {
        internal string Code { get; }
        internal DiagnosticSeverity Severity { get; }
        internal string Title { get; }
        internal string Detail { get; }
        internal UnityEngine.Object Target { get; }

        internal ControlCenterIssue
        (
            string code, DiagnosticSeverity severity,
            string title, string detail, UnityEngine.Object target
        )
        {
            Code = code;
            Severity = severity;
            Title = title;
            Detail = detail;
            Target = target;
        }
    }

    internal static class ControlCenterValidation
    {

    #region 검사 실행

        internal static List<ControlCenterIssue> Run
        (
            UISettingsAsset settings,
            PresentationLayout selectedLayout,
            IReadOnlyList<ControlCenterSceneEntry> scenes
        )
        {
            var issues = new List<ControlCenterIssue>();
            var visited = new HashSet<PresentationLayout>();
            if (selectedLayout != null && visited.Add(selectedLayout))
            {
                CheckLayout(selectedLayout, issues);
            }

            if (settings != null)
            {
                if (settings.DefaultLayout == null)
                {
                    Error(issues, "XUI-SET-001", "기본 레이아웃 없음", "설정에서 기본 레이아웃을 선택하세요.", settings);
                }
                else
                {
                    if (visited.Add(settings.DefaultLayout))
                    {
                        CheckLayout(settings.DefaultLayout, issues);
                    }

                    CheckRoot(settings, issues);
                }

                CheckInput(settings, issues);
            }

            if (scenes != null)
            {
                ControlCenterHostValidation.Validate(scenes, issues);
            }

            return issues;
        }

    #endregion

    #region 레이아웃 검사

        private static void CheckLayout(PresentationLayout layout, List<ControlCenterIssue> issues)
        {
            if (layout.Layers == null || layout.Layers.Count == 0)
            {
                Error(issues, "XUI-LAYOUT-001", "레이어 없음", "레이어를 하나 이상 정의하세요.", layout);
                return;
            }

            var layerIDs = new HashSet<string>(StringComparer.Ordinal);
            var orders = new HashSet<int>();
            var localOrders = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
            for (var index = 0; index < layout.Layers.Count; index++)
            {
                var layer = layout.Layers[index];
                if (layer == null || string.IsNullOrWhiteSpace(layer.LayerID))
                {
                    Error
                    (
                        issues, "XUI-LAYOUT-002", "레이어 ID 없음",
                        (index + 1) + "번째 레이어에 ID가 없습니다.", layout
                    );
                    continue;
                }

                if (!layerIDs.Add(layer.LayerID))
                {
                    Error(issues, "XUI-LAYOUT-003", "레이어 ID 중복", layer.LayerID, layout);
                }

                if (!orders.Add(layer.LayerOrder))
                {
                    Error(issues, "XUI-LAYOUT-004", "레이어 순서 중복", layer.LayerID + " · 순서 " + layer.LayerOrder, layout);
                }

                if (layer.LayerOrder < short.MinValue || layer.LayerOrder > short.MaxValue)
                {
                    Error(issues, "XUI-LAYOUT-005", "레이어 순서 범위 초과", layer.LayerID, layout);
                }

                if (!Enum.IsDefined(typeof(PresentationBackend), layer.Backend))
                {
                    Error(issues, "XUI-LAYOUT-006", "백엔드 값 오류", layer.LayerID, layout);
                }

                if (string.IsNullOrWhiteSpace(layer.DisplayName))
                {
                    Add
                    (
                        issues, "XUI-LAYOUT-007", DiagnosticSeverity.Info, "레이어 이름 없음",
                        layer.LayerID + " 레이어에 표시 이름이 없습니다.", layout
                    );
                }

                localOrders.TryAdd(layer.LayerID, new HashSet<int>());
            }

            var placementIDs = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < layout.Placements.Count; index++)
            {
                var placement = layout.Placements[index];
                if (placement == null || string.IsNullOrWhiteSpace(placement.PresentationID))
                {
                    Error
                    (
                        issues, "XUI-PLACEMENT-001", "배치 ID 없음",
                        (index + 1) + "번째 배치에 ID가 없습니다.", layout
                    );
                    continue;
                }

                if (!placementIDs.Add(placement.PresentationID))
                {
                    Error(issues, "XUI-PLACEMENT-002", "배치 ID 중복", placement.PresentationID, layout);
                }

                if (!localOrders.TryGetValue(placement.LayerID ?? "", out var local))
                {
                    Error
                    (
                        issues, "XUI-PLACEMENT-003", "배치 레이어 없음",
                        placement.PresentationID + " → " + placement.LayerID, layout
                    );
                }
                else if (!local.Add(placement.LocalOrder))
                {
                    Error
                    (
                        issues, "XUI-PLACEMENT-004", "배치 순서 중복",
                        placement.LayerID + " · 순서 " + placement.LocalOrder, layout
                    );
                }
            }
        }

    #endregion

    #region 설정과 입력 검사

        private static void CheckRoot(UISettingsAsset settings, List<ControlCenterIssue> issues)
        {
            var support = settings.BackendSupport;
            if (support == PresentationBackendSupport.None
                || (support & ~PresentationBackendSupport.All) != PresentationBackendSupport.None)
            {
                Error(issues, "XUI-DEST-006", "지원 백엔드 설정 오류", "지원 백엔드를 확인하세요.", settings);
            }

            if (!Enum.IsDefined(typeof(UIPresentationPostProcessing), settings.UIPostProcessing))
            {
                Error(issues, "XUI-DEST-007", "후처리 설정 오류", "후처리 설정을 확인하세요.", settings);
            }

            var fadeTime = settings.DefaultFadeDuration;
            if (float.IsNaN(fadeTime) || float.IsInfinity(fadeTime) || fadeTime < 0f)
            {
                Error(issues, "XUI-DEST-008", "전환 시간 오류", "전환 시간은 0 이상이어야 합니다.", settings);
            }

            // 기본 레이아웃의 레이어마다 설정이 해당 백엔드를 지원해야 한다.
            var layers = new HashSet<string>(StringComparer.Ordinal);
            foreach (var layer in settings.DefaultLayout.Layers)
            {
                if (layer == null || string.IsNullOrWhiteSpace(layer.LayerID))
                {
                    continue;
                }

                layers.Add(layer.LayerID);
                var required = layer.Backend == PresentationBackend.UITK
                    ? PresentationBackendSupport.UITK : PresentationBackendSupport.UGUI;
                if ((support & required) != required)
                {
                    Error(issues, "XUI-DEST-003", "미지원 백엔드", layer.LayerID + " · " + layer.Backend, settings);
                }
            }

            CheckDestinations(settings, layers, issues);
        }

        private static void CheckDestinations
        (
            UISettingsAsset settings, HashSet<string> layers, List<ControlCenterIssue> issues
        )
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (settings.DestinationDefinitions == null || settings.DestinationDefinitions.Count == 0)
            {
                Error(issues, "XUI-DEST-001", "연결 대상 없음", "연결 대상을 하나 이상 정의하세요.", settings);
            }
            else
            {
                foreach (var definition in settings.DestinationDefinitions)
                {
                    if (definition == null || string.IsNullOrWhiteSpace(definition.DestinationID)
                        || !ids.Add(definition.DestinationID))
                    {
                        Error(issues, "XUI-DEST-002", "연결 ID 오류", "연결 ID가 비어 있거나 중복됐습니다.", settings);
                        continue;
                    }

                    if (!layers.Contains(definition.LayerID ?? ""))
                    {
                        Error
                        (
                            issues, "XUI-DEST-004", "연결 레이어 없음",
                            definition.DestinationID + " → " + definition.LayerID, settings
                        );
                    }
                }
            }

            if (!ids.Contains(PresentationDestinationID.System))
            {
                Error
                (
                    issues, "XUI-DEST-005", "시스템 연결 대상 없음",
                    PresentationDestinationID.System + " 연결 대상을 추가하세요.", settings
                );
            }
        }

        private static void CheckInput(UISettingsAsset settings, List<ControlCenterIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(settings.UIActionMap))
            {
                Error(issues, "XUI-INPUT-001", "UI 액션 맵 없음", "UI 액션 맵 이름을 입력하세요.", settings);
            }

            if ((settings.BackendSupport & PresentationBackendSupport.UGUI) == PresentationBackendSupport.None
                && settings.UIActionsAsset == null)
            {
                Error(issues, "XUI-INPUT-007", "UI 입력 에셋 없음", "UI 입력 에셋을 선택하세요.", settings);
            }

            if (settings.GameplayActionsAsset == null || string.IsNullOrWhiteSpace(settings.GameplayActionMap))
            {
                Error(issues, "XUI-INPUT-002", "게임 입력 설정 누락", "게임 입력 에셋과 액션 맵을 지정하세요.", settings);
            }
            else if (settings.GameplayActionsAsset.FindActionMap(settings.GameplayActionMap, false) == null)
            {
                Error(issues, "XUI-INPUT-003", "게임 액션 맵 없음", settings.GameplayActionMap, settings);
            }

            if (settings.UIActionsAsset != null
                && settings.UIActionsAsset.FindActionMap(settings.UIActionMap, false) == null)
            {
                Error(issues, "XUI-INPUT-004", "UI 액션 맵을 찾을 수 없음", settings.UIActionMap, settings);
            }

            if (settings.ReleaseActionNames == null || settings.ReleaseActionNames.Count == 0)
            {
                Error(issues, "XUI-INPUT-005", "해제 입력 없음", "해제 입력을 하나 이상 지정하세요.", settings);
                return;
            }

            foreach (var name in settings.ReleaseActionNames)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    Error(issues, "XUI-INPUT-006", "해제 입력 이름 없음", "빈 항목을 수정하세요.", settings);
                }
            }
        }

    #endregion

    #region 진단 결과 생성

        private static void Error
        (
            List<ControlCenterIssue> issues, string code, string title, string detail,
            UnityEngine.Object target
        )
        {
            Add(issues, code, DiagnosticSeverity.Error, title, detail, target);
        }

        private static void Add
        (
            List<ControlCenterIssue> issues, string code, DiagnosticSeverity severity,
            string title, string detail, UnityEngine.Object target
        )
        {
            issues.Add(new ControlCenterIssue(code, severity, title, detail, target));
        }

    #endregion

    }
}
