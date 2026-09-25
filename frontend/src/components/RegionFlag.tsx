import React from 'react';
import { Globe } from 'lucide-react';

export interface RegionFlagProps {
  regionCode?: string;
  className?: string;
  showTitle?: boolean;
}

export const RegionFlag: React.FC<RegionFlagProps> = ({
  regionCode,
  className = 'w-4 h-[11px] rounded-[1.5px] overflow-hidden border border-cyan-900/60 shrink-0 shadow-xs relative inline-flex items-center justify-center',
  showTitle = true,
}) => {
  const raw = (regionCode || '').trim().toUpperCase();

  // Normalize code
  let code = raw;
  if (code.includes('EU-CENTRAL') || code === 'DE' || code === 'GER') {
    code = 'DE';
  } else if (code.startsWith('EU') || code.includes('LON') || code.includes('IRL')) {
    code = 'EU';
  } else if (code.startsWith('US') || code.includes('NA') || code.includes('VIRGINIA') || code.includes('OREGON')) {
    code = 'US';
  } else if (code.includes('AUS') || code.includes('OCE') || code.includes('SYD') || code.includes('APSE')) {
    code = 'AUS';
  } else if (code.includes('ASIA') || code.includes('HK') || code.includes('JP') || code.includes('APNE')) {
    code = 'ASIA';
  }

  if (code === 'DE') {
    return (
      <span className={`${className} bg-[#000000]`} title={showTitle ? 'Deutschland (DE / EU-Central)' : undefined}>
        <svg viewBox="0 0 16 11" className="w-full h-full block">
          <rect width="16" height="3.67" y="0" fill="#000000" />
          <rect width="16" height="3.67" y="3.67" fill="#DD0000" />
          <rect width="16" height="3.67" y="7.34" fill="#FFCE00" />
        </svg>
      </span>
    );
  }

  if (code === 'EU') {
    return (
      <span className={`${className} bg-[#003399]`} title={showTitle ? 'Europa (EU)' : undefined}>
        <svg viewBox="0 0 16 11" className="w-full h-full block">
          <circle cx="8" cy="1.8" r="0.8" fill="#FFCC00" />
          <circle cx="11.4" cy="2.9" r="0.8" fill="#FFCC00" />
          <circle cx="12.9" cy="5.5" r="0.8" fill="#FFCC00" />
          <circle cx="11.4" cy="8.1" r="0.8" fill="#FFCC00" />
          <circle cx="8" cy="9.2" r="0.8" fill="#FFCC00" />
          <circle cx="4.6" cy="8.1" r="0.8" fill="#FFCC00" />
          <circle cx="3.1" cy="5.5" r="0.8" fill="#FFCC00" />
          <circle cx="4.6" cy="2.9" r="0.8" fill="#FFCC00" />
        </svg>
      </span>
    );
  }

  if (code === 'US') {
    return (
      <span className={`${className} bg-[#B22234]`} title={showTitle ? 'USA / Nordamerika (US)' : undefined}>
        <svg viewBox="0 0 16 11" className="w-full h-full block">
          <rect y="1.8" width="16" height="1.8" fill="#FFFFFF" />
          <rect y="5.4" width="16" height="1.8" fill="#FFFFFF" />
          <rect y="9.0" width="16" height="1.8" fill="#FFFFFF" />
          <rect width="7" height="6" fill="#3C3B6E" />
          <circle cx="2" cy="1.8" r="0.5" fill="#FFFFFF" />
          <circle cx="5" cy="1.8" r="0.5" fill="#FFFFFF" />
          <circle cx="3.5" cy="3" r="0.5" fill="#FFFFFF" />
          <circle cx="2" cy="4.2" r="0.5" fill="#FFFFFF" />
          <circle cx="5" cy="4.2" r="0.5" fill="#FFFFFF" />
        </svg>
      </span>
    );
  }

  if (code === 'AUS') {
    return (
      <span className={`${className} bg-[#00008B]`} title={showTitle ? 'Australien / APAC (AUS)' : undefined}>
        <svg viewBox="0 0 16 11" className="w-full h-full block">
          <rect width="7" height="5.5" fill="#00247D" />
          <path d="M 0,0 L 7,5.5 M 7,0 L 0,5.5" stroke="#FFFFFF" strokeWidth="0.8" />
          <path d="M 0,0 L 7,5.5 M 7,0 L 0,5.5" stroke="#CF142B" strokeWidth="0.4" />
          <rect x="2.5" width="1.8" height="5.5" fill="#FFFFFF" />
          <rect y="2" width="7" height="1.8" fill="#FFFFFF" />
          <rect x="2.9" width="1" height="5.5" fill="#CF142B" />
          <rect y="2.4" width="7" height="1" fill="#CF142B" />
          <circle cx="11.5" cy="2.5" r="0.6" fill="#FFFFFF" />
          <circle cx="13.5" cy="4.5" r="0.6" fill="#FFFFFF" />
          <circle cx="10.5" cy="6.5" r="0.6" fill="#FFFFFF" />
          <circle cx="12.5" cy="8.5" r="0.6" fill="#FFFFFF" />
          <circle cx="3.5" cy="8.2" r="0.8" fill="#FFFFFF" />
        </svg>
      </span>
    );
  }

  if (code === 'ASIA') {
    return (
      <span className={`${className} bg-[#0b1b2d]`} title={showTitle ? 'Asien (ASIA)' : undefined}>
        <Globe className="w-2.5 h-2.5 text-cyan-400" />
      </span>
    );
  }

  return (
    <span className={`${className} bg-[#0b1b2d]`} title={showTitle ? 'Persistent Universe' : undefined}>
      <Globe className="w-2.5 h-2.5 text-cyan-400" />
    </span>
  );
};

export function extractRegionFromText(text?: string): string | undefined {
  if (!text) return undefined;
  const m = text.match(/Server\s+beigetreten:\s*([A-Za-z0-9_-]+)/i)
    || text.match(/Server\s+verbunden:\s*([A-Za-z0-9_-]+)/i)
    || text.match(/\b(EU-Central|EU-West|US-East|US-West|EU|US|AUS|Asia|HK|DE|PU)\b/i);
  return m ? m[1].toUpperCase() : undefined;
}

export function cleanServerEventDescription(text?: string): string {
  if (!text) return '';
  // Strip leading unicode flag emojis (Regional Indicator Symbols U+1F1E6 to U+1F1FF) and globe emoji
  let cleaned = text.replace(/^[\uD83C][\uDDE6-\uDDFF][\uD83C][\uDDE6-\uDDFF]\s*/g, '');
  cleaned = cleaned.replace(/^[🌐🌏🌎🌍]\s*/g, '');
  // Also strip broken double regional indicator letters when rendered as plain text (e.g. "EU Server beigetreten: EU" -> "Server beigetreten: EU")
  cleaned = cleaned.replace(/^(EU|US|DE|AUS|HK)\s+(Server\s+beigetreten)/i, '$2');
  return cleaned.trim();
}
