'use client';

import {
  FALLBACK_CURRENCY_OPTIONS,
  type CurrencyOption,
  dismissSuggestionFor,
  isSuggestionDismissed,
  normalizeCurrencyCode,
  readStoredCurrency,
  storeCurrency,
} from '@/lib/currency/currency-preference';
import { useLocale } from 'next-intl';
import * as React from 'react';

/**
 * Currency display state for the whole app (issue #412 spec E): the selected
 * display currency, the tenant's enabled currency list and the location-based
 * suggestion. Switching updates every consumer on the spot — no page reload —
 * and the choice is remembered across sessions via local storage.
 */
export interface CurrencyContextValue {
  /** Currently selected display currency (ISO 4217), defaults to "USD". */
  currency: string;
  /** Enabled currencies for the tenant; falls back to a static common set. */
  currencies: readonly CurrencyOption[];
  /** Whether the enabled list came from the tenant (vs. the static fallback). */
  currenciesFromTenant: boolean;
  /** Location-based suggestion, when one was produced and not dismissed. */
  suggestion: CurrencyOption | null;
  /** Selects a display currency and persists it. */
  selectCurrency: (code: string) => void;
  /** Accepts the location-based suggestion. */
  acceptSuggestion: () => void;
  /** Dismisses the suggestion for this browser. */
  ignoreSuggestion: () => void;
  /** Formats an amount in the selected display currency. */
  format: (amount: number, code?: string) => string;
}

const CurrencyContext = React.createContext<CurrencyContextValue | null>(null);

interface CurrencyApiResponse {
  currencies?: Array<{ currencyCode?: string; displaySymbol?: string; englishName?: string; isEnabled?: boolean }>;
  suggestion?: { suggestedCurrency?: string; privacyNotice?: string } | null;
}

function toOptions(payload: CurrencyApiResponse): readonly CurrencyOption[] {
  const rows = Array.isArray(payload?.currencies) ? payload.currencies : [];
  return rows
    .filter((row) => row.isEnabled !== false)
    .map((row) => ({
      code: normalizeCurrencyCode(row.currencyCode) ?? '',
      symbol: typeof row.displaySymbol === 'string' ? row.displaySymbol : '',
      name: typeof row.englishName === 'string' ? row.englishName : '',
    }))
    .filter((option) => option.code.length === 3);
}

/**
 * Mounts the currency display state. One provider per app shell; the enabled
 * list and suggestion load lazily from the authenticated proxy so anonymous
 * visitors still get the (GDPR-minimized) suggestion.
 */
export function CurrencyProvider({
  children,
  initialCurrency,
}: {
  children: React.ReactNode;
  initialCurrency?: string;
}) {
  const locale = useLocale();
  const [currency, setCurrency] = React.useState(() => initialCurrency ?? 'USD');
  const [currencies, setCurrencies] = React.useState<readonly CurrencyOption[]>(FALLBACK_CURRENCY_OPTIONS);
  const [currenciesFromTenant, setCurrenciesFromTenant] = React.useState(false);
  const [suggestionCode, setSuggestionCode] = React.useState<string | null>(null);
  const [suggestionDismissed, setSuggestionDismissed] = React.useState(true);

  // Restore the cross-session preference, then enrich with the tenant list
  // and the location-based suggestion (single fetch through the proxy).
  React.useEffect(() => {
    const stored = readStoredCurrency();
    if (stored) setCurrency(stored);

    let cancelled = false;
    fetch('/api/currencies', { credentials: 'same-origin' })
      .then((response) => (response.ok ? response.json() : null))
      .then((payload: CurrencyApiResponse | null) => {
        if (cancelled || !payload) return;
        const options = toOptions(payload);
        if (options.length > 0) {
          setCurrencies(options);
          setCurrenciesFromTenant(true);
          if (!stored) {
            const base = options.find((option) => option.code === 'USD');
            if (base) setCurrency(base.code);
          }
        }
        const suggested = normalizeCurrencyCode(payload.suggestion?.suggestedCurrency);
        if (suggested) {
          setSuggestionCode(suggested);
          setSuggestionDismissed(isSuggestionDismissed(suggested));
        }
      })
      .catch(() => {
        // Anonymous or offline: the static fallback keeps the switcher usable.
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const suggestion = React.useMemo(() => {
    if (!suggestionCode || suggestionDismissed || suggestionCode === currency) return null;
    return currencies.find((option) => option.code === suggestionCode) ?? { code: suggestionCode, symbol: '', name: suggestionCode };
  }, [currencies, currency, suggestionCode, suggestionDismissed]);

  const value = React.useMemo<CurrencyContextValue>(() => {
    const format = (amount: number, code?: string) => {
      const target = code ?? currency;
      try {
        return new Intl.NumberFormat(locale, { style: 'currency', currency: target }).format(amount);
      } catch {
        return `${amount.toFixed(2)} ${target}`;
      }
    };
    return {
      currency,
      currencies,
      currenciesFromTenant,
      suggestion,
      selectCurrency: (code: string) => {
        const normalized = normalizeCurrencyCode(code);
        if (!normalized) return;
        setCurrency(normalized);
        storeCurrency(normalized);
        setSuggestionDismissed(true);
      },
      acceptSuggestion: () => {
        if (!suggestionCode) return;
        setCurrency(suggestionCode);
        storeCurrency(suggestionCode);
        setSuggestionDismissed(true);
      },
      ignoreSuggestion: () => {
        if (!suggestionCode) return;
        dismissSuggestionFor(suggestionCode);
        setSuggestionDismissed(true);
      },
      format,
    };
  }, [currencies, currenciesFromTenant, currency, locale, suggestion, suggestionCode]);

  return <CurrencyContext.Provider value={value}>{children}</CurrencyContext.Provider>;
}

/** Accesses the currency display state; must be used inside {@link CurrencyProvider}. */
export function useCurrency(): CurrencyContextValue {
  const context = React.useContext(CurrencyContext);
  if (!context) {
    throw new Error('useCurrency must be used within a CurrencyProvider.');
  }
  return context;
}
