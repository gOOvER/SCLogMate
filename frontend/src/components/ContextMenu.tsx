import React, { useEffect, useRef } from 'react';

export interface ContextMenuItem {
  label: string;
  icon?: React.ComponentType<{ className?: string }>;
  onClick: () => void;
  disabled?: boolean;
  divider?: boolean;
}

interface ContextMenuProps {
  x: number;
  y: number;
  items: ContextMenuItem[];
  onClose: () => void;
}

export const ContextMenu: React.FC<ContextMenuProps> = ({ x, y, items, onClose }) => {
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleOutsideClick = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        onClose();
      }
    };

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };

    window.addEventListener('mousedown', handleOutsideClick);
    window.addEventListener('keydown', handleKeyDown);
    return () => {
      window.removeEventListener('mousedown', handleOutsideClick);
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [onClose]);

  // Viewport clamping
  const menuWidth = 220;
  const menuHeight = items.length * 36 + 16;
  const adjustedX = Math.min(x, window.innerWidth - menuWidth - 10);
  const adjustedY = Math.min(y, window.innerHeight - menuHeight - 10);

  return (
    <div
      ref={menuRef}
      style={{ left: `${Math.max(10, adjustedX)}px`, top: `${Math.max(10, adjustedY)}px` }}
      className="fixed z-50 min-w-[210px] bg-[#070e1c]/95 border border-cyan-500/40 rounded-lg shadow-[0_8px_30px_rgba(0,0,0,0.85)] backdrop-blur-md py-1.5 animate-fade-in select-none"
    >
      <div className="px-3 py-1 text-[10px] font-mono font-bold text-cyan-400/80 tracking-wider uppercase border-b border-cyan-950/60 mb-1">
        Aktionen
      </div>
      {items.map((item, idx) => {
        if (item.divider) {
          return <div key={idx} className="my-1 border-t border-cyan-950/60" />;
        }

        const Icon = item.icon;
        return (
          <button
            key={idx}
            disabled={item.disabled}
            onClick={() => {
              item.onClick();
              onClose();
            }}
            className={`w-full flex items-center gap-2.5 px-3 py-1.5 text-xs text-left transition cursor-pointer ${
              item.disabled
                ? 'text-slate-600 cursor-not-allowed'
                : 'text-slate-300 hover:text-cyan-200 hover:bg-cyan-950/60 active:bg-cyan-900/60'
            }`}
          >
            {Icon && <Icon className="w-3.5 h-3.5 text-cyan-400 shrink-0" />}
            <span className="truncate">{item.label}</span>
          </button>
        );
      })}
    </div>
  );
};
