/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriUIControlCenterWindow.cs
수정일 : 2026-11-17

# 설명
Xeri UI 관리 도구 창의 탭, 설정 선택 메뉴, 검사 상태 표시와 Editor 이벤트 연결을 담당한다.

# 특이사항, 제약사항
탭별 화면은 ControlCenterPage가 소유하고 창은 변경을 전달만 한다.
선택 상태는 창에 직렬화해 도킹, 창 크기 변경과 Domain Reload 뒤에도 유지한다.
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
using inonego.Xeri.Bootstrapper;

namespace inonego.Xeri.UI.Editor
{
    // ============================================================
    /// <summary>
    /// Xeri UI의 설정·구성·진단·실행 상태를 한 창에서 관리한다.
    /// </summary>
    // ============================================================
    public sealed class XeriUIControlCenterWindow : EditorWindow
    {

    #region 필드

        private const string menuPath = "Tools/Xeri UI/관리 도구 열기";
        private const float compactWidth = 640f;
        private static readonly Vector2 defaultSize = new(1280f, 800f);

        [SerializeField] private ControlCenterState state = new();
        private ControlCenterContext context;
        private readonly List<ControlCenterPage> pages = new();
        private readonly Dictionary<ControlCenterTab, ToolbarButton> tabButtons = new();
        private VisualElement root;
        private ToolbarMenu settingsMenu;
        private ToolbarButton validation;
        private SeverityIndicator validationIcon;
        private Label validationLabel;
        private ControlCenterChange queuedRefresh;

    #endregion

    #region 창과 수명

        // ------------------------------------------------------------
        /// <summary>
        /// 관리 도구 창을 연다.
        /// </summary>
        // ------------------------------------------------------------
        [MenuItem(menuPath)]
        public static void Open()
        {
            var opened = HasOpenInstances<XeriUIControlCenterWindow>();
            var window = GetWindow<XeriUIControlCenterWindow>();
            window.titleContent = new GUIContent("Xeri UI 관리 도구");
            window.minSize = new Vector2(480f, 320f);

            // 처음 열 때는 목록과 표를 함께 볼 수 있는 넓은 크기로 에디터 가운데에 둔다.
            if (!opened)
            {
                var main = EditorGUIUtility.GetMainWindowPosition();
                var width = Mathf.Min(defaultSize.x, main.width - 40f);
                var height = Mathf.Min(defaultSize.y, main.height - 40f);
                window.position = new Rect
                (
                    main.x + (main.width - width) * 0.5f, main.y + (main.height - height) * 0.5f, width, height
                );
            }

            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.projectChanged += HandleProjectChanged;
            EditorApplication.hierarchyChanged += HandleHierarchyChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
            EditorApplication.update += HandleUpdate;
            Undo.undoRedoPerformed += HandleUndoRedo;
            BootstrapperSettings.OnModulesChange += HandleBootstrapperSettingsChanged;
            BootstrapperModuleAsset.OnChange += HandleBootstrapperModuleChanged;
            Undo.postprocessModifications += HandleModifications;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= HandleProjectChanged;
            EditorApplication.hierarchyChanged -= HandleHierarchyChanged;
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            EditorApplication.update -= HandleUpdate;
            Undo.undoRedoPerformed -= HandleUndoRedo;
            BootstrapperSettings.OnModulesChange -= HandleBootstrapperSettingsChanged;
            BootstrapperModuleAsset.OnChange -= HandleBootstrapperModuleChanged;
            Undo.postprocessModifications -= HandleModifications;
            context = null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// UXML 화면과 탭별 화면을 구성하고 현재 상태를 표시한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            pages.Clear();
            tabButtons.Clear();
            try
            {
                BuildShell();
            }
            catch (InvalidOperationException exception)
            {
                rootVisualElement.Clear();
                rootVisualElement.Add(new HelpBox(exception.Message, HelpBoxMessageType.Error));
                context = null;
                return;
            }

            // 선택이 비어 있을 때만 프로젝트에서 선택한 설정이나 레이아웃을 이어받는다.
            if (state.Settings == null && Selection.activeObject is UISettingsAsset selectedSettings)
            {
                state.Settings = selectedSettings;
                state.SettingsChosen = true;
            }

            if (state.Layout == null && Selection.activeObject is PresentationLayout selectedLayout)
            {
                state.Layout = selectedLayout;
            }

            context.RefreshCatalog();
            context.RefreshScenes();
            ShowTab(state.Tab);
        }

        private void BuildShell()
        {
            ControlCenterElements.CloneLayout("Shell", rootVisualElement);
            root = ControlCenterElements.Require<VisualElement>(rootVisualElement, "control-center");
            root.styleSheets.Add(ControlCenterElements.LoadStyle());
            root.styleSheets.Add(ControlCenterElements.LoadRowStyle());
            root.EnableInClassList("xeri-cc--light", !EditorGUIUtility.isProSkin);
            root.RegisterCallback<GeometryChangedEvent>(HandleGeometryChanged);

            context = new ControlCenterContext(state);
            context.Changed += HandleContextChanged;
            context.TabRequested += ShowTab;

            pages.Add(new ControlCenterOverviewPage(context));
            pages.Add(new ControlCenterSettingsPage(context));
            pages.Add(new ControlCenterCompositionPage(context));
            pages.Add(new ControlCenterDiagnosticsPage(context));
            pages.Add(new ControlCenterLivePage(context));

            var tabs = ControlCenterElements.Require<VisualElement>(root, "control-center-tabs");
            var pageHost = ControlCenterElements.Require<VisualElement>(root, "control-center-pages");
            foreach (var page in pages)
            {
                var tab = page.Tab;
                var button = new ToolbarButton(() => ShowTab(tab))
                {
                    text = page.Title,
                    tooltip = page.Title
                };
                button.iconImage = XeriIcon.LoadBackground(page.IconName);
                button.AddToClassList("xeri-cc__tab");
                tabs.Add(button);
                tabButtons.Add(tab, button);
                pageHost.Add(page.Root);
                page.SetVisible(false);
            }

            settingsMenu = ControlCenterElements.Require<ToolbarMenu>(root, "control-center-settings");
            validation = ControlCenterElements.Require<ToolbarButton>(root, "control-center-validation");
            validationIcon = new SeverityIndicator();
            validationIcon.AddToClassList("xeri-cc__validation-icon");
            validationLabel = new Label();
            validationLabel.AddToClassList("xeri-cc__validation-text");
            validation.Add(validationIcon);
            validation.Add(validationLabel);
            validation.tooltip = "검사 실행 후 진단 탭으로 이동";
            validation.clicked += () =>
            {
                context.RunValidation();
                ShowTab(ControlCenterTab.Diagnostics);
            };
        }

    #endregion

    #region 탭과 도구 모음

        private void ShowTab(ControlCenterTab tab)
        {
            state.Tab = tab;
            foreach (var page in pages)
            {
                page.SetVisible(page.Tab == tab);
                tabButtons[page.Tab].EnableInClassList("xeri-cc__tab--active", page.Tab == tab);
            }
        }

        private void HandleContextChanged(ControlCenterChange change)
        {
            if ((change & (ControlCenterChange.Selection | ControlCenterChange.Catalog)) != 0)
            {
                RebuildSettingsMenu();
            }

            RefreshValidationStatus();
            foreach (var page in pages)
            {
                page.Notify(change);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 설정 메뉴에 프로젝트·패키지 설정과 생성·해제 명령을 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RebuildSettingsMenu()
        {
            var settings = context.Settings;
            settingsMenu.text = settings != null ? settings.name : "설정 선택";
            settingsMenu.tooltip = settings != null
                ? "관리 중인 UI 설정 · " + AssetDatabase.GetAssetPath(settings)
                : "관리할 UI 설정 에셋";

            var menu = settingsMenu.menu;
            menu.ClearItems();
            foreach (var asset in context.SettingsAssets)
            {
                var choice = asset;
                menu.AppendAction
                (
                    asset.name + "  (" + ControlCenterAssets.DescribeLocation(asset) + ")",
                    _ => context.SelectSettings(choice),
                    _ => choice == context.Settings ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
                );
            }

            if (context.SettingsAssets.Count > 0)
            {
                menu.AppendSeparator();
            }

            menu.AppendAction("새 설정 만들기", _ => context.CreateSettings());
            menu.AppendAction
            (
                "선택 해제", _ => context.SelectSettings(null),
                _ => context.Settings != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled
            );
        }

        private void RefreshValidationStatus()
        {
            DiagnosticSeverity? severity = null;
            string text;
            switch (context.ValidationState)
            {
                case ControlCenterValidationState.NotRun:
                    {
                        text = "검사 전";
                        break;
                    }

                case ControlCenterValidationState.Stale:
                    {
                        text = "재검사 필요";
                        severity = DiagnosticSeverity.Info;
                        break;
                    }

                case ControlCenterValidationState.Failed:
                    {
                        text = "검사 실패";
                        severity = DiagnosticSeverity.Error;
                        break;
                    }

                default:
                    {
                        var errors = context.CountIssues(DiagnosticSeverity.Error);
                        var warnings = context.CountIssues(DiagnosticSeverity.Warning);
                        text = errors + warnings == 0 ? "문제 없음" : "오류 " + errors + " · 경고 " + warnings;
                        severity = errors > 0 ? DiagnosticSeverity.Error
                            : warnings > 0 ? DiagnosticSeverity.Warning : null;
                        break;
                    }
            }

            validationIcon.SetSeverity(severity);
            validationLabel.text = text;

            // 결과 상태에 따라 칩의 테두리 색을 바꾼다.
            var done = context.ValidationState == ControlCenterValidationState.Done;
            validation.EnableInClassList("xeri-cc__validation--error", severity == DiagnosticSeverity.Error);
            validation.EnableInClassList("xeri-cc__validation--warning", severity == DiagnosticSeverity.Warning);
            validation.EnableInClassList("xeri-cc__validation--ok", done && severity == null);
        }

        private void HandleGeometryChanged(GeometryChangedEvent evt)
        {
            // 좁은 창에서는 탭을 아이콘만 남기고 좌우 분할을 위아래로 바꾼다.
            var compact = evt.newRect.width < compactWidth;
            root.EnableInClassList("xeri-cc--compact", compact);
            foreach (var page in pages)
            {
                tabButtons[page.Tab].text = compact ? string.Empty : page.Title;
                page.SetCompact(compact);
            }
        }

    #endregion

    #region Editor 이벤트

        private void HandleProjectChanged()
        {
            if (context == null)
            {
                return;
            }

            context.RefreshCatalog();
            context.MarkValidationStale();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 계층 변경이 연속으로 와도 다음 프레임에 한 번만 씬을 다시 수집한다.
        /// </summary>
        // ------------------------------------------------------------
        private void HandleHierarchyChanged()
        {
            QueueRefresh(ControlCenterChange.Scenes);
        }

        private void HandlePlayModeChanged(PlayModeStateChange change)
        {
            if (context == null)
            {
                return;
            }

            context.RefreshScenes();
            context.MarkValidationStale();
            context.NotifyPlayModeChanged();
        }

        private void HandleUndoRedo()
        {
            context?.MarkValidationStale();
        }

        private void HandleBootstrapperSettingsChanged(BootstrapperSettings settings)
        {
            QueueRefresh(ControlCenterChange.Bootstrapper);
        }

        private void HandleBootstrapperModuleChanged(BootstrapperModuleAsset module)
        {
            QueueRefresh(ControlCenterChange.Bootstrapper);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 계층 변경과 Bootstrapper 변경은 연달아 오므로 다음 프레임에 한 번만 모아서 처리한다.
        /// <br/> 편집 기록과 에셋 로드 때도 이벤트가 겹쳐 오기 때문이다.
        /// </summary>
        // ------------------------------------------------------------
        private void QueueRefresh(ControlCenterChange change)
        {
            if (context == null)
            {
                return;
            }

            var idle = queuedRefresh == ControlCenterChange.None;
            queuedRefresh |= change;
            if (idle)
            {
                rootVisualElement.schedule.Execute(FlushQueuedRefresh);
            }
        }

        private void FlushQueuedRefresh()
        {
            var change = queuedRefresh;
            queuedRefresh = ControlCenterChange.None;
            if (context == null)
            {
                return;
            }

            if ((change & ControlCenterChange.Scenes) != 0)
            {
                context.RefreshScenes();
                context.MarkValidationStale();
            }

            if ((change & ControlCenterChange.Bootstrapper) != 0)
            {
                context.NotifyBootstrapperChanged();
            }
        }

        private UndoPropertyModification[] HandleModifications(UndoPropertyModification[] modifications)
        {
            if (context == null)
            {
                return modifications;
            }

            foreach (var modification in modifications)
            {
                var target = modification.currentValue?.target;
                if (target != null && (target == context.Settings || target == context.Layout
                    || target is UIRuntime || target is PresentationLayerHost || target is PresentationPlacementHost))
                {
                    context.MarkValidationStale();
                    break;
                }
            }


            return modifications;
        }

        private void HandleUpdate()
        {
            if (context == null)
            {
                return;
            }

            foreach (var page in pages)
            {
                if (page.Tab == state.Tab)
                {
                    page.Tick();
                }
            }
        }

    #endregion

    }
}
