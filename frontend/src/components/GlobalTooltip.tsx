import React, { useEffect, useState, useRef } from 'react';
import { createPortal } from 'react-dom';

interface TooltipState {
  visible: boolean;
  content: string;
  top: number;
  left: number;
  placement: 'top' | 'bottom' | 'left' | 'right';
}

export const GlobalTooltip: React.FC = () => {
  const [tooltip, setTooltip] = useState<TooltipState>({
    visible: false,
    content: '',
    top: 0,
    left: 0,
    placement: 'top',
  });

  const activeElementRef = useRef<HTMLElement | null>(null);
  const showTimerRef = useRef<number | null>(null);

  const hideTooltip = () => {
    if (showTimerRef.current) {
      window.clearTimeout(showTimerRef.current);
      showTimerRef.current = null;
    }
    activeElementRef.current = null;
    setTooltip((prev) => (prev.visible ? { ...prev, visible: false } : prev));
  };

  useEffect(() => {
    const handleMouseOver = (e: MouseEvent) => {
      const target = e.target as HTMLElement | null;
      if (!target) return;

      // Find nearest element with data-tooltip or title
      const tooltipEl = target.closest<HTMLElement>('[data-tooltip], [title]');
      if (!tooltipEl) return;

      // Intercept and convert native title to data-tooltip to avoid the buggy, ugly WebView2 native tooltip
      if (tooltipEl.hasAttribute('title')) {
        const rawTitle = tooltipEl.getAttribute('title')?.trim();
        if (rawTitle) {
          tooltipEl.setAttribute('data-tooltip', rawTitle);
        }
        tooltipEl.removeAttribute('title');
      }

      const content = tooltipEl.getAttribute('data-tooltip')?.trim();
      if (!content) return;

      // If already on this element, do nothing
      if (activeElementRef.current === tooltipEl) return;
      activeElementRef.current = tooltipEl;

      if (showTimerRef.current) {
        window.clearTimeout(showTimerRef.current);
      }

      showTimerRef.current = window.setTimeout(() => {
        if (!activeElementRef.current) return;
        const rect = activeElementRef.current.getBoundingClientRect();

        const prefPos = (activeElementRef.current.getAttribute('data-tooltip-pos') || 'top') as
          | 'top'
          | 'bottom'
          | 'left'
          | 'right';

        let top = rect.top - 8;
        let left = rect.left + rect.width / 2;
        let placement: 'top' | 'bottom' | 'left' | 'right' = prefPos;

        if (prefPos === 'top') {
          // If too close to viewport top edge, flip to bottom
          if (rect.top < 45) {
            top = rect.bottom + 8;
            placement = 'bottom';
          }
        } else if (prefPos === 'bottom') {
          top = rect.bottom + 8;
          // If too close to bottom edge, flip to top
          if (rect.bottom > window.innerHeight - 45) {
            top = rect.top - 8;
            placement = 'top';
          }
        } else if (prefPos === 'left') {
          left = rect.left - 8;
          top = rect.top + rect.height / 2;
        } else if (prefPos === 'right') {
          left = rect.right + 8;
          top = rect.top + rect.height / 2;
        }

        // Clamp horizontal coordinates within window padding
        left = Math.max(16, Math.min(window.innerWidth - 16, left));

        setTooltip({
          visible: true,
          content,
          top,
          left,
          placement,
        });
      }, 100);
    };

    const handleMouseOut = (e: MouseEvent) => {
      const target = e.target as HTMLElement | null;
      if (!target || !activeElementRef.current) return;

      // If moving into another child of the active element, don't hide
      const related = e.relatedTarget as HTMLElement | null;
      if (related && activeElementRef.current.contains(related)) {
        return;
      }

      hideTooltip();
    };

    const handleAction = () => {
      hideTooltip();
    };

    document.addEventListener('mouseover', handleMouseOver, { passive: true });
    document.addEventListener('mouseout', handleMouseOut, { passive: true });
    window.addEventListener('scroll', handleAction, { passive: true, capture: true });
    window.addEventListener('click', handleAction, { passive: true });
    window.addEventListener('mousedown', handleAction, { passive: true });

    return () => {
      document.removeEventListener('mouseover', handleMouseOver);
      document.removeEventListener('mouseout', handleMouseOut);
      window.removeEventListener('scroll', handleAction, { capture: true });
      window.removeEventListener('click', handleAction);
      window.removeEventListener('mousedown', handleAction);
      if (showTimerRef.current) {
        window.clearTimeout(showTimerRef.current);
      }
    };
  }, []);

  if (!tooltip.visible || !tooltip.content) {
    return null;
  }

  const transformStyle =
    tooltip.placement === 'top'
      ? 'translate(-50%, -100%)'
      : tooltip.placement === 'bottom'
      ? 'translate(-50%, 0)'
      : tooltip.placement === 'left'
      ? 'translate(-100%, -50%)'
      : 'translate(0, -50%)';

  return createPortal(
    <div
      style={{
        top: `${tooltip.top}px`,
        left: `${tooltip.left}px`,
        transform: transformStyle,
      }}
      className="fixed pointer-events-none z-[99999] max-w-xs px-2.5 py-1.5 rounded-lg bg-[#030914]/95 border border-cyan-500/40 text-slate-200 text-xs font-mono shadow-[0_0_20px_rgba(6,182,212,0.25)] shadow-black/80 backdrop-blur-md animate-in fade-in zoom-in-95 duration-100 flex items-center gap-2"
    >
      <div className="w-1.5 h-1.5 rounded-full bg-cyan-400 shadow-[0_0_8px_rgba(6,182,212,0.9)] shrink-0 animate-pulse" />
      <span className="leading-snug text-[11.5px] tracking-wide break-words">{tooltip.content}</span>
    </div>,
    document.body
  );
};
