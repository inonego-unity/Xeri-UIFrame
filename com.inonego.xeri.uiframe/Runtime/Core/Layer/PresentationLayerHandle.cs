/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : PresentationLayerHandle.cs
수정일 : 2026-11-02

# 설명
PresentationSession Registry의 Layer 등록 소유권과 활성 소비자 수명을 연결한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri.UI.Frame
{
    // ============================================================
    /// <summary>
    /// Presentation Layer 등록 소유권 Handle.
    /// </summary>
    // ============================================================
    internal sealed class PresentationLayerHandle : IDisposable
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 연결된 레이어의 식별자를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string ID { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 레이어 사용권이 반환됐는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool IsDisposed => entry == null;

        // ------------------------------------------------------------
        /// <summary>
        /// 레이어를 사용하는 소비자가 남았는지 확인한다.
        /// </summary>
        // ------------------------------------------------------------
        public bool HasConsumers => entry != null && entry.ConsumerCount > 0;

        private PresentationLayerRegistry owner = null;
        private PresentationLayerRegistry.Entry entry = null;

    #endregion

    #region 생성자

        internal PresentationLayerHandle
        (
            PresentationLayerRegistry owner,
            PresentationLayerRegistry.Entry entry
        )
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.entry = entry ?? throw new ArgumentNullException(nameof(entry));
            ID = entry.ID;
        }

    #endregion

    #region 메서드

        internal void MarkDisposed()
        {
            owner = null;
            entry = null;
        }

    #endregion

    #region 수명 해제

        // ------------------------------------------------------------
        /// <summary>
        /// 활성 소비자가 없을 때 Layer 등록을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public void Dispose()
        {
            if (entry == null) return;

            if (entry.ConsumerCount > 0)
            {
                throw new InvalidOperationException
                (
                    $"Presentation Layer '{ID}'에 활성 소비자가 남아 있습니다."
                );
            }

            var currentOwner = owner;
            var currentEntry = entry;
            owner = null;
            entry = null;
            currentOwner.Unregister(currentEntry);
        }

    #endregion

    }
}
