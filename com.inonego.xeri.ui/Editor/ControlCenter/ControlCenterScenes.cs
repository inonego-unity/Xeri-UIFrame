/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterScenes.cs
수정일 : 2026-11-17

# 설명
로드된 씬과 프리팹 편집 단계의 UI Runtime 및 Layer·Placement Host를 읽기 전용으로 수집한다.

# 특이사항, 제약사항
씬에 Host가 없는 생성형 레이어는 정상일 수 있으므로 누락으로 판정하지 않는다.
등록됨은 Runtime의 Host 목록에 참조가 있다는 의미이며 출력 구성의 유효성을 보증하지 않는다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace inonego.Xeri.UI.Editor
{
    internal enum ControlCenterSceneKind { Runtime, Layer, Placement }

    internal enum ControlCenterSceneStatus { Placed, Running, Registered, Unlinked, Invalid }

    internal sealed class ControlCenterSceneEntry
    {
        internal Component Component { get; }
        internal string SceneName { get; }
        internal ControlCenterSceneKind Kind { get; }
        internal string ID { get; }
        internal ControlCenterSceneStatus Status { get; private set; }

        internal ControlCenterSceneEntry
        (
            Component component, string sceneName, ControlCenterSceneKind kind,
            string id, ControlCenterSceneStatus status
        )
        {
            Component = component;
            SceneName = sceneName;
            Kind = kind;
            ID = id;
            Status = status;
        }

        internal string KindText => Kind switch
        {
            ControlCenterSceneKind.Runtime => "런타임",
            ControlCenterSceneKind.Layer => "레이어 호스트",
            _ => "배치 호스트",
        };

        internal string StatusText => Status switch
        {
            ControlCenterSceneStatus.Running => "실행 중",
            ControlCenterSceneStatus.Registered => "등록됨",
            ControlCenterSceneStatus.Unlinked => "미연결",
            ControlCenterSceneStatus.Invalid => "참조 오류",
            _ => "씬에 배치됨",
        };

        internal void MarkInvalid()
        {
            Status = ControlCenterSceneStatus.Invalid;
        }
    }

    internal static class ControlCenterScenes
    {

    #region 씬 구성 수집

        internal static List<ControlCenterSceneEntry> Collect()
        {
            var runtimes = new List<UIRuntime>();
            var layers = new List<PresentationLayerHost>();
            var placements = new List<PresentationPlacementHost>();
            CollectComponents(runtimes, layers, placements);

            // Runtime이 실제로 등록한 Host만 연결된 것으로 본다.
            var owners = new Dictionary<PresentationLayerHost, UIRuntime>();
            var registeredPlacements = new HashSet<PresentationPlacementHost>();
            foreach (var runtime in runtimes)
            {
                foreach (var host in ReadRegisteredHosts(runtime))
                {
                    owners[host] = runtime;
                    foreach (var placement in host.PlacementHosts)
                    {
                        if (placement != null)
                        {
                            registeredPlacements.Add(placement);
                        }
                    }
                }
            }

            var result = new List<ControlCenterSceneEntry>();
            foreach (var runtime in runtimes)
            {
                result.Add
                (
                    new ControlCenterSceneEntry
                    (
                        runtime, runtime.gameObject.scene.name, ControlCenterSceneKind.Runtime,
                        runtime.name,
                        runtime.IsInitialized ? ControlCenterSceneStatus.Running : ControlCenterSceneStatus.Placed
                    )
                );
            }

            foreach (var host in layers)
            {
                result.Add
                (
                    new ControlCenterSceneEntry
                    (
                        host, host.gameObject.scene.name, ControlCenterSceneKind.Layer, host.LayerID,
                        owners.ContainsKey(host) ? ControlCenterSceneStatus.Registered : ControlCenterSceneStatus.Unlinked
                    )
                );
            }

            foreach (var host in placements)
            {
                result.Add
                (
                    new ControlCenterSceneEntry
                    (
                        host, host.gameObject.scene.name, ControlCenterSceneKind.Placement, host.PresentationID,
                        registeredPlacements.Contains(host)
                            ? ControlCenterSceneStatus.Registered : ControlCenterSceneStatus.Unlinked
                    )
                );
            }

            // Host 검사에서 문제가 난 구성 요소는 목록에서도 참조 오류로 표시한다.
            var issues = new List<ControlCenterIssue>();
            ControlCenterHostValidation.Validate(result, issues);
            var invalid = new HashSet<UnityEngine.Object>();
            foreach (var issue in issues)
            {
                if (issue.Target != null)
                {
                    invalid.Add(issue.Target);
                }
            }

            foreach (var entry in result)
            {
                if (invalid.Contains(entry.Component))
                {
                    entry.MarkInvalid();
                }
            }

            return result;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime이 직렬화한 Layer Host 참조를 순서대로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static List<PresentationLayerHost> ReadRegisteredHosts(UIRuntime runtime)
        {
            var result = new List<PresentationLayerHost>();
            if (TryReadHostReferences(runtime, out var references))
            {
                foreach (var host in references)
                {
                    if (host != null)
                    {
                        result.Add(host);
                    }
                }
            }

            return result;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> Runtime이 직렬화한 Layer Host 참조를 빈 참조까지 포함해 순서대로 읽는다.
        /// <br/> 호스트 목록을 직렬화에서 찾을 수 없으면 false를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static bool TryReadHostReferences(UIRuntime runtime, out List<PresentationLayerHost> references)
        {
            references = new List<PresentationLayerHost>();
            using var serialized = new SerializedObject(runtime);
            var property = serialized.FindProperty("presentationLayerHosts");
            if (property == null || !property.isArray)
            {
                return false;
            }

            for (var index = 0; index < property.arraySize; index++)
            {
                references.Add(property.GetArrayElementAtIndex(index).objectReferenceValue as PresentationLayerHost);
            }

            return true;
        }

        private static void CollectComponents
        (
            List<UIRuntime> runtimes,
            List<PresentationLayerHost> layers,
            List<PresentationPlacementHost> placements
        )
        {
            var scanned = new HashSet<Scene>();
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                scanned.Add(scene);
                foreach (var root in scene.GetRootGameObjects())
                {
                    AddComponents(root, runtimes, layers, placements);
                }
            }

            // 프리팹 편집 단계는 별도 씬이므로 로드된 씬 목록과 겹치지 않을 때만 추가한다.
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.prefabContentsRoot != null && !scanned.Contains(stage.scene))
            {
                AddComponents(stage.prefabContentsRoot, runtimes, layers, placements);
            }
        }

        private static void AddComponents
        (
            GameObject root,
            List<UIRuntime> runtimes,
            List<PresentationLayerHost> layers,
            List<PresentationPlacementHost> placements
        )
        {
            runtimes.AddRange(root.GetComponentsInChildren<UIRuntime>(true));
            layers.AddRange(root.GetComponentsInChildren<PresentationLayerHost>(true));
            placements.AddRange(root.GetComponentsInChildren<PresentationPlacementHost>(true));
        }

    #endregion

    }
}
