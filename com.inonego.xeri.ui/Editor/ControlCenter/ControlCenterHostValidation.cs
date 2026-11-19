/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterHostValidation.cs
수정일 : 2026-11-17

# 설명
로드된 씬의 Runtime Host 연결을 실제 Presentation 해석 규칙으로 검사한다.

# 특이사항, 제약사항
Runtime에 등록된 Host는 같은 Runtime Transform root 아래 있어야 한다.
레이아웃 대응은 실행 중인 Runtime의 실제 설정에서만 확정한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.UI.Editor
{
    internal static class ControlCenterHostValidation
    {

    #region 호스트 참조 검사

        internal static void Validate
        (
            IReadOnlyList<ControlCenterSceneEntry> entries,
            List<ControlCenterIssue> issues
        )
        {
            var assigned = new HashSet<PresentationLayerHost>();
            foreach (var entry in entries)
            {
                if (entry.Kind == ControlCenterSceneKind.Runtime && entry.Component is UIRuntime runtime)
                {
                    ValidateRuntime(runtime, assigned, issues);
                }
            }

            // 등록되지 않은 Host도 식별자가 없으면 이후 연결할 때 사용할 수 없다.
            foreach (var entry in entries)
            {
                if (entry.Component is PresentationLayerHost layer
                    && !assigned.Contains(layer) && string.IsNullOrWhiteSpace(layer.LayerID))
                {
                    Add(issues, "XUI-HOST-009", "미연결 레이어 식별자 없음", layer, "레이어 식별자를 지정하세요.");
                }

                if (entry.Component is PresentationPlacementHost placement
                    && string.IsNullOrWhiteSpace(placement.PresentationID))
                {
                    Add(issues, "XUI-HOST-010", "배치 식별자 없음", placement, "배치 식별자를 지정하세요.");
                }
            }
        }

        private static void ValidateRuntime
        (
            UIRuntime runtime,
            HashSet<PresentationLayerHost> assigned,
            List<ControlCenterIssue> issues
        )
        {
            if (!ControlCenterScenes.TryReadHostReferences(runtime, out var references))
            {
                Add(issues, "XUI-HOST-001", "호스트 목록 확인 실패", runtime, "런타임의 호스트 참조를 확인할 수 없습니다.");
                return;
            }

            var hosts = new List<PresentationLayerHost>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var placements = new HashSet<PresentationPlacementHost>();
            foreach (var host in references)
            {
                if (host == null)
                {
                    Add(issues, "XUI-HOST-002", "레이어 호스트 참조 없음", runtime, "호스트 목록의 빈 참조를 수정하세요.");
                    continue;
                }

                hosts.Add(host);
                if (!assigned.Add(host))
                {
                    Add(issues, "XUI-HOST-003", "레이어 호스트 중복 참조", host, "하나의 호스트가 중복 등록됐습니다.");
                }

                if (host.transform.root != runtime.transform)
                {
                    Add(issues, "XUI-HOST-004", "다른 루트의 레이어 호스트", host, "런타임과 동일한 루트에 배치하세요.");
                }

                if (string.IsNullOrWhiteSpace(host.LayerID) || !ids.Add(host.LayerID))
                {
                    Add(issues, "XUI-HOST-005", "레이어 식별자 오류", host, "식별자가 비어 있거나 중복됐습니다.");
                }

                ValidatePlacements(runtime, host, placements, issues);
            }

            ValidatePlan(runtime, hosts, issues);
        }

        private static void ValidatePlacements
        (
            UIRuntime runtime,
            PresentationLayerHost host,
            HashSet<PresentationPlacementHost> placements,
            List<ControlCenterIssue> issues
        )
        {
            foreach (var placement in host.PlacementHosts)
            {
                if (placement == null)
                {
                    Add(issues, "XUI-HOST-006", "배치 호스트 참조 없음", host, "빈 배치 참조를 수정하세요.");
                    continue;
                }

                if (placement.transform.root != runtime.transform)
                {
                    Add(issues, "XUI-HOST-007", "다른 루트의 배치 호스트", placement, "런타임과 동일한 루트에 배치하세요.");
                }

                if (!placements.Add(placement))
                {
                    Add(issues, "XUI-HOST-008", "배치 호스트 중복 참조", placement, "하나의 배치 호스트가 중복 등록됐습니다.");
                }
            }
        }

    #endregion

    #region 런타임 표시 매핑

        private static void ValidatePlan
        (
            UIRuntime runtime, IReadOnlyList<PresentationLayerHost> hosts,
            List<ControlCenterIssue> issues
        )
        {
            if (!runtime.IsInitialized || runtime.Settings?.DefaultLayout == null)
            {
                return;
            }

            try
            {
                var plan = PresentationLayoutResolver.Resolve(runtime.Settings.DefaultLayout);
                var layers = PresentationHostMapResolver.CreateLayerHostMap(plan, hosts);
                PresentationHostMapResolver.CreatePlacementHostMap(plan, layers);
            }
            catch (InvalidOperationException exception)
            {
                Add(issues, "XUI-HOST-011", "호스트와 레이아웃 불일치", runtime, exception.Message);
            }
        }

        private static void Add
        (
            List<ControlCenterIssue> issues, string code, string title,
            UnityEngine.Object target, string detail
        )
        {
            issues.Add(new ControlCenterIssue(code, DiagnosticSeverity.Error, title, detail, target));
        }

    #endregion

    }
}
