/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : URPControlCenterRendering.cs
수정일 : 2026-11-12
# 설명
기존 URP 출력 설치 기능을 통합 관리 도구에 연결한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.URP.Editor
{
    // ============================================================
    /// <summary>
    /// URP 출력 설치 도구를 통합 관리 창에 연결한다.
    /// </summary>
    // ============================================================
    public sealed class URPControlCenterRendering : inonego.Xeri.UI.Editor.IControlCenterRendering
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 렌더 파이프라인 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string DisplayName => "URP";

        // ------------------------------------------------------------
        /// <summary>
        /// URP 출력 도구의 사용 가능 여부를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsAvailable => URPDirectOutputInstaller.FindProjectPipelineAssets().Count > 0;

        // ------------------------------------------------------------
        /// <summary>
        /// 설정 에셋이 필요한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool RequiresSettings => true;

        // ------------------------------------------------------------
        /// <summary>
        /// 출력 설정의 변경 내용을 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Preview(UISettingsAsset settings)
        {
            return URPDirectOutputInstaller.Preview(settings);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 URP 설치기를 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Install(UISettingsAsset settings)
        {
            URPDirectOutputInstaller.InstallProject(settings);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 URP 제거기를 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Uninstall()
        {
            URPDirectOutputInstaller.UninstallProject();
        }
    }
}