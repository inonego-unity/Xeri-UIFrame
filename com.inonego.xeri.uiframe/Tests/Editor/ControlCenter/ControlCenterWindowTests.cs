/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterWindowTests.cs
수정일 : 2026-11-17

# 설명
관리 도구의 화면 자산, 탭 구성과 Core Collection Editor 연결을 검사한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Reflection;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;
using inonego.Xeri.UI.Frame;
using inonego.Xeri.UI.Frame.Editor;

namespace inonego.Xeri.UI.Editor.TEST
{
    // ============================================================
    /// <summary>
    /// 관리 창의 화면 자산과 탭 구성을 확인한다.
    /// </summary>
    // ============================================================
    public sealed class ControlCenterWindowTests
    {

    #region 화면 자산 검사

        // ------------------------------------------------------------
        /// <summary>
        /// 창과 탭별 UXML, 공통 USS가 로드되는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriUIControlCenterWindow_AssetsLoad()
        {
            const string root = "Xeri/UI/ControlCenter/";
            var names = new[]
            {
                "Shell", "Overview", "Settings", "Composition", "Diagnostics", "Live"
            };
            foreach (var name in names)
            {
                Assert.NotNull(Resources.Load<VisualTreeAsset>(root + name), "UXML 누락: " + name);
            }

            Assert.NotNull(Resources.Load<StyleSheet>(root + "ControlCenter"));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 실행 진단창의 화면과 테마 자산을 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriUIDebugOverlay_AssetsLoad()
        {
            const string root = "Xeri/UI/Diagnostics/";
            Assert.NotNull(Resources.Load<VisualTreeAsset>(root + "XeriUIDebugOverlay"));
            Assert.NotNull(Resources.Load<StyleSheet>(root + "XeriUIDebugOverlay"));
            Assert.NotNull(Resources.Load<ThemeStyleSheet>(root + "XeriUIDebugTheme"));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Core의 Lucide API가 관리 도구 탭 아이콘을 불러오는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriUIControlCenterWindow_IconResourcesLoad()
        {
            var names = new[]
            {
                "layout-dashboard", "settings-2", "layers", "scan-search", "activity", "plus"
            };
            foreach (var name in names)
            {
                Assert.NotNull(XeriIcon.Load(name), "Lucide 리소스 누락: " + name);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 레이아웃과 설정 컬렉션이 Core 편집기의 필드 열을 사용하는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriUIControlCenterWindow_CollectionsUseCoreEditor()
        {
            VerifyCollectionField(typeof(PresentationLayout), "layers", "displayName", "layerID", "layerOrder", "backend");
            VerifyCollectionField(typeof(PresentationLayout), "placements", "presentationID", "layerID", "localOrder");
            VerifyCollectionField(typeof(UISettingsAsset), "presentationDestinations", "destinationID", "layerID");
            VerifyCollectionField(typeof(UISettingsAsset), "releaseActionNames");
        }

        private static void VerifyCollectionField(Type owner, string name, params string[] columns)
        {
            var field = owner.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field, "컬렉션 필드 누락: " + name);
            var attribute = field.GetCustomAttribute<CollectionEditorAttribute>();
            Assert.NotNull(attribute, "Core Collection Editor 미연결: " + name);
            Assert.AreEqual(columns.Length == 0 ? null : columns, attribute.Columns);
        }

    #endregion

    #region 창 화면 구성

        // ------------------------------------------------------------
        /// <summary>
        /// 관리 창이 다섯 개의 탭과 탭별 화면을 구성하는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_XeriUIControlCenterWindow_MainTabsArePresent()
        {
            var window = ScriptableObject.CreateInstance<XeriUIControlCenterWindow>();
            try
            {
                window.CreateGUI();
                var root = window.rootVisualElement;
                Assert.AreEqual(5, root.Query<VisualElement>(className: "xeri-cc__tab").ToList().Count);
                var names = new[]
                {
                    "Overview", "Settings", "Composition", "Diagnostics", "Live"
                };
                foreach (var name in names)
                {
                    Assert.NotNull(root.Q<VisualElement>("page-" + name), "탭 화면 누락: " + name);
                }

                Assert.NotNull(root.Q<VisualElement>("control-center-settings"));
                Assert.NotNull(root.Q<VisualElement>("control-center-validation"));
                Assert.NotNull(root.Q<VisualElement>("layout-tables"));
                Assert.NotNull(root.Q<VisualElement>("scene-tree"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

    #endregion

    }
}
