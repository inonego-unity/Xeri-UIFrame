/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterValidationTests.cs
수정일 : 2026-11-17

# 설명
레이아웃별 검증 범위와 씬 호스트 중복 참조를 검사한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using NUnit;
using NUnit.Framework;
using inonego.Xeri.UI.Frame;
using inonego.Xeri.UI.Frame.Editor;

namespace inonego.Xeri.UI.Editor.TEST
{
    // ============================================================
    /// <summary>
    /// 레이아웃과 씬 호스트의 검증 경계를 확인한다.
    /// </summary>
    // ============================================================
    public sealed class ControlCenterValidationTests
    {

    #region 레이아웃과 설정 검사

        // ------------------------------------------------------------
        /// <summary>
        /// 서로 다른 레이아웃의 같은 ID를 허용하는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ControlCenterValidation_SameLayerIDInDifferentLayouts()
        {
            var first = CreateLayout("공통", false);
            var second = CreateLayout("공통", false);

            try
            {
                Assert.IsEmpty
                (
                    ControlCenterValidation.Run
                    (
                        null, first, Array.Empty<ControlCenterSceneEntry>()
                    )
                );
                Assert.IsEmpty
                (
                    ControlCenterValidation.Run
                    (
                        null, second, Array.Empty<ControlCenterSceneEntry>()
                    )
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 레이아웃의 중복 ID를 검출하는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ControlCenterValidation_DuplicateLayerIDWithinLayout()
        {
            var layout = CreateLayout("중복", true);

            try
            {
                var issues = ControlCenterValidation.Run
                (
                    null, layout, Array.Empty<ControlCenterSceneEntry>()
                );
                Assert.IsTrue(issues.Exists(issue => issue.Code == "XUI-LAYOUT-003"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(layout);
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 설정의 시스템 표시 대상 누락을 검출한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ControlCenterValidation_SystemDestinationRequired()
        {
            var layout = CreateLayout("기본", false);
            var settings = ScriptableObject.CreateInstance<UISettingsAsset>();

            try
            {
                var serialized = new SerializedObject(settings);
                serialized.FindProperty("defaultLayout").objectReferenceValue = layout;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var issues = ControlCenterValidation.Run
                (
                    settings, layout, Array.Empty<ControlCenterSceneEntry>()
                );
                Assert.IsTrue(issues.Exists(issue => issue.Code == "XUI-DEST-005"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
                UnityEngine.Object.DestroyImmediate(layout);
            }
        }

        private static PresentationLayout CreateLayout(string id, bool duplicate)
        {
            var layout = ScriptableObject.CreateInstance<PresentationLayout>();
            var serialized = new SerializedObject(layout);
            var layers = serialized.FindProperty("layers");
            layers.arraySize = duplicate ? 2 : 1;

            for (var index = 0; index < layers.arraySize; index++)
            {
                var layer = layers.GetArrayElementAtIndex(index);
                layer.FindPropertyRelative("layerID").stringValue = id;
                layer.FindPropertyRelative("displayName").stringValue = id;
                layer.FindPropertyRelative("layerOrder").intValue = index;
                layer.FindPropertyRelative("backend").enumValueIndex =
                    (int)PresentationBackend.UITK;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return layout;
        }

    #endregion

    #region 씬 호스트 참조 검사

        // ------------------------------------------------------------
        /// <summary>
        /// 중복 등록한 씬 호스트를 검출하는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        [Test]
        public void TEST_ControlCenterHostValidation_DuplicateSceneHostReference()
        {
            var root = new GameObject("검증 런타임");
            var child = new GameObject("레이어");
            child.transform.SetParent(root.transform, false);

            try
            {
                var runtime = root.AddComponent<UIRuntime>();
                var host = child.AddComponent<UGUIPresentationLayerHost>();

                var serialized = new SerializedObject(runtime);
                var property = serialized.FindProperty("presentationLayerHosts");
                Assert.NotNull(property);
                property.arraySize = 2;
                property.GetArrayElementAtIndex(0).objectReferenceValue = host;
                property.GetArrayElementAtIndex(1).objectReferenceValue = host;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var entries = new List<ControlCenterSceneEntry>
                {
                    new(runtime, "검증 씬", ControlCenterSceneKind.Runtime, "", ControlCenterSceneStatus.Placed),
                    new(host, "검증 씬", ControlCenterSceneKind.Layer, "", ControlCenterSceneStatus.Registered)
                };
                var issues = new List<ControlCenterIssue>();
                ControlCenterHostValidation.Validate(entries, issues);
                Assert.IsTrue(issues.Exists(issue => issue.Code == "XUI-HOST-003"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

    #endregion

    }
}