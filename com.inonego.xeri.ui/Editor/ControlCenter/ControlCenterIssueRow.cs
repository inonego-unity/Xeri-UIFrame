/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : ControlCenterIssueRow.cs
수정일 : 2026-11-17

# 설명
진단 결과 한 건을 심각도 아이콘, 진단명과 대상으로 표시하는 목록 행이다.

# 특이사항, 제약사항
개요와 진단 탭이 같은 행 표시를 공유하며 심각도 아이콘은 Xeri Core SeverityIndicator를 사용한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;
using UnityEngine.UIElements;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.UI.Editor
{
    internal sealed class ControlCenterIssueRow : VisualElement
    {

    #region 필드

        private readonly SeverityIndicator severity = new();
        private readonly Label title = new();
        private readonly Label target = new();
        private readonly Label code = new();

    #endregion

    #region 생성자

        internal ControlCenterIssueRow()
        {
            AddToClassList("xeri-cc__issue");
            severity.AddToClassList("xeri-cc__issue-icon");
            title.AddToClassList("xeri-cc__issue-title");
            target.AddToClassList("xeri-cc__issue-target");
            code.AddToClassList("xeri-cc__issue-code");
            Add(severity);
            Add(title);
            Add(target);
            Add(code);
        }

    #endregion

    #region 표시

        internal void Bind(ControlCenterIssue issue)
        {
            severity.SetSeverity(issue.Severity);
            title.text = issue.Title;
            // 같은 진단명이 반복돼도 구분되도록 대상과 내용을 함께 표시한다.
            target.text = (issue.Target != null ? issue.Target.name : "대상 없음") + " · " + issue.Detail;
            code.text = issue.Code;
            tooltip = issue.Detail;
        }

    #endregion

    }
}
