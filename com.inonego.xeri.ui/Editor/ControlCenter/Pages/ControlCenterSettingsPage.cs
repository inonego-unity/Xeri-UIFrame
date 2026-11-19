/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterSettingsPage.cs
수정일 : 2026-11-17

# 설명
선택한 UISettingsAsset을 일반·연결 대상·입력·렌더링·초기화 분류로 나눠 편집한다.

# 특이사항, 제약사항
필드는 SerializedObject 바인딩으로 편집해 Undo와 Dirty 처리를 Unity에 맡긴다.
분류나 대상이 바뀔 때만 필드를 다시 만들고, 컬렉션은 남은 높이를 채워 바깥 스크롤과 겹치지 않는다.
렌더링 출력의 적용·제거는 확인 뒤 기존 Installer를 호출한다.
초기화 분류에서는 UI 초기화 모듈의 생성·Bootstrapper 등록과 호스트 프리팹 지정을 제공한다.
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
    internal sealed class ControlCenterSettingsPage : ControlCenterPage
    {

    #region 필드

        internal const int GeneralCategory = 0;
        internal const int DestinationsCategory = 1;
        internal const int InputCategory = 2;
        internal const int RenderingCategory = 3;
        internal const int BootstrapperCategory = 4;

        private static readonly string[] categoryNames =
        {
            "일반", "연결 대상", "입력", "렌더링", "초기화",
        };

        private readonly ListView categories;
        private readonly Label title;
        private readonly Label location;
        private readonly VisualElement body;
        private readonly VisualElement split;
        private readonly VisualElement empty;
        private SerializedObject serialized;
        private readonly List<SerializedObject> moduleObjects = new();
        private UISettingsAsset renderedSettings;
        private int renderedCategory = -1;

        internal override ControlCenterTab Tab => ControlCenterTab.Settings;
        internal override string Title => "설정";
        internal override string IconName => "settings-2";

    #endregion

    #region 생성자

        internal ControlCenterSettingsPage(ControlCenterContext context)
            : base(context, "Settings")
        {
            split = RegisterResponsiveSplit(ControlCenterElements.Require<TwoPaneSplitView>(Root, "settings-split"));
            categories = ControlCenterElements.Require<ListView>(Root, "settings-categories");
            title = ControlCenterElements.Require<Label>(Root, "settings-title");
            location = ControlCenterElements.Require<Label>(Root, "settings-location");
            body = ControlCenterElements.Require<VisualElement>(Root, "settings-body");
            empty = ControlCenterElements.Require<VisualElement>(Root, "settings-empty");

            categories.itemsSource = categoryNames;
            categories.makeItem = () =>
            {
                var label = new Label();
                label.AddToClassList("xeri-cc__nav-item");
                return label;
            };
            categories.bindItem = (element, index) => ((Label)element).text = categoryNames[index];
            categories.selectedIndicesChanged += indices =>
            {
                foreach (var index in indices)
                {
                    Context.State.SettingsCategory = index;
                    Render();
                    return;
                }
            };
        }

    #endregion

    #region 표시

        protected override void Refresh(ControlCenterChange change)
        {
            // 초기화 모듈과 등록 상태는 다른 에셋의 변경에도 달라지므로 같은 분류여도 다시 구성한다.
            var assetsChanged = (change & (ControlCenterChange.Catalog | ControlCenterChange.Bootstrapper)) != 0;
            Render(assetsChanged && Context.State.SettingsCategory == BootstrapperCategory);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 설정과 분류가 바뀐 경우에만 편집 필드를 다시 구성한다. rebuild가 true면 항상 다시 구성한다.
        /// </summary>
        // ------------------------------------------------------------
        private void Render(bool rebuild = false)
        {
            var settings = Context.Settings;
            var category = Math.Clamp(Context.State.SettingsCategory, 0, categoryNames.Length - 1);
            var selectedIndexes = new[]
            {
                category
            };
            categories.SetSelectionWithoutNotify(selectedIndexes);

            if (settings == null)
            {
                ShowEmpty();
                return;
            }

            split.style.display = DisplayStyle.Flex;
            empty.style.display = DisplayStyle.None;
            if (!rebuild && settings == renderedSettings && category == renderedCategory && serialized != null)
            {
                return;
            }

            // 이전 필드의 바인딩을 먼저 해제한 뒤 SerializedObject를 교체한다.
            body.Unbind();
            body.Clear();
            DisposeModuleObjects();
            if (serialized == null || serialized.targetObject != settings)
            {
                serialized?.Dispose();
                serialized = new SerializedObject(settings);
            }

            renderedSettings = settings;
            renderedCategory = category;
            title.text = categoryNames[category];
            location.text = settings.name + " · " + ControlCenterAssets.DescribeLocation(settings);
            location.tooltip = AssetDatabase.GetAssetPath(settings);
            switch (category)
            {
                case GeneralCategory: RenderGeneral(); break;
                case DestinationsCategory: RenderDestinations(); break;
                case InputCategory: RenderInput(); break;
                case RenderingCategory: RenderRendering(settings); break;
                default: RenderBootstrapper(settings); break;
            }
        }

        private void ShowEmpty()
        {
            split.style.display = DisplayStyle.None;
            empty.style.display = DisplayStyle.Flex;
            empty.Clear();
            body.Unbind();
            body.Clear();
            DisposeModuleObjects();
            renderedSettings = null;
            serialized?.Dispose();
            serialized = null;

            // 프로젝트의 설정 에셋을 바로 고를 수 있게 버튼으로 나열한다.
            var actions = new List<(string, Action)>();
            foreach (var asset in Context.SettingsAssets)
            {
                var choice = asset;
                actions.Add((asset.name + " (" + ControlCenterAssets.DescribeLocation(asset) + ")", () => Context.SelectSettings(choice)));
            }

            actions.Add(("새 설정 만들기", Context.CreateSettings));
            empty.Add
            (
                ControlCenterElements.CreateNotice("관리할 UI 설정 에셋을 선택하세요.", actions.ToArray())
            );
        }

        private void DisposeModuleObjects()
        {
            foreach (var moduleObject in moduleObjects)
            {
                moduleObject.Dispose();
            }

            moduleObjects.Clear();
        }

    #endregion

    #region 분류별 필드

        private VisualElement CreateForm(bool scroll)
        {
            // 상세 영역 폭을 가득 채우는 내용 영역을 둔다.
            var form = new VisualElement();
            form.AddToClassList("xeri-cc__form");

            // Inspector와 같은 필드 레이블 정렬을 적용한다.
            form.AddToClassList(InspectorElement.ussClassName);
            if (!scroll)
            {
                body.Add(form);
                return form;
            }

            var scrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
            };
            scrollView.AddToClassList("xeri-cc__form-scroll");
            scrollView.Add(form);
            body.Add(scrollView);
            return form;
        }

        private void AddFillCollection(string path, string label)
        {
            var host = new VisualElement();
            host.AddToClassList("xeri-cc__collection-fill");
            host.EnableInClassList("xeri-cc__collection-fill--below-form", body.childCount > 0);
            body.Add(host);
            ControlCenterElements.AddProperty(host, serialized, path, label);
        }

        private void RenderGeneral()
        {
            var form = CreateForm(true);
            ControlCenterElements.AddProperty(form, serialized, "defaultLayout", "기본 레이아웃");
            ControlCenterElements.AddProperty(form, serialized, "uitkPanelSettingsTemplate", "UITK 패널 설정");
            ControlCenterElements.AddProperty(form, serialized, "uguiOutputTemplate", "UGUI 출력");
            ControlCenterElements.AddProperty(form, serialized, "defaultFadeColor", "전환 색상");
            ControlCenterElements.AddProperty(form, serialized, "defaultFadeDuration", "전환 시간");
        }

        private void RenderDestinations()
        {
            AddFillCollection("presentationDestinations", "연결 대상");
        }

        private void RenderInput()
        {
            var form = CreateForm(false);
            ControlCenterElements.AddProperty(form, serialized, "uiActionsAsset", "UI 입력 에셋");
            ControlCenterElements.AddProperty(form, serialized, "uiActionMap", "UI 액션 맵");
            ControlCenterElements.AddProperty(form, serialized, "gameplayActionsAsset", "게임 입력 에셋");
            ControlCenterElements.AddProperty(form, serialized, "gameplayActionMap", "게임 액션 맵");
            AddFillCollection("releaseActionNames", "해제 입력");
        }

        private void RenderRendering(UISettingsAsset settings)
        {
            var form = CreateForm(true);
            ControlCenterElements.AddProperty(form, serialized, "backendSupport", "지원 백엔드");
            ControlCenterElements.AddProperty(form, serialized, "highPrecisionOutput", "고정밀 출력");
            ControlCenterElements.AddProperty(form, serialized, "uiPostProcessing", "후처리 시점");

            var heading = new Label("파이프라인 출력");
            heading.AddToClassList("xeri-cc__form-heading");
            form.Add(heading);

            var integrations = ControlCenterRendering.Find();
            if (integrations.Count == 0)
            {
                form.Add(ControlCenterElements.CreateRow("상태", "현재 렌더 파이프라인용 출력 도구가 없습니다."));
                return;
            }

            foreach (var integration in integrations)
            {
                form.Add(CreateRenderingRow(integration, settings));
            }
        }

        private VisualElement CreateRenderingRow(IControlCenterRendering integration, UISettingsAsset settings)
        {
            var row = new VisualElement();
            row.AddToClassList("xeri-cc__action-row");

            var name = new Label(integration.DisplayName);
            name.AddToClassList("xeri-cc__row-key");
            row.Add(name);

            var enabled = !integration.RequiresSettings || settings != null;
            var preview = new Button(() => PreviewRendering(integration, settings))
            {
                text = "미리보기"
            };
            preview.SetEnabled(enabled);
            row.Add(preview);

            var install = new Button
            (
                () =>
                {
                    if (EditorUtility.DisplayDialog
                    (
                        integration.DisplayName + " 출력 적용", "렌더링 설정에 Xeri UI 출력을 적용하시겠습니까?", "적용", "취소"
                    ))
                    {
                        RunRenderingAction(() => integration.Install(settings));
                    }
                }
            )
            {
                text = "적용",
            };
            install.SetEnabled(enabled);
            row.Add(install);

            var remove = new Button
            (
                () =>
                {
                    if (EditorUtility.DisplayDialog
                    (
                        integration.DisplayName + " 출력 제거", "Xeri UI가 설치한 출력 설정을 제거하시겠습니까?", "제거", "취소"
                    ))
                    {
                        RunRenderingAction(integration.Uninstall);
                    }
                }
            )
            {
                text = "제거",
            };
            row.Add(remove);
            return row;
        }

        private static void PreviewRendering(IControlCenterRendering integration, UISettingsAsset settings)
        {
            try
            {
                EditorUtility.DisplayDialog
                (
                    integration.DisplayName + " 변경 미리보기", integration.Preview(settings), "닫기"
                );
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("미리보기 실패", exception.Message, "닫기");
            }
        }

        private void RunRenderingAction(Action action)
        {
            try
            {
                action();
                Context.MarkValidationStale();
                EditorUtility.DisplayDialog("렌더링 출력", "작업을 완료했습니다.", "확인");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("렌더링 출력 오류", exception.Message, "닫기");
            }
        }

        private void RenderBootstrapper(UISettingsAsset settings)
        {
            var form = CreateForm(true);
            var modules = ControlCenterAssets.FindBootstrapperModules(settings);
            var openProjectSettings = ("프로젝트 초기화 설정 열기", (Action)(() => SettingsService.OpenProjectSettings("Project/Bootstrapper")));

            // 모듈이 없으면 안내와 두 명령을 한 곳에 모은다.
            if (modules.Count == 0)
            {
                form.Add
                (
                    ControlCenterElements.CreateNotice
                    (
                        "이 설정을 사용하는 UI 초기화 모듈이 없습니다.",
                        ("초기화 모듈 만들기", () => CreateBootstrapperModule(settings)),
                        openProjectSettings
                    )
                );
                return;
            }

            foreach (var module in modules)
            {
                AddModule(form, module);
            }

            var actions = new VisualElement();
            actions.AddToClassList("xeri-cc__action-row");
            actions.Add
            (
                new Button(() => CreateBootstrapperModule(settings))
                {
                    text = "초기화 모듈 추가",
                }
            );
            actions.Add
            (
                new Button(openProjectSettings.Item2)
                {
                    text = openProjectSettings.Item1,
                }
            );
            form.Add(actions);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모듈의 등록 상태와 호스트 프리팹을 표시한다. 패키지 모듈은 읽기 전용으로 보여 준다.
        /// </summary>
        // ------------------------------------------------------------
        private void AddModule(VisualElement form, UIBootstrapperModuleAsset module)
        {
            var heading = new Label(module.name);
            heading.AddToClassList("xeri-cc__form-heading");
            heading.tooltip = AssetDatabase.GetAssetPath(module);
            form.Add(heading);

            var moduleObject = new SerializedObject(module);
            moduleObjects.Add(moduleObject);
            var hostPrefab = moduleObject.FindProperty("hostPrefab").objectReferenceValue;
            if (ControlCenterAssets.IsPackageAsset(module))
            {
                form.Add
                (
                    hostPrefab == null
                        ? ControlCenterElements.CreateRow("호스트 프리팹", "선택 안 됨")
                        : ControlCenterElements.CreateLinkRow
                        (
                            "호스트 프리팹", hostPrefab.name, () => ControlCenterContext.Reveal(hostPrefab)
                        )
                );
            }
            else
            {
                ControlCenterElements.AddProperty(form, moduleObject, "hostPrefab", "호스트 프리팹");
            }

            if (ControlCenterAssets.IsBootstrapperRegistered(module))
            {
                form.Add(ControlCenterElements.CreateRow("실행 등록", "프로젝트 초기화 설정에 등록됨"));
            }
            else
            {
                form.Add
                (
                    ControlCenterElements.CreateNotice
                    (
                        "프로젝트 초기화 설정에 등록되지 않아 앱 시작 때 실행되지 않습니다.",
                        ("초기화 설정에 등록", () => RegisterBootstrapperModule(module))
                    )
                );
            }
        }

        private void CreateBootstrapperModule(UISettingsAsset settings)
        {
            if (ControlCenterAssets.CreateBootstrapperModule(settings) != null)
            {
                RebuildBootstrapperPage();
            }
        }

        private void RegisterBootstrapperModule(UIBootstrapperModuleAsset module)
        {
            try
            {
                ControlCenterAssets.RegisterBootstrapperModule(module);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("초기화 모듈 등록 실패", exception.Message, "닫기");
            }

            RebuildBootstrapperPage();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 에셋 목록이 바뀐 뒤 현재 분류를 다시 구성하고 개요의 요약도 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        private void RebuildBootstrapperPage()
        {
            Context.RefreshCatalog();
        }

    #endregion

    }
}
