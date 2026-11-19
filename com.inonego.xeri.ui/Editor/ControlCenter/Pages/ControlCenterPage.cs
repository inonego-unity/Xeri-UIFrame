/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterPage.cs
수정일 : 2026-11-17

# 설명
관리 도구 탭 화면의 공통 수명, 지연 갱신과 좁은 창 배치 전환을 정의한다.

# 특이사항, 제약사항
숨겨진 탭은 변경 범위만 누적하고 다시 표시될 때 한 번에 갱신한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace inonego.Xeri.UI.Editor
{
    internal abstract class ControlCenterPage
    {

    #region 필드

        private const float compactPaneDimension = 120f;
        private readonly List<TwoPaneSplitView> responsiveSplits = new();
        private readonly List<float> splitDimensions = new();
        private ControlCenterChange pending = ControlCenterChange.All;
        private bool visible;

        protected ControlCenterContext Context { get; }

        internal VisualElement Root { get; }
        internal abstract ControlCenterTab Tab { get; }
        internal abstract string Title { get; }
        internal abstract string IconName { get; }

    #endregion

    #region 생성자

        protected ControlCenterPage(ControlCenterContext context, string layoutName)
        {
            Context = context;
            Root = new VisualElement
            {
                name = "page-" + layoutName
            };
            Root.AddToClassList("xeri-cc__page");
            ControlCenterElements.CloneLayout(layoutName, Root);
        }

    #endregion

    #region 표시와 갱신

        internal void SetVisible(bool value)
        {
            visible = value;
            Root.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            if (value)
            {
                Flush();
            }
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 변경을 받아 표시 중이면 즉시, 숨겨져 있으면 다음 표시 때 갱신한다.
        /// </summary>
        // ------------------------------------------------------------
        internal void Notify(ControlCenterChange change)
        {
            pending |= change;
            if (visible)
            {
                Flush();
            }
        }

        private void Flush()
        {
            if (pending == ControlCenterChange.None)
            {
                return;
            }

            var change = pending;
            pending = ControlCenterChange.None;
            Refresh(change);
        }

        protected abstract void Refresh(ControlCenterChange change);

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 중인 탭에 Editor 주기 갱신을 전달한다.
        /// </summary>
        // ------------------------------------------------------------
        internal virtual void Tick()
        {
            // NONE
        }

    #endregion

    #region 반응형 배치

        // ------------------------------------------------------------
        /// <summary>
        /// 좁은 창에서 위아래로 전환할 좌우 분할 영역을 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        protected TwoPaneSplitView RegisterResponsiveSplit(TwoPaneSplitView split)
        {
            responsiveSplits.Add(split);
            splitDimensions.Add(split.fixedPaneInitialDimension);
            return split;
        }

        internal void SetCompact(bool compact)
        {
            var orientation = compact
                ? TwoPaneSplitViewOrientation.Vertical
                : TwoPaneSplitViewOrientation.Horizontal;
            for (var index = 0; index < responsiveSplits.Count; index++)
            {
                var split = responsiveSplits[index];
                if (split.orientation == orientation)
                {
                    continue;
                }

                // 위아래 배치에서는 고정 영역이 가로 폭을 높이로 쓰지 않도록 줄인다.
                split.fixedPaneInitialDimension = compact
                    ? Math.Min(splitDimensions[index], compactPaneDimension)
                    : splitDimensions[index];
                split.orientation = orientation;
            }
        }

    #endregion

    }
}
