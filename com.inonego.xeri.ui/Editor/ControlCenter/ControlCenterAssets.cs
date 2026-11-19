/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterAssets.cs
수정일 : 2026-11-17

# 설명
관리 도구가 다루는 설정·레이아웃·초기화(Bootstrapper) 모듈 에셋을 조회하고 생성한다.

# 특이사항, 제약사항
조회는 읽기 전용이며, 에셋 생성과 Bootstrapper 등록은 사용자가 누른 명시적 명령에서만 수행한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using inonego.Xeri.Bootstrapper;

namespace inonego.Xeri.UI.Editor
{
    internal static class ControlCenterAssets
    {

    #region 조회

        // ------------------------------------------------------------
        /// <summary>
        /// 형식의 모든 에셋을 프로젝트 에셋 먼저, 경로 순으로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static List<T> FindAll<T>() where T : UnityEngine.Object
        {
            var result = new List<T>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    result.Add(asset);
                }
            }

            result.Sort
            (
                (left, right) =>
                {
                    var package = IsPackageAsset(left).CompareTo(IsPackageAsset(right));
                    return package != 0 ? package : string.Compare
                    (
                        AssetDatabase.GetAssetPath(left), AssetDatabase.GetAssetPath(right),
                        StringComparison.OrdinalIgnoreCase
                    );
                }
            );
            return result;
        }

        internal static bool IsPackageAsset(UnityEngine.Object asset)
        {
            return asset != null
                && AssetDatabase.GetAssetPath(asset).StartsWith("Packages/", StringComparison.Ordinal);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 에셋이 프로젝트와 패키지 중 어디에 있는지 반환한다. 전체 경로는 툴팁으로 보여준다.
        /// </summary>
        // ------------------------------------------------------------
        internal static string DescribeLocation(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            return IsPackageAsset(asset) ? "패키지 에셋" : "프로젝트 에셋";
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 설정을 참조하는 UI Bootstrapper 모듈 에셋을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static List<UIBootstrapperModuleAsset> FindBootstrapperModules(UISettingsAsset settings)
        {
            var result = new List<UIBootstrapperModuleAsset>();
            if (settings == null)
            {
                return result;
            }

            foreach (var module in FindAll<UIBootstrapperModuleAsset>())
            {
                using var serialized = new SerializedObject(module);
                if (serialized.FindProperty("settings")?.objectReferenceValue == settings)
                {
                    result.Add(module);
                }
            }

            return result;
        }

    #endregion

    #region Bootstrapper 등록

        // ------------------------------------------------------------
        /// <summary>
        /// 프로젝트 Bootstrapper 설정에 모듈이 등록되어 있는지 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static bool IsBootstrapperRegistered(BootstrapperModuleAsset module)
        {
            var settings = AssetDatabase.LoadAssetAtPath<BootstrapperSettings>(BootstrapperSettings.ResourcesAssetPath);
            if (settings == null || module == null)
            {
                return false;
            }

            foreach (var registered in settings.Modules)
            {
                if (registered == module)
                {
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모듈을 프로젝트 Bootstrapper 설정의 실행 목록 끝에 등록한다. 설정 에셋이 없으면 만든다.
        /// </summary>
        // ------------------------------------------------------------
        internal static void RegisterBootstrapperModule(BootstrapperModuleAsset module)
        {
            if (module == null || IsBootstrapperRegistered(module))
            {
                return;
            }

            var settings = BootstrapperSettings.CreateAsset();
            using var serialized = new SerializedObject(settings);
            var modules = serialized.FindProperty("modules")
                ?? throw new InvalidOperationException("Bootstrapper 설정의 모듈 목록을 찾을 수 없습니다.");
            var index = modules.arraySize;
            modules.InsertArrayElementAtIndex(index);
            modules.GetArrayElementAtIndex(index).objectReferenceValue = module;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 설정을 연결한 UI 초기화 모듈을 사용자가 고른 위치에 만들고 Bootstrapper에 등록한다.
        /// 취소하면 null을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static UIBootstrapperModuleAsset CreateBootstrapperModule(UISettingsAsset settings)
        {
            var module = Create<UIBootstrapperModuleAsset>("UI Bootstrapper Module");
            if (module == null)
            {
                return null;
            }

            using (var serialized = new SerializedObject(module))
            {
                serialized.FindProperty("settings").objectReferenceValue = settings;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(module);
            AssetDatabase.SaveAssets();
            RegisterBootstrapperModule(module);
            return module;
        }

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// 사용자가 고른 위치에 새 에셋을 만든다. 취소하면 null을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static T Create<T>(string fileName) where T : ScriptableObject
        {
            var path = EditorUtility.SaveFilePanelInProject
            (
                "에셋 생성", fileName, "asset", "저장 위치를 선택하세요."
            );
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

    #endregion

    }
}
