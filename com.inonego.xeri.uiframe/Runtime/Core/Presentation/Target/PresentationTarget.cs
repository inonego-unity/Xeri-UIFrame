/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationTarget.cs
수정일 : 2026-11-12

# 설명
UI를 현재 Session의 local Layer 또는 Runtime PresentationHost destination에 표시할 대상을 정의한다.
Screen identity, local topology와 app-wide destination identity를 분리한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.Frame
{
    // ============================================================
    /// <summary>
    /// 표시 대상이 속하는 범위를 정의한다.
    /// </summary>
    // ============================================================
    public enum PresentationTargetScope
    {
        Local = 0,
        Host = 1,
    }

    // ======================================================================
    /// <summary>
    /// local Layer 또는 app-wide Host destination을 가리키는 표시 대상.
    /// </summary>
    // ======================================================================
    public readonly struct PresentationTarget : IEquatable<PresentationTarget>
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Session 또는 Host 대상 범위이다.
        /// </summary>
        // ------------------------------------------------------------
        public PresentationTargetScope Scope { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택한 범위에서 해석할 Layer 또는 Destination ID이다.
        /// </summary>
        // ------------------------------------------------------------
        public string ID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 대상 식별자가 비어 있지 않은지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsValid => !string.IsNullOrWhiteSpace(ID);

    #endregion

    #region 생성

        private PresentationTarget(PresentationTargetScope scope, string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Presentation Target ID가 비어 있습니다.", nameof(id));
            }

            Scope = scope;
            ID = id;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 Session의 Layer를 대상으로 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PresentationTarget Local(string layerID)
        {
            return new PresentationTarget(PresentationTargetScope.Local, layerID);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// Runtime Host의 Destination을 대상으로 지정한다.
        /// </summary>
        // ------------------------------------------------------------
        public static PresentationTarget Host(string destinationID)
        {
            return new PresentationTarget(PresentationTargetScope.Host, destinationID);
        }

    #endregion

    #region 비교

        // ------------------------------------------------------------
        /// <summary>
        /// 범위와 식별자가 같은 대상인지 비교한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool Equals(PresentationTarget other)
        {
            return Scope == other.Scope &&
                string.Equals(ID, other.ID, StringComparison.Ordinal);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 다른 표시 대상과의 동등성을 비교한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Equals(object obj)
        {
            return obj is PresentationTarget other && Equals(other);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 대상의 해시 코드를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public override int GetHashCode()
        {
            return HashCode.Combine((int)Scope, ID);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 두 표시 대상이 같은지 비교한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator ==(PresentationTarget left, PresentationTarget right)
        {
            return left.Equals(right);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 두 표시 대상이 다른지 비교한다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool operator !=(PresentationTarget left, PresentationTarget right)
        {
            return !left.Equals(right);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표시 대상 식별자를 문자열로 표현한다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToString()
        {
            return IsValid ? $"{Scope}:{ID}" : "<invalid>";
        }

    #endregion

    }

    // ============================================================
    /// <summary>
    /// Runtime에서 사용하는 기본 Destination 식별자이다.
    /// </summary>
    // ============================================================
    public static class PresentationDestinationID
    {
        // ------------------------------------------------------------
        /// <summary>
        /// 일반 애플리케이션 화면의 표시 대상이다.
        /// </summary>
        // ------------------------------------------------------------
        public const string Application = "Application";

        // ------------------------------------------------------------
        /// <summary>
        /// 겹쳐 표시되는 콘텐츠의 표시 대상이다.
        /// </summary>
        // ------------------------------------------------------------
        public const string Overlay = "Overlay";

        // ------------------------------------------------------------
        /// <summary>
        /// 모달 화면에 사용하는 표시 대상이다.
        /// </summary>
        // ------------------------------------------------------------
        public const string Modal = "Modal";

        // ------------------------------------------------------------
        /// <summary>
        /// 시스템 화면에 사용하는 표시 대상이다.
        /// </summary>
        // ------------------------------------------------------------
        public const string System = "System";
    }
}
