/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : HDRPControlCenterRendering.cs
수정일 : 2026-11-12
# 설명
HDRP 출력 설치 기능을 관리 도구에 연결한다.
========================================================================= BLOCK_HEADER_END */

using System;
using inonego.Xeri.UI.Frame;

namespace inonego.Xeri.UI.HDRP.Editor
{
    // ============================================================
    /// <summary>
    /// HDRP 출력 설치 도구를 통합 관리 창에 연결한다.
    /// </summary>
    // ============================================================
    public sealed class HDRPControlCenterRendering : inonego.Xeri.UI.Editor.IControlCenterRendering
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 렌더 파이프라인 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string DisplayName => "HDRP";

        // ------------------------------------------------------------
        /// <summary>
        /// HDRP 출력 도구의 사용 가능 여부를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsAvailable => HDRPNativeOutputInstaller.FindProjectPipelineAssets().Count > 0;

        // ------------------------------------------------------------
        /// <summary>
        /// 설정 에셋이 필요한지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool RequiresSettings => false;

        // ------------------------------------------------------------
        /// <summary>
        /// 출력 설정의 변경 내용을 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Preview(UISettingsAsset settings)
        {
            return HDRPNativeOutputInstaller.Preview();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 HDRP 설치기를 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Install(UISettingsAsset settings)
        {
            HDRPNativeOutputInstaller.Install();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기존 HDRP 제거기를 실행한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Uninstall()
        {
            HDRPNativeOutputInstaller.Uninstall();
        }
    }
}