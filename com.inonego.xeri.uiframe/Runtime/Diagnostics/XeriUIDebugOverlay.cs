/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriUIDebugOverlay.cs
수정일 : 2026-11-12
# 설명
개발 빌드의 게임 화면에서 Runtime 상태를 확인하고 정상 화면 닫기 명령을 실행한다.
========================================================================= BLOCK_HEADER_END */

#if UNITY_EDITOR || DEBUG

using System;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Frame
{
    internal sealed class XeriUIDebugOverlay : MonoBehaviour
    {

    #region 필드

        private const string RESOURCE = "Xeri/UI/Diagnostics/XeriUIDebugOverlay";
        private const string THEME_RESOURCE = "Xeri/UI/Diagnostics/XeriUIDebugTheme";

        private UIDocument document;
        private PanelSettings panel;
        private VisualElement overlayRoot;
        private ScrollView entries;
        private Button closeScreen;
        private float nextRefresh;
        private bool isShown;

    #endregion

    #region 설치와 수명

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Debug.isDebugBuild)
            {
                return;
            }

            var existing = FindAnyObjectByType<XeriUIDebugOverlay>();
            if (existing != null)
            {
                return;
            }

            var host = new GameObject("Xeri UIFrame 진단");
            host.SetActive(false);
            var theme = Resources.Load<ThemeStyleSheet>(THEME_RESOURCE);
            if (theme == null)
            {
                Debug.LogWarning("Xeri UIFrame 진단 테마를 찾을 수 없습니다.");
                Destroy(host);
                return;
            }

            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.themeStyleSheet = theme;
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.sortingOrder = 10000;
            var overlay = host.AddComponent<XeriUIDebugOverlay>();
            overlay.panel = panel;
            host.SetActive(true);
            DontDestroyOnLoad(host);
        }

        private void Start()
        {
            document = GetComponent<UIDocument>();
            var template = Resources.Load<VisualTreeAsset>(RESOURCE);
            var stylesheet = Resources.Load<StyleSheet>(RESOURCE);

            if (document == null || template == null || stylesheet == null)
            {
                enabled = false;
                Debug.LogWarning("Xeri UIFrame 진단창 자산을 찾을 수 없습니다.");
                return;
            }

            overlayRoot = template.CloneTree();
            overlayRoot.styleSheets.Add(stylesheet);
            document.rootVisualElement.Add(overlayRoot);
            entries = overlayRoot.Q<ScrollView>("Entries");
            closeScreen = overlayRoot.Q<Button>("CloseScreen");

            overlayRoot.Q<Button>("Hide").clicked += () => SetVisible(false);
            closeScreen.clicked += CloseTopScreen;
            SetVisible(false);
        }

        private void OnDestroy()
        {

            if (panel != null)
            {
                Destroy(panel);
                panel = null;
            }
        }

    #endregion

    #region 키 입력과 조회

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
            {
                SetVisible(!isShown);
            }

            if (Time.unscaledTime < nextRefresh)
            {
                return;
            }

            nextRefresh = Time.unscaledTime + 0.3f;
            XeriUIDebugEvents.Track();
            if (isShown)
            {
                Refresh();
            }
        }

        private void SetVisible(bool visible)
        {
            isShown = visible;
            if (overlayRoot == null)
            {
                return;
            }

            overlayRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
            {
                nextRefresh = 0f;
                Refresh();
            }
        }

        private void Refresh()
        {
            var snapshot = XeriUIDiagnostics.Capture();
            entries.Clear();
            AddRow("상태", snapshot.State);
            closeScreen.SetEnabled(false);
            if (!snapshot.IsAvailable)
            {
                return;
            }

            AddRow("설정", snapshot.SettingsName);
            AddRow("기본 레이아웃", snapshot.LayoutName);
            AddRow("입력 장치", snapshot.LastInputDevice);
            for (var index = 0; index < snapshot.Contexts.Count; index++)
            {
                var context = snapshot.Contexts[index];
                var prefix = new string('　', context.Depth);
                AddRow(prefix + "컨텍스트 " + (index + 1),
                    context.IsEffective ? "입력 권한 있음" : "입력 권한 없음");
                AddRow(prefix + "레이아웃", context.LayoutName);
                AddRow(prefix + "포커스", context.HasFocusAuthority ? "보유" : "없음");
                AddRow(prefix + "모달", context.ModalCount.ToString());
                foreach (var screen in context.Screens)
                {
                    AddRow(prefix + "화면", screen);
                }

                if (context.IsEffective && context.Screens.Count > 0)
                {
                    closeScreen.SetEnabled(true);
                }
            }

            foreach (var presentation in snapshot.Presentations)
            {
                var prefix = new string('　', presentation.Depth);
                AddRow(prefix + "표현 세션", presentation.LayoutName);
                foreach (var layer in presentation.Layers)
                {
                    AddRow(prefix + "레이어", layer);
                }

                foreach (var placement in presentation.Placements)
                {
                    AddRow(prefix + "배치", placement);
                }
            }

            var history = XeriUIDiagnostics.ReadEvents();
            for (var index = Math.Max(0, history.Count - 10);
                index < history.Count; index++)
            {
                var item = history[index];
                AddRow(item.Time + " " + item.Kind, item.Message);
            }
        }

        private void AddRow(string label, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("xeri-debug-remote__row");
            var name = new Label(label);
            name.AddToClassList("xeri-debug-remote__label");
            var data = new Label(value);
            data.AddToClassList("xeri-debug-remote__value");
            row.Add(name);
            row.Add(data);
            entries.Add(row);
        }

        private void CloseTopScreen()
        {
            try
            {
                XeriUIDiagnostics.TryCloseTopScreen();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            Refresh();
        }

    #endregion

    }
}

#endif