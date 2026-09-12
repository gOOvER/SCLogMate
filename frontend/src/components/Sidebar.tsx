import React from 'react';
import {
  Award,
  Box,
  Coins,
  Compass,
  FileCode2,
  Info,
  MapPin,
  Maximize2,
  Minimize2,
  Pickaxe,
  Radar,
  Rocket,
  Scroll,
  Settings,
  Shield,
  ShoppingBag,
  Target,
  Wrench,
  BookOpen,
} from 'lucide-react';

export type NavTabId =
  | 'events'
  | 'finances'
  | 'missions'
  | 'reputation'
  | 'starmap'
  | 'places'
  | 'blackbox'
  | 'orescanner'
  | 'market'
  | 'fleet'
  | 'wiki'
  | 'warehouse'
  | 'blueprints'
  | 'loadout'
  | 'tools'
  | 'settings'
  | 'about';

interface NavItem {
  id: NavTabId;
  label: string;
  icon: React.ElementType;
  badge?: string | number;
}

interface NavGroup {
  title: string;
  items: NavItem[];
}

interface SidebarProps {
  activeTab: NavTabId;
  onSelectTab: (id: NavTabId) => void;
  collapsed: boolean;
  onToggleCollapsed: () => void;
  warehouseCount?: number;
  liveEventCount?: number;
}

export const Sidebar: React.FC<SidebarProps> = ({
  activeTab,
  onSelectTab,
  collapsed,
  onToggleCollapsed,
  warehouseCount,
  liveEventCount,
}) => {
  const navGroups: NavGroup[] = [
    {
      title: 'Hauptfunktionen',
      items: [
        { id: 'events', label: 'Ereignisse', icon: Scroll, badge: liveEventCount },
        { id: 'finances', label: 'Finanzen', icon: Coins },
        { id: 'missions', label: 'Missionen', icon: Target },
        { id: 'reputation', label: 'Ruf & Fraktionen', icon: Award },
      ],
    },
    {
      title: 'Universum & Raum',
      items: [
        { id: 'starmap', label: 'Sternenkarte', icon: Radar },
        { id: 'places', label: 'Orte & POIs', icon: Compass },
        { id: 'blackbox', label: 'Flugschreiber', icon: MapPin },
        { id: 'orescanner', label: 'Erz-Scanner', icon: Pickaxe },
      ],
    },
    {
      title: 'Flotte & Inventar',
      items: [
        { id: 'market', label: 'Markt', icon: ShoppingBag },
        { id: 'fleet', label: 'Flotte', icon: Rocket },
        { id: 'wiki', label: 'Wiki Explorer', icon: BookOpen },
        { id: 'warehouse', label: 'Warenlager', icon: Box, badge: warehouseCount },
        { id: 'blueprints', label: 'Baupläne', icon: FileCode2 },
        { id: 'loadout', label: 'Ausrüstung', icon: Shield },
      ],
    },
    {
      title: 'System & Optionen',
      items: [
        { id: 'tools', label: 'Werkzeuge', icon: Wrench },
        { id: 'settings', label: 'Einstellungen', icon: Settings },
        { id: 'about', label: 'Über', icon: Info },
      ],
    },
  ];

  return (
    <aside
      className={`flex flex-col border-r border-slate-800 bg-[#030712]/95 backdrop-blur-md transition-all duration-200 select-none z-20 ${
        collapsed ? 'w-16' : 'w-60'
      }`}
    >
      {/* Brand Header */}
      <div className="flex items-center justify-between px-3 py-3 border-b border-slate-800 h-14">
        {!collapsed ? (
          <div className="flex items-center gap-2.5 overflow-hidden pl-1">
            <div className="flex items-center justify-center w-7 h-7 rounded bg-cyan-950/60 border border-cyan-500/40 text-cyan-400 shrink-0 shadow-[0_0_10px_rgba(0,240,255,0.2)]">
              <Rocket className="w-4 h-4" />
            </div>
            <div className="truncate">
              <span className="text-sm font-bold tracking-wider uppercase text-cyan-300">
                SCLog<span className="text-amber-400 font-extrabold">Mate</span>
              </span>
            </div>
          </div>
        ) : (
          <div className="w-full flex justify-center">
            <div className="flex items-center justify-center w-8 h-8 rounded bg-cyan-950/60 border border-cyan-500/40 text-cyan-400 shrink-0">
              <Rocket className="w-4 h-4" />
            </div>
          </div>
        )}

        <button
          onClick={onToggleCollapsed}
          title={collapsed ? 'Seitenleiste ausklappen' : 'Seitenleiste einklappen'}
          className="p-1 rounded text-slate-400 hover:text-cyan-300 hover:bg-slate-800/60 transition cursor-pointer"
        >
          {collapsed ? <Maximize2 className="w-3.5 h-3.5" /> : <Minimize2 className="w-3.5 h-3.5" />}
        </button>
      </div>

      {/* Navigation List */}
      <div className="flex-1 overflow-y-auto py-2 px-2 space-y-4">
        {navGroups.map((group, gIdx) => (
          <div key={gIdx} className="space-y-1">
            {!collapsed && (
              <div className="px-2.5 py-1 text-[10px] font-mono font-semibold uppercase tracking-wider text-slate-500">
                {group.title}
              </div>
            )}
            {group.items.map((item) => {
              const Icon = item.icon;
              const isActive = activeTab === item.id;
              return (
                <button
                  key={item.id}
                  onClick={() => onSelectTab(item.id)}
                  title={collapsed ? item.label : undefined}
                  className={`w-full flex items-center gap-2.5 px-2.5 py-2 rounded text-xs font-medium transition cursor-pointer group ${
                    isActive
                      ? 'bg-cyan-950/40 text-cyan-300 border border-cyan-500/40 shadow-[0_0_12px_rgba(0,240,255,0.15)] font-semibold'
                      : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900/60 border border-transparent'
                  } ${collapsed ? 'justify-center px-0' : ''}`}
                >
                  <Icon
                    className={`w-4 h-4 shrink-0 transition-transform ${
                      isActive ? 'text-cyan-400 scale-110' : 'text-slate-400 group-hover:text-cyan-300'
                    }`}
                  />
                  {!collapsed && (
                    <span className="truncate flex-1 text-left">{item.label}</span>
                  )}
                  {!collapsed && item.badge !== undefined && (
                    <span
                      className={`px-1.5 py-0.5 rounded text-[10px] font-mono ${
                        isActive
                          ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/30'
                          : 'bg-slate-800 text-slate-400'
                      }`}
                    >
                      {item.badge}
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        ))}
      </div>
    </aside>
  );
};
