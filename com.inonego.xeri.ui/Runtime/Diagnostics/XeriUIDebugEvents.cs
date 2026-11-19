/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriUIDebugEvents.cs
수정일 : 2026-11-12
# 설명
개발 환경에서 화면과 모달 변경을 제한된 크기의 사건 기록으로 수집한다.
========================================================================= BLOCK_HEADER_END */

#if UNITY_EDITOR || DEBUG

using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 실행 중 발생한 UI 진단 기록을 보관한다.
    /// </summary>
    // ============================================================
    public sealed class XeriUIDebugEvent
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 기록 시각을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Time { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 사건 종류를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Kind { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 사건 내용을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Message { get; }

        internal XeriUIDebugEvent(string time, string kind, string message)
        {
            Time = time;
            Kind = kind;
            Message = message;
        }
    }

    internal static class XeriUIDebugEvents
    {

    #region 내부 데이터

        private sealed class Subscription
        {
            internal UIContext Context;
            internal Action ScreensChanged;
            internal Action ModalsChanged;
        }

    #endregion

    #region 필드

        private const int MAX_EVENTS = 200;
        private static readonly List<XeriUIDebugEvent> history = new();
        private static readonly Dictionary<UIContext, Subscription> subscriptions = new();
        private static readonly HashSet<UIContext> visited = new();

        private static UIRuntime runtime;

    #endregion

    #region 구독

        internal static void Track()
        {
            if (!UIRuntime.TryCurrent(out var current) ||
                current == null || !current.IsInitialized || current.Main == null)
            {
                Detach();
                return;
            }

            if (!ReferenceEquals(runtime, current))
            {
                Detach();
                runtime = current;
                current.OnReleasing += HandleReleasing;
                Record("런타임", "초기화 완료");
            }

            visited.Clear();
            TrackContext(current.Main);

            List<UIContext> stale = null;
            foreach (var context in subscriptions.Keys)
            {
                if (!visited.Contains(context))
                {
                    (stale ??= new List<UIContext>()).Add(context);
                }
            }

            if (stale != null)
            {
                foreach (var context in stale)
                {
                    Unsubscribe(context);
                }
            }
        }

        private static void TrackContext(UIContext context)
        {
            if (context == null || context.IsDisposed || context.IsDisposing)
            {
                return;
            }

            visited.Add(context);
            if (!subscriptions.ContainsKey(context))
            {
                var current = context;
                var subscription = new Subscription
                {
                    Context = current,
                    ScreensChanged = () => Record
                    (
                        "화면", "화면 스택 " + current.Screens.Count
                    ),
                    ModalsChanged = () => Record
                    (
                        "모달", "모달 스택 " + current.Modals.Count
                    )
                };
                current.Screens.OnStackChanged += subscription.ScreensChanged;
                current.Modals.OnStackChanged += subscription.ModalsChanged;
                subscriptions.Add(current, subscription);
            }

            foreach (var child in context.DebugChildren)
            {
                TrackContext(child);
            }
        }

        private static void Unsubscribe(UIContext context)
        {
            if (!subscriptions.TryGetValue(context, out var subscription))
            {
                return;
            }

            context.Screens.OnStackChanged -= subscription.ScreensChanged;
            context.Modals.OnStackChanged -= subscription.ModalsChanged;
            subscriptions.Remove(context);
        }

        private static void HandleReleasing(UIRuntime _)
        {
            Record("런타임", "해제");
            Detach();
        }

        private static void Detach()
        {
            if (runtime != null)
            {
                runtime.OnReleasing -= HandleReleasing;
                runtime = null;
            }

            foreach (var item in subscriptions.Values)
            {
                item.Context.Screens.OnStackChanged -= item.ScreensChanged;
                item.Context.Modals.OnStackChanged -= item.ModalsChanged;
            }

            subscriptions.Clear();
            visited.Clear();
        }

    #endregion

    #region 기록

        internal static IReadOnlyList<XeriUIDebugEvent> Snapshot()
        {
            return history.ToArray();
        }

        private static void Record(string kind, string message)
        {
            if (history.Count >= MAX_EVENTS)
            {
                history.RemoveAt(0);
            }

            history.Add(new XeriUIDebugEvent
            (
                DateTime.Now.ToString("HH:mm:ss.fff"), kind, message
            ));
        }

    #endregion

    }
}

#endif