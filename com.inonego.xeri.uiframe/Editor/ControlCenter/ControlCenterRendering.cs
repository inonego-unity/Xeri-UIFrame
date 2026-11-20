/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterRendering.cs
수정일 : 2026-11-12
# 설명
조건부 파이프라인 Editor 기능을 메인 Editor 어셈블리에 노출한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

namespace inonego.Xeri.UI.Frame.Editor
{
    // ============================================================
    /// <summary>
    /// 렌더 파이프라인별 출력 설치 도구를 통합 관리 창에 연결한다.
    /// </summary>
    // ============================================================
    public interface IControlCenterRendering
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 렌더 파이프라인의 표시 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        string DisplayName { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 프로젝트에서 지원되는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool IsAvailable { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 설정 에셋이 필요한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        bool RequiresSettings { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 설치 변경 내역을 미리 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        string Preview(UISettingsAsset settings);

        // ------------------------------------------------------------
        /// <summary>
        /// 출력 설정을 설치한다.
        /// </summary>
        // ------------------------------------------------------------
        void Install(UISettingsAsset settings);

        // ------------------------------------------------------------
        /// <summary>
        /// 설치된 출력 설정을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        void Uninstall();
    }

    internal static class ControlCenterRendering
    {
        internal static IReadOnlyList<IControlCenterRendering> Find()
        {
            var result = new List<IControlCenterRendering>();

            foreach (var type in TypeCache.GetTypesDerivedFrom<IControlCenterRendering>())
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                try
                {
                    var integration = Activator.CreateInstance(type) as IControlCenterRendering;
                    if (integration != null && integration.IsAvailable)
                    {
                        result.Add(integration);
                    }
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }
            }

            result.Sort
            (
                (left, right) =>
                string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal)
            );
            return result;
        }
    }
}