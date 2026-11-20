/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriUIDebugSnapshot.cs
수정일 : 2026-11-12
# 설명
Editor와 개발 빌드에서 동일하게 읽는 UI Runtime 진단 결과와 안전 명령을 제공한다.
========================================================================= BLOCK_HEADER_END */

#if UNITY_EDITOR || DEBUG

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.UI.Frame
{
    // ============================================================
    /// <summary>
    /// 하나의 UI 컨텍스트의 진단 상태를 제공한다.
    /// </summary>
    // ============================================================
    public sealed class XeriUIDebugContext
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 컨텍스트 또는 표현 세션의 깊이를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int Depth { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 이 컨텍스트가 입력을 받는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsEffective { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 활성 입력 경로에 포함되는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsOnActivePath { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 포커스 권한을 보유하는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasFocusAuthority { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 표현 레이아웃 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string LayoutName { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 열려 있는 화면 정보를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<string> Screens { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 모달 스택 항목 수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int ModalCount { get; }

        internal XeriUIDebugContext
        (
            int depth, bool isEffective, bool onActivePath,
            bool hasFocusAuthority, string layoutName,
            IReadOnlyList<string> screens, int modalCount
        )
        {
            Depth = depth;
            IsEffective = isEffective;
            IsOnActivePath = onActivePath;
            HasFocusAuthority = hasFocusAuthority;
            LayoutName = layoutName;
            Screens = screens;
            ModalCount = modalCount;
        }
    }

    // ============================================================
    /// <summary>
    /// 표현 세션의 레이아웃과 표시 항목을 제공한다.
    /// </summary>
    // ============================================================
    public sealed class XeriUIDebugPresentation
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 컨텍스트 또는 표현 세션의 깊이를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int Depth { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 표현 레이아웃 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string LayoutName { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 표현 세션의 레이어 정보를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<string> Layers { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 표현 세션의 배치 정보를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<string> Placements { get; }

        internal XeriUIDebugPresentation
        (
            int depth, string layoutName,
            IReadOnlyList<string> layers, IReadOnlyList<string> placements
        )
        {
            Depth = depth;
            LayoutName = layoutName;
            Layers = layers;
            Placements = placements;
        }
    }

    // ============================================================
    /// <summary>
    /// UI 런타임의 진단 결과를 읽기 전용으로 제공한다.
    /// </summary>
    // ============================================================
    public sealed class XeriUIDebugSnapshot
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 진단 대상이 준비됐는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsAvailable { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 런타임 상태를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string State { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 사용 중인 설정 에셋 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string SettingsName { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 표현 레이아웃 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string LayoutName { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막 입력 장치를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string LastInputDevice { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 자식 표현 세션 수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int ChildPresentationCount { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 컨텍스트 진단 결과를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<XeriUIDebugContext> Contexts { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 표현 세션 진단 결과를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public IReadOnlyList<XeriUIDebugPresentation> Presentations { get; }

        internal XeriUIDebugSnapshot
        (
            bool available, string state, string settings, string layout,
            string inputDevice, int childPresentations,
            IReadOnlyList<XeriUIDebugContext> contexts,
            IReadOnlyList<XeriUIDebugPresentation> presentations
        )
        {
            IsAvailable = available;
            State = state;
            SettingsName = settings;
            LayoutName = layout;
            LastInputDevice = inputDevice;
            ChildPresentationCount = childPresentations;
            Contexts = contexts;
            Presentations = presentations;
        }
    }

    // ============================================================
    /// <summary>
    /// 개발 환경에서 런타임 상태와 명령을 노출한다.
    /// </summary>
    // ============================================================
    public static class XeriUIDiagnostics
    {

    #region 상태 조회와 명령

        // ------------------------------------------------------------
        /// <summary>
        /// 화면과 모달의 변경 기록을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public static IReadOnlyList<XeriUIDebugEvent> ReadEvents()
        {
            XeriUIDebugEvents.Track();
            return XeriUIDebugEvents.Snapshot();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 런타임의 진단 스냅샷을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public static XeriUIDebugSnapshot Capture()
        {
            XeriUIDebugEvents.Track();
            if (!UIRuntime.TryCurrent(out var runtime) || runtime == null)
            {
                return Empty("실행 중인 UI Runtime 없음");
            }

            if (!runtime.IsInitialized || runtime.Main == null)
            {
                return Empty("UI Runtime 초기화 전");
            }

            var contexts = new List<XeriUIDebugContext>();
            CaptureContext(runtime.Main, 0, contexts);
            var presentations = new List<XeriUIDebugPresentation>();
            CapturePresentation(runtime.RootPresentation, 0, presentations);
            var settings = runtime.Settings;
            return new XeriUIDebugSnapshot
            (
                true, "실행 중",
                settings != null ? settings.name : "없음",
                settings != null && settings.DefaultLayout != null
                    ? settings.DefaultLayout.name : "없음",
                runtime.LastInputDevice != null
                    ? runtime.LastInputDevice.displayName : "없음",
                runtime.RootPresentation != null
                    ? runtime.RootPresentation.ChildCount : 0,
                contexts, presentations
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 입력 권한 컨텍스트의 최상단 화면을 닫는다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryCloseTopScreen()
        {
            if (!UIRuntime.TryCurrent(out var runtime) ||
                runtime == null || !runtime.IsInitialized || runtime.Main == null)
            {
                return false;
            }

            var context = FindEffective(runtime.Main);
            if (context == null || !context.Screens.IsAvailable || context.Screens.Count == 0)
            {
                return false;
            }

            return context.Screens.Close();
        }

    #endregion

    #region 컨텍스트 추적

        private static UIContext FindEffective(UIContext context)
        {
            if (context.IsDisposed || context.IsDisposing)
            {
                return null;
            }

            foreach (var child in context.DebugChildren)
            {
                var found = FindEffective(child);
                if (found != null)
                {
                    return found;
                }
            }

            return context.IsEffective ? context : null;
        }

        private static void CaptureContext
        (
            UIContext context, int depth, List<XeriUIDebugContext> output
        )
        {
            if (context == null || context.IsDisposed || context.IsDisposing)
            {
                return;
            }

            var screens = new List<string>();
            foreach (var session in context.Screens.DebugStack)
            {
                if (session != null)
                {
                    screens.Add(session.ID + " · " + session.State);
                }
            }

            var sessionLayout = context.Presentation?.DebugLayout;
            var inheritsPresentation = context.Parent != null &&
                ReferenceEquals(context.Presentation, context.Parent.Presentation);
            output.Add(new XeriUIDebugContext
            (
                depth, context.IsEffective, context.IsOnActivePath,
                context.HasFocusAuthority,
                inheritsPresentation ? "부모 표현 공유" :
                    sessionLayout != null ? sessionLayout.name : "원본 미확인",
                screens, context.Modals.Count
            ));

            foreach (var child in context.DebugChildren)
            {
                CaptureContext(child, depth + 1, output);
            }
        }

    #endregion

    #region 표현 계층 조회

        private static void CapturePresentation
        (
            PresentationSession session, int depth,
            List<XeriUIDebugPresentation> output
        )
        {
            if (session == null || session.IsDisposed)
            {
                return;
            }

            var layers = new List<string>();
            foreach (var layer in session.Plan.Layers)
            {
                var name = string.IsNullOrWhiteSpace(layer.DisplayName)
                    ? layer.LayerID : layer.DisplayName;
                layers.Add
                (
                    name + " · " + layer.LayerID + " · " +
                    layer.Backend + " · " + layer.LayerOrder
                );
            }

            var placements = new List<string>();
            foreach (var placement in session.Plan.Placements)
            {
                placements.Add
                (
                    placement.PresentationID + " → " +
                    placement.Layer.LayerID + " · " + placement.LocalOrder
                );
            }

            output.Add(new XeriUIDebugPresentation
            (
                depth,
                session.DebugLayout != null ? session.DebugLayout.name : "원본 미확인",
                layers, placements
            ));
            foreach (var child in session.DebugChildren)
            {
                CapturePresentation(child, depth + 1, output);
            }
        }

        private static XeriUIDebugSnapshot Empty(string state)
        {
            return new XeriUIDebugSnapshot
            (
                false, state, "", "", "", 0,
                Array.Empty<XeriUIDebugContext>(),
                Array.Empty<XeriUIDebugPresentation>()
            );
        }

    #endregion

    }
}

#endif