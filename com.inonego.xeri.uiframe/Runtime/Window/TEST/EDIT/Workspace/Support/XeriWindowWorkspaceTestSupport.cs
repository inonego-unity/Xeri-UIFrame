/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriWindowWorkspaceTestSupport.cs
수정일 : 2026-11-10

# 설명
Window Workspace 통합 테스트가 공유하는 test double, runtime fixture와 Application helper를 제공한다.

테스트 대역과 Workspace fixture, Application helper를 선언 순서에 따라 구분한다.

# 테스트 구성
 A–F: 테스트 대역
 G: Workspace 테스트 환경
 H: 공용 테스트 지원
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.UI;
using inonego.Xeri.UI.TEST;
using inonego.Xeri.UI.TEST.Core;
using inonego.Xeri.UI.Window;
using inonego.Xeri.UI.Frame;

namespace inonego.Xeri.UI.TEST.Window
{
    #region A: 테스트 대역: 창 Driver

    internal sealed class TestWindowDriver : IXeriWindowDriver
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 UI에 적용된 알파 상태를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationAlpha Alpha { get; } = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 UI의 표시 상태를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationVisibility Visibility { get; } = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 창의 위치를 반환하거나 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 Pos { get; set; } = Vector2.zero;

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 창의 크기를 반환하거나 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        public Vector2 Size { get; set; } = new Vector2(200f, 120f);

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 창의 상태를 반환하거나 설정한다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriWindowState State { get; set; } = XeriWindowState.Normal;

        // ------------------------------------------------------------
        /// <summary>
        /// 최대화 영역 적용 여부를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool MaximizedApplied { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 창의 위치와 크기 영역을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public Rect Bounds
        {
            get => new Rect(Pos, Size);
            set
            {
                Pos = value.position;
                Size = value.size;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 창의 확정 상태를 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        public void CommitState(XeriWindowState state)
        {
            State = state;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 창의 시각적 상태를 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ApplyVisualState(XeriWindowState state)
        {
            // NONE
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 창의 위치와 크기를 반영한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ApplyBounds(Rect bounds)
        {
            Bounds = bounds;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 창의 최대화 영역을 적용한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ApplyMaximizedBounds()
        {
            MaximizedApplied = true;
        }
    }

    #endregion

    #region B: 테스트 대역: Focus Driver

    internal sealed class TestFocusDriver : FocusDriverBehaviour
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 테스트에서 현재 선택된 포커스 요소를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override object Current => current;

        private object current = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 선택을 복원할 대체 요소를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public object Fallback { get; } = new object();

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 요소를 포커스로 선택할 수 있는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool CanSelect(object target)
        {
            return target != null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 포커스 또는 표시 대상의 유효성을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool IsValid(object target)
        {
            return target != null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정 요소를 현재 포커스로 선택한다.
        /// </summary>
        // ------------------------------------------------------------
        public override void Select(object target)
        {
            current = target;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택할 수 있는 대체 포커스 대상을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override object FindFallback()
        {
            return Fallback;
        }
    }

    #endregion

    #region C: 테스트 대역: UI Session

    internal sealed class TestSession : IXeriUISession
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 세션 데이터 버전을 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public int Version { get; set; }
    }

    #endregion

    #region D: 테스트 대역: Application Screen과 Focus Scope

    internal sealed class TestApplicationScreenSource : IScreenSource
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 화면의 기본 포커스 대상을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement DefaultFocus { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 보조 포커스 대상을 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement SecondaryFocus { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 자원 반환 횟수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int ReleaseCount { get; private set; }

        private VisualElement root = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트용 화면 자원을 획득하고 횟수를 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        public ScreenInstance Acquire(ScreenViewScope scope)
        {
            if (scope.Layer is not IPresentationLayerDriver<VisualElement> layer)
            {
                throw new InvalidOperationException("Application Window Screen Layer가 UITK Root를 제공하지 않습니다.");
            }

            root = new VisualElement
            {
                name = "Application Screen",
            };
            DefaultFocus = new Button
            {
                name = "Application Default Focus",
            };
            SecondaryFocus = new Button
            {
                name = "Application Secondary Focus",
            };
            root.Add(DefaultFocus);
            root.Add(SecondaryFocus);
            layer.Root.Add(root);
            return new ScreenInstance(new UITKScreenDriver(root, DefaultFocus));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트용 화면 자원을 반환하고 횟수를 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Release(ScreenInstance instance)
        {
            ReleaseCount++;
            root?.RemoveFromHierarchy();
            root = null;
            DefaultFocus = null;
            SecondaryFocus = null;
        }
    }

    internal sealed class TestVisualFocusScope : IFocusScope
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 화면의 기본 포커스 대상을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public object DefaultFocus { get; }

        private readonly VisualElement root = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 시각 요소 기반 테스트 포커스 범위를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TestVisualFocusScope(VisualElement root, object defaultFocus)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            DefaultFocus = defaultFocus;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 포커스 범위가 지정 요소를 포함하는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool ContainsFocus(object target)
        {
            if (target is not VisualElement element)
            {
                return false;
            }

            for (var current = element; current != null; current = current.parent)
            {
                if (ReferenceEquals(current, root))
                {
                    return true;
                }
            }

            return false;
        }
    }

    #endregion

    #region E: 테스트 대역: View Source

    internal sealed class TestViewSource : IXeriUIViewSource, IXeriUIViewSessionPersistence
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 뷰 소스의 식별자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string ID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 데이터 불러오기 횟수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int LoadCount { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 자원 획득 횟수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int AcquireCount { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 세션 저장 요청 횟수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int SaveCount { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 자원 반환 횟수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int ReleaseCount { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막으로 전달된 뷰 범위를 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriUIViewScope LastScope { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 마지막으로 전달된 뷰를 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement LastView { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 부모가 지정된 뷰를 반환할지 결정한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool ReturnParentedView { get; set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 자원을 획득할 때 호출할 콜백을 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public Action Acquiring { get; set; }

        private readonly VisualElement foreignParent = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트용 콘텐츠 뷰 소스를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TestViewSource(string id)
        {
            ID = id;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트용 창 콘텐츠 뷰를 획득한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement AcquireView(XeriUIViewScope scope)
        {
            AcquireCount++;
            LastScope = scope;
            Acquiring?.Invoke();
            LastView = new VisualElement();

            if (ReturnParentedView)
            {
                foreignParent.Add(LastView);
            }

            return LastView;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 획득한 창 콘텐츠 뷰를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ReleaseView(XeriUIViewScope scope, VisualElement view)
        {
            ReleaseCount++;
            LastScope = scope;
            LastView = view;
            view?.RemoveFromHierarchy();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 뷰의 테스트 세션 상태를 저장한다.
        /// </summary>
        // ------------------------------------------------------------
        public void SaveSession(XeriUIViewScope scope)
        {
            SaveCount++;
            LastScope = scope;

            if (scope.UISession is TestSession session)
            {
                session.Version++;
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 뷰의 테스트 세션 상태를 복원한다.
        /// </summary>
        // ------------------------------------------------------------
        public void LoadSession(XeriUIViewScope scope)
        {
            LoadCount++;
            LastScope = scope;
        }
    }

    internal sealed class TestViewOnlySource : IXeriUIViewSource
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 뷰 소스의 식별자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string ID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 자원 획득 횟수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int AcquireCount { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 자원 반환 횟수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int ReleaseCount { get; private set; }

        // ------------------------------------------------------------
        /// <summary>
        /// 콘텐츠 뷰만 제공하는 테스트 소스를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public TestViewOnlySource(string id)
        {
            ID = id;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트용 창 콘텐츠 뷰를 획득한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement AcquireView(XeriUIViewScope scope)
        {
            AcquireCount++;
            return new VisualElement();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 획득한 창 콘텐츠 뷰를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public void ReleaseView(XeriUIViewScope scope, VisualElement view)
        {
            ReleaseCount++;
            view?.RemoveFromHierarchy();
        }
    }

    #endregion

    #region F: 테스트 대역: Drag Factory 재진입

    internal sealed class CallbackDragFactory : IXeriWindowDragFactory
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 끌기 동작 생성 시 호출할 콜백을 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public Action<XeriWindowPanel, XeriWindowController> Creating { get; set; }

        private readonly XeriWindowDragFactory inner = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 창 제목줄의 끌기 동작을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriWindowTitleBarManipulator CreateTitleBarDrag
        (
            XeriWindowPanel panel,
            XeriWindowController controller
        )
        {
            Creating?.Invoke(panel, controller);
            return inner.CreateTitleBarDrag(panel, controller);
        }
    }

    #endregion

    #region G: Workspace 테스트 환경

    internal sealed class WorkspaceFixture : IDisposable
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 레이어 출력 드라이버를 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public IPresentationLayerDriver<VisualElement> LayerDriver { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 작업 공간의 배치 루트를 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public VisualElement WorkspacePlacementRoot { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 포커스 드라이버를 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public TestFocusDriver FocusDriver { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 게임 입력 액션 맵을 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public InputActionMap GameplayMap { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 창 작업 공간을 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriWindowWorkspace Workspace { get; }

        private readonly List<PresentationLayout> ownedLayouts = new();
        private readonly GameObject runtimeHost = null;
        private readonly GameObject presentationRoot = null;
        private readonly UIRuntime runtime = null;
        private readonly PresentationSession rootPresentation = null;
        private readonly DOTweenPresentationTransitioner transitioner = null;
        private readonly UIFocusDriver focusController = null;
        private readonly InputSystemScreenInputDriver inputDriver = null;
        private readonly UIContextAuthorityCoordinator authority = null;
        private readonly InputActionAsset uiActions = null;
        private readonly InputActionAsset gameplayActions = null;
        private readonly UISettingsAsset settings = null;
        private readonly PanelSettings panelSettings = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 작업 공간의 부모 컨텍스트를 보관한다.
        /// </summary>
        // ------------------------------------------------------------
        public UIContext ParentContext => rootContext;
        private readonly UIContext rootContext = null;

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 작업 공간과 의존 객체를 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        public WorkspaceFixture
        (
            IXeriUIViewResolver viewResolver = null,
            IXeriWindowDragFactory dragFactory = null,
            IXeriWindowRegistry registry = null,
            IXeriWindowThemeResolver themeResolver = null
        )
        {
            UIRuntime.Clear();

            runtimeHost = new GameObject("TEST_XeriWindowWorkspace_Runtime");
            runtimeHost.SetActive(false);
            runtimeHost.AddComponent<EventSystem>();
            var inputModule = runtimeHost.AddComponent<InputSystemUIInputModule>();
            FocusDriver = runtimeHost.AddComponent<TestFocusDriver>();
            focusController = runtimeHost.AddComponent<UIFocusDriver>();
            inputDriver = runtimeHost.AddComponent<InputSystemScreenInputDriver>();
            runtime = runtimeHost.AddComponent<UIRuntime>();

            uiActions = ScriptableObject.CreateInstance<InputActionAsset>();
            var uiMap = new InputActionMap("UI");
            uiMap.AddAction("Cancel", InputActionType.Button);
            uiActions.AddActionMap(uiMap);
            inputModule.actionsAsset = uiActions;

            gameplayActions = ScriptableObject.CreateInstance<InputActionAsset>();
            var gameplayMap = new InputActionMap("Player");
            gameplayMap.AddAction("Move", InputActionType.Value);
            gameplayActions.AddActionMap(gameplayMap);
            GameplayMap = gameplayMap;

            settings = ScriptableObject.CreateInstance<UISettingsAsset>();
            XeriWindowWorkspaceTestSupport.SetField(settings, "gameplayActionsAsset", gameplayActions);
            XeriWindowWorkspaceTestSupport.SetField(settings, "uiActionMap", "UI");
            XeriWindowWorkspaceTestSupport.SetField(settings, "gameplayActionMap", "Player");
            XeriWindowWorkspaceTestSupport.SetField
            (
                settings,
                "releaseActionNames",
                new[]
                {
                    "Cancel",
                }
            );

            focusController.Initialize();
            // Input Driver는 비활성 Host에서 초기화될 수 없으므로 조립 후 Host를 활성화한다.
            runtimeHost.SetActive(true);
            inputDriver.Initialize(inputModule, settings);
            transitioner = new DOTweenPresentationTransitioner();
            XeriWindowWorkspaceTestSupport.SetField(runtime, "transitioner", transitioner);
            XeriWindowWorkspaceTestSupport.SetField(runtime, "focusDriver", focusController);
            XeriWindowWorkspaceTestSupport.SetField(runtime, "inputDriver", inputDriver);
            XeriWindowWorkspaceTestSupport.SetProperty(runtime, nameof(UIRuntime.Settings), settings);
            authority = new UIContextAuthorityCoordinator(runtime, inputDriver);
            XeriWindowWorkspaceTestSupport.SetField(runtime, "authority", authority);
            inputDriver.OnPolicySettled += authority.HandleInputPolicySettled;
            XeriWindowWorkspaceTestSupport.SetRuntimeState(runtime, "Initialized");

            presentationRoot = new GameObject("TEST Presentation Root");
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            UITKTestPanelSettings.ApplyDefaultRuntimeTheme(panelSettings);
            var parentLayer = new PresentationPlanLayer
            (
                "TEST.Window",
                "TEST Window",
                0,
                PresentationBackend.UITK
            );
            var parentPlan = new PresentationPlan
            (
                new List<PresentationPlanLayer>
                {
                    parentLayer,
                },
                Array.Empty<PresentationPlanPlacement>()
            );
            rootPresentation = PresentationSession.CreateTopLevel
            (
                parentPlan,
                presentationRoot.transform,
                panelSettings,
                null,
                focusController.BindLayer
            );

            Assert.IsTrue
            (
                rootPresentation.LayerRegistry.TryGet
                (
                    "TEST.Window",
                    out var layer
                )
            );
            LayerDriver = (IPresentationLayerDriver<VisualElement>)layer;
            WorkspacePlacementRoot = LayerDriver.Root;

            rootContext = new UIContext
            (
                runtime,
                null,
                rootPresentation,
                transitioner,
                focusController,
                inputDriver
            );
            XeriWindowWorkspaceTestSupport.SetProperty(runtime, nameof(UIRuntime.Main), rootContext);
            XeriWindowWorkspaceTestSupport.SetProperty(runtime, nameof(UIRuntime.RootPresentation), rootPresentation);
            rootContext.SetBaseAuthority();

            Workspace = new XeriWindowWorkspace
            (
                rootContext,
                PresentationTarget.Local("TEST.Window"),
                registry: registry,
                viewResolver: viewResolver,
                dragFactory: dragFactory,
                animationOptions: XeriWindowAnimationOptions.Immediate(),
                themeResolver: themeResolver
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트 앱 창의 생성 옵션을 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        public XeriWindowApplicationOptions CreateApplicationOptions(string prefix = "Application")
        {
            var screenID = $"{prefix}.Screen";
            var overlayID = $"{prefix}.Overlay";
            var modalID = $"{prefix}.Modal";
            var layout = CreateLayout
            (
                new[]
                {
                    CreateApplicationLayer(screenID, 0),
                    CreateApplicationLayer(overlayID, 100),
                    CreateApplicationLayer(modalID, 200),
                },
                Array.Empty<PresentationPlacementDefinition>()
            );
            return new XeriWindowApplicationOptions(layout);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 테스트용 자원을 해제하고 실행 결과를 기록한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            inputDriver.OnPolicySettled -= authority.HandleInputPolicySettled;
            Workspace?.Dispose();

            if (rootContext != null && !rootContext.IsDisposed)
            {
                var errors = rootContext.DisposeFromRuntime();

                if (errors.Count > 0)
                {
                    throw new AggregateException
                    (
                        "Workspace 테스트 Root Context 정리가 실패했습니다.",
                        errors
                    );
                }
            }

            rootPresentation?.Dispose();
            inputDriver?.Dispose();
            transitioner?.Dispose();
            authority.ResetFromRuntime();

            if (runtime != null)
            {
                XeriWindowWorkspaceTestSupport.SetProperty(runtime, nameof(UIRuntime.Main), null);
                XeriWindowWorkspaceTestSupport.SetProperty(runtime, nameof(UIRuntime.RootPresentation), null);
                XeriWindowWorkspaceTestSupport.SetRuntimeState(runtime, "Released");
            }

            if (settings != null)
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }

            if (panelSettings != null)
            {
                UnityEngine.Object.DestroyImmediate(panelSettings);
            }

            if (uiActions != null)
            {
                UnityEngine.Object.DestroyImmediate(uiActions);
            }

            if (gameplayActions != null)
            {
                UnityEngine.Object.DestroyImmediate(gameplayActions);
            }

            for (var index = ownedLayouts.Count - 1; index >= 0; index--)
            {
                if (ownedLayouts[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(ownedLayouts[index]);
                }
            }

            ownedLayouts.Clear();

            if (presentationRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(presentationRoot);
            }

            if (runtimeHost != null)
            {
                UnityEngine.Object.DestroyImmediate(runtimeHost);
            }

            UIRuntime.Clear();
        }

        private PresentationLayout CreateLayout
        (
            IReadOnlyList<PresentationLayerDefinition> layers,
            IReadOnlyList<PresentationPlacementDefinition> placements
        )
        {
            var layout = PresentationTestScope.CreateLayout(layers, placements);
            ownedLayouts.Add(layout);
            return layout;
        }

        private static PresentationLayerDefinition CreateApplicationLayer
        (
            string id,
            int layerOrder
        )
        {
            return new PresentationLayerDefinition
            (
                id,
                id,
                layerOrder,
                PresentationBackend.UITK
            );
        }
    }

    #endregion

    #region H: 공용 테스트 헬퍼

    internal static class XeriWindowWorkspaceTestSupport
    {
        internal static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField
            (
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            field.SetValue(target, value);
        }

        internal static void SetProperty(object target, string name, object value)
        {
            var property = target.GetType().GetProperty
            (
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            property.SetValue(target, value);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// 조립을 우회하는 Workspace 테스트 Runtime의 내부 수명 상태를 설정한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal static void SetRuntimeState(UIRuntime runtime, string state)
        {
            var field = typeof(UIRuntime).GetField
            (
                "state",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            field.SetValue(runtime, Enum.Parse(field.FieldType, state));
        }

        internal static XeriWindowSession OpenWindow
        (
            XeriWindowWorkspace workspace,
            string id
        )
        {
            return OpenWindow(workspace, id, new VisualElement());
        }

        internal static XeriWindowSession OpenWindow
        (
            XeriWindowWorkspace workspace,
            string id,
            VisualElement view
        )
        {
            return workspace.OpenWindow
            (
                id,
                id,
                view,
                new Vector2(20f, 30f),
                new Vector2(320f, 220f)
            );
        }

        internal static ModalSession OpenApplicationModal
        (
            UIContext context,
            string layerID,
            string name,
            out Button secondaryFocus
        )
        {
            var modalRoot = new VisualElement
            {
                name = name,
            };
            var defaultFocus = new Button
            {
                name = $"{name} Default Focus",
            };
            secondaryFocus = new Button
            {
                name = $"{name} Secondary Focus",
            };
            modalRoot.Add(defaultFocus);
            modalRoot.Add(secondaryFocus);
            return UITKModal.OpenWithFocus
            (
                context,
                PresentationTarget.Local(layerID),
                modalRoot,
                new TestVisualFocusScope(modalRoot, defaultFocus)
            );
        }
    }

    #endregion
}
