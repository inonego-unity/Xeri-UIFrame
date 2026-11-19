/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterElements.cs
수정일 : 2026-11-19

# 설명
관리 도구 화면이 공유하는 레이아웃 자산 로드, 빈 상태 안내, 속성 행, 아이콘 버튼과 직렬화 필드 구성을 제공한다.

# 특이사항, 제약사항
Unity 기본 컨트롤의 내부 스타일은 바꾸지 않고 바깥 컨테이너의 클래스만 지정한다.
아이콘은 Xeri Core의 XeriIcon을 사용한다.
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

namespace inonego.Xeri.UI.Editor
{
    internal static class ControlCenterElements
    {

    #region 자산

        private const string resourceRoot = "Xeri/UI/ControlCenter/";

        // ------------------------------------------------------------
        /// <summary>
        /// 관리 도구의 UXML을 지정한 부모에 복제한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static void CloneLayout(string name, VisualElement parent)
        {
            var tree = Resources.Load<VisualTreeAsset>(resourceRoot + name)
                ?? throw new InvalidOperationException("관리 도구 화면 자산을 찾을 수 없습니다: " + name);
            tree.CloneTree(parent);
        }

        internal static StyleSheet LoadStyle()
        {
            return Resources.Load<StyleSheet>(resourceRoot + "ControlCenter")
                ?? throw new InvalidOperationException("관리 도구 스타일을 찾을 수 없습니다.");
        }

        // --------------------------------------------------------------------------------
        /// <summary>
        /// Xeri Core가 제공하는 공통 교차 행 스타일을 반환한다. 대비는 --xeri-row-alt로 정한다.
        /// </summary>
        // --------------------------------------------------------------------------------
        internal static StyleSheet LoadRowStyle()
        {
            return Resources.Load<StyleSheet>("Xeri/Common/ListRows")
                ?? throw new InvalidOperationException("공통 목록 행 스타일을 찾을 수 없습니다.");
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 컨테이너의 행을 번갈아 표시하도록 짝수 번째 행에 교차 클래스를 붙인다.
        /// <br/> USS에는 순번 선택자가 없으므로 행을 다시 만든 뒤마다 호출한다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal static void StripeRows(VisualElement container)
        {
            var index = 0;
            foreach (var child in container.Children())
            {
                if (!child.ClassListContains("xeri-cc__row") && !child.ClassListContains("xeri-cc__issue"))
                {
                    continue;
                }

                child.EnableInClassList("xeri-cc__row--alt", index % 2 == 1);
                index++;
            }
        }

        internal static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name)
                ?? throw new InvalidOperationException("관리 도구 화면 요소 누락: " + name);
        }

    #endregion

    #region 공통 요소

        // ------------------------------------------------------------
        /// <summary>
        /// 화면 가운데 표시하는 빈 상태 안내와 선택적 명령 버튼을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        internal static VisualElement CreateNotice
        (
            string message, params (string Text, Action Action)[] actions
        )
        {
            var notice = new VisualElement();
            notice.AddToClassList("xeri-cc__notice");

            var label = new Label(message);
            label.AddToClassList("xeri-cc__notice-text");
            notice.Add(label);

            if (actions.Length > 0)
            {
                var buttons = new VisualElement();
                buttons.AddToClassList("xeri-cc__notice-actions");
                foreach (var (text, action) in actions)
                {
                    buttons.Add
                    (
                        new Button(action)
                        {
                            text = text
                        }
                    );
                }

                notice.Add(buttons);
            }

            return notice;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 이름과 값을 한 줄에 배치한 읽기 전용 속성 행을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        internal static VisualElement CreateRow(string name, string value, string tooltip = null)
        {
            var row = CreateRowFrame(name);
            var data = new Label(string.IsNullOrEmpty(value) ? "—" : value);
            data.AddToClassList("xeri-cc__row-value");
            data.tooltip = tooltip ?? value;
            row.Add(data);
            return row;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 값을 누르면 대상으로 이동하는 속성 행을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        internal static VisualElement CreateLinkRow
        (
            string name, string value, Action navigate, string tooltip = null
        )
        {
            var row = CreateRowFrame(name);
            var link = new Button(navigate)
            {
                text = value,
                tooltip = tooltip ?? value
            };
            link.AddToClassList("xeri-cc__row-link");
            row.Add(link);
            return row;
        }

        private static VisualElement CreateRowFrame(string name)
        {
            var row = new VisualElement();
            row.AddToClassList("xeri-cc__row");

            var key = new Label(name);
            key.AddToClassList("xeri-cc__row-key");
            row.Add(key);
            return row;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 목록 행이나 머리말에 붙이는 짧은 표식을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        internal static Badge CreateBadge(string text, bool accent = false)
        {
            // Core 공용 Badge에 관리 도구 색·모서리만 입힌다(ControlCenter.uss의 .xeri-cc__badge).
            var badge = new Badge(text);
            badge.Tone = accent ? BadgeTone.Accent : BadgeTone.Neutral;
            badge.AddToClassList("xeri-cc__badge");
            return badge;
        }

        internal static void SetIcon(Button button, string iconName)
        {
            button.iconImage = XeriIcon.LoadBackground(iconName);
            button.AddToClassList("xeri-cc__icon-button");
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 심각도의 한국어 표시 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static string SeverityText(DiagnosticSeverity severity) => severity switch
        {
            DiagnosticSeverity.Error => "오류",
            DiagnosticSeverity.Warning => "경고",
            _ => "정보",
        };

    #endregion

    #region 직렬화 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 직렬화 속성을 한국어 레이블의 PropertyField로 만들어 바인딩한다.
        /// </summary>
        // ------------------------------------------------------------
        internal static PropertyField AddProperty
        (
            VisualElement parent, SerializedObject serialized, string path, string label
        )
        {
            var property = serialized.FindProperty(path)
                ?? throw new InvalidOperationException("직렬화 필드를 찾을 수 없습니다: " + path);
            var field = new PropertyField(property, label);
            field.Bind(serialized);
            parent.Add(field);
            return field;
        }

    #endregion

    }
}
