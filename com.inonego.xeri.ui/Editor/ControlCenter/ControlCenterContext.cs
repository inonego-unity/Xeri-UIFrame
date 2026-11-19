/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterContext.cs
수정일 : 2026-11-17

# 설명
관리 도구의 선택 상태, 에셋·씬 목록, 검사 결과를 소유하고 변경을 화면에 알린다.

# 특이사항, 제약사항
직렬화 상태(ControlCenterState)는 창이 소유해 Domain Reload 뒤에도 유지한다.
여러 설정 에셋이 있으면 임의로 선택하지 않고, 프로젝트 설정이 하나뿐일 때만 자동 선택한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.UI.Editor
{
    internal enum ControlCenterTab { Overview, Settings, Composition, Diagnostics, Live }

    internal enum ControlCenterValidationState { NotRun, Done, Stale, Failed }

    // ============================================================
    /// <summary>
    /// 화면이 다시 그려야 할 변경 범위를 나타낸다.
    /// </summary>
    // ============================================================
    [Flags]
    internal enum ControlCenterChange
    {
        None = 0,
        Selection = 1 << 0,
        Catalog = 1 << 1,
        Scenes = 1 << 2,
        Validation = 1 << 3,
        PlayMode = 1 << 4,
        View = 1 << 5,
        Bootstrapper = 1 << 6,
        All = Selection | Catalog | Scenes | Validation | PlayMode | View | Bootstrapper,
    }

    // ============================================================
    /// <summary>
    /// 창과 함께 직렬화되는 관리 도구의 선택 상태다.
    /// </summary>
    // ============================================================
    [Serializable]
    internal sealed class ControlCenterState
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 선택된 UI 설정 에셋이다.
        /// </summary>
        // ------------------------------------------------------------
        public UISettingsAsset Settings;

        // ------------------------------------------------------------
        /// <summary>
        /// 사용자가 설정 선택을 직접 고른 적이 있는지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool SettingsChosen;

        // ------------------------------------------------------------
        /// <summary>
        /// 선택된 표시 레이아웃 에셋이다.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationLayout Layout;

        // ------------------------------------------------------------
        /// <summary>
        /// 선택된 관리 도구 탭이다.
        /// </summary>
        // ------------------------------------------------------------
        public ControlCenterTab Tab = ControlCenterTab.Overview;

        // ------------------------------------------------------------
        /// <summary>
        /// 설정 탭에서 선택된 분류 번호다.
        /// </summary>
        // ------------------------------------------------------------
        public int SettingsCategory;

        // ------------------------------------------------------------
        /// <summary>
        /// 구성 탭에서 씬 화면을 보일지 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public bool ShowScenes;
    }

    internal sealed class ControlCenterContext
    {

    #region 필드

        internal ControlCenterState State => state;
        private readonly ControlCenterState state;
        internal UISettingsAsset Settings => state.Settings;
        internal PresentationLayout Layout => state.Layout;
        internal IReadOnlyList<UISettingsAsset> SettingsAssets => settingsAssets;
        private readonly List<UISettingsAsset> settingsAssets = new();
        internal IReadOnlyList<PresentationLayout> Layouts => layouts;
        private readonly List<PresentationLayout> layouts = new();
        internal IReadOnlyList<ControlCenterSceneEntry> SceneEntries => sceneEntries;
        private readonly List<ControlCenterSceneEntry> sceneEntries = new();
        internal IReadOnlyList<ControlCenterIssue> Issues => issues;
        private readonly List<ControlCenterIssue> issues = new();
        internal ControlCenterValidationState ValidationState { get; private set; }
        internal string ValidationFailure { get; private set; }
        internal string ValidationScope { get; private set; }

    #endregion

    #region 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 선택·목록·검사 상태가 바뀌면 변경 범위와 함께 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        internal event Action<ControlCenterChange> Changed;

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 탭으로 이동해야 할 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        internal event Action<ControlCenterTab> TabRequested;

        // ------------------------------------------------------------
        /// <summary>
        /// 씬 항목을 화면에서 선택해야 할 때 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        internal event Action<ControlCenterSceneEntry> SceneEntryRequested;

    #endregion

    #region 생성자

        internal ControlCenterContext(ControlCenterState state)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
        }

    #endregion

    #region 선택

        internal void SelectSettings(UISettingsAsset settings)
        {
            // 사용자가 직접 고른 뒤에는(해제 포함) 자동 선택하지 않는다.
            state.SettingsChosen = true;
            if (state.Settings == settings)
            {
                return;
            }

            state.Settings = settings;

            // 레이아웃을 고른 적이 없으면 설정의 기본 레이아웃부터 보여준다.
            if (state.Layout == null && settings != null)
            {
                state.Layout = settings.DefaultLayout;
            }

            MarkValidationStale();
            Changed?.Invoke(ControlCenterChange.Selection);
        }

        internal void SelectLayout(PresentationLayout layout)
        {
            if (state.Layout == layout)
            {
                return;
            }

            state.Layout = layout;
            MarkValidationStale();
            Changed?.Invoke(ControlCenterChange.Selection);
        }

        internal void ShowTab(ControlCenterTab tab)
        {
            TabRequested?.Invoke(tab);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 설정 탭의 분류를 바꿔 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void ShowSettingsCategory(int category)
        {
            state.SettingsCategory = category;
            Changed?.Invoke(ControlCenterChange.View);
            ShowTab(ControlCenterTab.Settings);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 구성 탭을 레이아웃 또는 씬 화면으로 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void ShowComposition(bool scenes)
        {
            state.ShowScenes = scenes;
            Changed?.Invoke(ControlCenterChange.View);
            ShowTab(ControlCenterTab.Composition);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 대상의 종류에 맞는 탭과 항목으로 이동한다.
        /// <br/> 관리 도구가 다루지 않는 대상은 Unity 선택과 Ping만 수행한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Navigate(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            switch (target)
            {
                case UISettingsAsset settings:
                    {
                        SelectSettings(settings);
                        ShowTab(ControlCenterTab.Settings);
                        return;
                    }

                case PresentationLayout layout:
                    {
                        SelectLayout(layout);
                        ShowComposition(false);
                        return;
                    }
            }

            var entry = sceneEntries.Find(item => item.Component == target);
            if (entry != null)
            {
                ShowComposition(true);
                SceneEntryRequested?.Invoke(entry);
            }

            Reveal(target);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Unity 선택을 대상으로 바꾸고 프로젝트 창이나 씬 뷰에서 보여준다.
        /// </summary>
        // ------------------------------------------------------------
        internal static void Reveal(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
            if (target is Component component && component.gameObject.scene.IsValid())
            {
                SceneView.FrameLastActiveSceneView();
            }
        }

    #endregion

    #region 목록 갱신

        // ------------------------------------------------------------
        /// <summary>
        /// 프로젝트의 설정·레이아웃 에셋을 다시 찾고 삭제된 선택을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void RefreshCatalog()
        {
            settingsAssets.Clear();
            settingsAssets.AddRange(ControlCenterAssets.FindAll<UISettingsAsset>());
            layouts.Clear();
            layouts.AddRange(ControlCenterAssets.FindAll<PresentationLayout>());

            // 에셋이 삭제되면 Unity 참조는 fake null이 되므로 실제 null로 정리한다.
            if (state.Settings == null)
            {
                state.Settings = null;
            }

            if (state.Layout == null)
            {
                state.Layout = null;
            }

            // 사용자가 고르지 않았고 프로젝트 설정이 하나뿐일 때만 선택 대상이 분명하다.
            if (state.Settings == null && !state.SettingsChosen)
            {
                UISettingsAsset only = null;
                var projectCount = 0;
                foreach (var asset in settingsAssets)
                {
                    if (!ControlCenterAssets.IsPackageAsset(asset))
                    {
                        only = asset;
                        projectCount++;
                    }
                }

                if (projectCount == 1)
                {
                    state.Settings = only;
                }
            }

            if (state.Layout == null && state.Settings != null)
            {
                state.Layout = state.Settings.DefaultLayout;
            }

            Changed?.Invoke(ControlCenterChange.Catalog | ControlCenterChange.Selection);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 새 UI 설정 에셋을 만들고 목록을 갱신해 선택한다. 저장 위치 선택을 취소하면 아무것도 하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        internal void CreateSettings()
        {
            var asset = ControlCenterAssets.Create<UISettingsAsset>("UI Settings");
            if (asset == null)
            {
                return;
            }

            RefreshCatalog();
            SelectSettings(asset);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 새 레이아웃 에셋을 만들고 목록을 갱신해 선택한다. 저장 위치 선택을 취소하면 아무것도 하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        internal void CreateLayout()
        {
            var layout = ControlCenterAssets.Create<PresentationLayout>("Presentation Layout");
            if (layout == null)
            {
                return;
            }

            RefreshCatalog();
            SelectLayout(layout);
        }

        internal void RefreshScenes()
        {
            sceneEntries.Clear();
            sceneEntries.AddRange(ControlCenterScenes.Collect());
            Changed?.Invoke(ControlCenterChange.Scenes);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 프로젝트 Bootstrapper 설정이나 UI 초기화 모듈이 바뀌었음을 화면에 알린다.
        /// </summary>
        // ------------------------------------------------------------
        internal void NotifyBootstrapperChanged()
        {
            Changed?.Invoke(ControlCenterChange.Bootstrapper);
        }

        internal void NotifyPlayModeChanged()
        {
            Changed?.Invoke(ControlCenterChange.PlayMode);
        }

    #endregion

    #region 검사

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 설정, 관련 레이아웃과 로드된 씬을 검사한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void RunValidation()
        {
            RefreshScenes();
            issues.Clear();
            ValidationScope = DescribeScope();
            try
            {
                // 심각도 순으로 정렬하되 같은 심각도는 검사 순서를 유지한다.
                var found = ControlCenterValidation.Run(state.Settings, state.Layout, sceneEntries);
                issues.AddRange(found.OrderByDescending(issue => issue.Severity));
                ValidationFailure = null;
                ValidationState = ControlCenterValidationState.Done;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                issues.Clear();
                ValidationFailure = exception.Message;
                ValidationState = ControlCenterValidationState.Failed;
            }

            Changed?.Invoke(ControlCenterChange.Validation);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 검사 대상이 바뀌면 이전 결과를 재검사 필요 상태로 바꾼다.
        /// </summary>
        // ------------------------------------------------------------
        internal void MarkValidationStale()
        {
            if (ValidationState != ControlCenterValidationState.Done
                && ValidationState != ControlCenterValidationState.Failed)
            {
                return;
            }

            ValidationState = ControlCenterValidationState.Stale;
            Changed?.Invoke(ControlCenterChange.Validation);
        }

        internal int CountIssues(DiagnosticSeverity severity)
        {
            var count = 0;
            foreach (var issue in issues)
            {
                if (issue.Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 검사 대상이 하나 이상 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal bool HasValidationTarget => state.Settings != null || state.Layout != null
            || sceneEntries.Count > 0;

        private string DescribeScope()
        {
            var parts = new List<string>();
            if (state.Settings != null)
            {
                parts.Add("설정 " + state.Settings.name);
            }

            var defaultLayout = state.Settings != null ? state.Settings.DefaultLayout : null;
            if (defaultLayout != null)
            {
                parts.Add("기본 레이아웃 " + defaultLayout.name);
            }

            if (state.Layout != null && state.Layout != defaultLayout)
            {
                parts.Add("레이아웃 " + state.Layout.name);
            }

            parts.Add("로드된 씬 구성 요소 " + sceneEntries.Count + "개");
            return string.Join(" · ", parts);
        }

    #endregion

    }
}
