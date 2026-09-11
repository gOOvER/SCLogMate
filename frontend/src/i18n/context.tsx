import React, { createContext, useContext, useEffect, useState, useMemo } from 'react';
import { Locale, DEFAULT_LOCALE, SUPPORTED_LOCALES, Translations } from './types';
import { de } from './locales/de';
import { en } from './locales/en';

interface I18nContextType {
  locale: Locale;
  setLocale: (locale: Locale) => void;
  t: (path: string, replacements?: Record<string, string | number>) => string;
  dict: Translations;
}

const dictionaries: Record<Locale, Translations> = {
  de,
  en,
};

const I18nContext = createContext<I18nContextType>({
  locale: DEFAULT_LOCALE,
  setLocale: () => {},
  t: (path) => path,
  dict: de,
});

export const I18nProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [locale, setLocaleState] = useState<Locale>(() => {
    try {
      const stored = localStorage.getItem('sclm_lang') as Locale | null;
      if (stored && SUPPORTED_LOCALES.includes(stored)) {
        return stored;
      }
    } catch {
      // localStorage may not be accessible in some environments
    }
    return DEFAULT_LOCALE;
  });

  useEffect(() => {
    try {
      localStorage.setItem('sclm_lang', locale);
      document.documentElement.lang = locale;
    } catch {
      // ignore
    }
  }, [locale]);

  const setLocale = (newLocale: Locale) => {
    if (!SUPPORTED_LOCALES.includes(newLocale)) return;
    setLocaleState(newLocale);
  };

  const dict = useMemo(() => dictionaries[locale] || dictionaries.de, [locale]);

  const t = (path: string, replacements?: Record<string, string | number>): string => {
    const segments = path.split('.');
    let current: any = dict;

    for (const segment of segments) {
      if (current && typeof current === 'object' && segment in current) {
        current = current[segment];
      } else {
        // Fallback to German
        let fallback: any = dictionaries.de;
        for (const fbSegment of segments) {
          if (fallback && typeof fallback === 'object' && fbSegment in fallback) {
            fallback = fallback[fbSegment];
          } else {
            return path;
          }
        }
        current = fallback;
        break;
      }
    }

    if (typeof current !== 'string') {
      return path;
    }

    if (!replacements) return current;

    return Object.entries(replacements).reduce((acc, [key, val]) => {
      return acc.replace(new RegExp(`\\{${key}\\}`, 'g'), String(val));
    }, current);
  };

  return (
    <I18nContext.Provider value={{ locale, setLocale, t, dict }}>
      {children}
    </I18nContext.Provider>
  );
};

export const useI18n = (): I18nContextType => {
  return useContext(I18nContext);
};
