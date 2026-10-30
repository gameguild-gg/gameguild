/**
 * Currency display preference (issue #412 spec E): client-side storage and
 * formatting helpers shared by the currency provider, the navigation switcher
 * and the transparent price displays. The preference is remembered across
 * sessions in local storage; the account-level preference (workspace settings)
 * remains the server-side source synced through the localization settings.
 */

/** Local-storage key holding the visitor's selected display currency. */
export const CURRENCY_STORAGE_KEY = 'gg.currency-preference';

/** Local-storage key marking a dismissed currency suggestion (per currency). */
export const SUGGESTION_DISMISSED_KEY = 'gg.currency-suggestion-dismissed';

export interface CurrencyOption {
  readonly code: string;
  readonly symbol: string;
  readonly name: string;
}

/**
 * Static fallback used when the tenant currency list cannot be fetched
 * (anonymous visitors on public pages, API hiccup). Mirrors the payment
 * supported set; the switcher marks it as a fallback in that state.
 */
export const FALLBACK_CURRENCY_OPTIONS: readonly CurrencyOption[] = [
  { code: 'USD', symbol: '$', name: 'US Dollar' },
  { code: 'EUR', symbol: '€', name: 'Euro' },
  { code: 'GBP', symbol: '£', name: 'British Pound' },
  { code: 'JPY', symbol: '¥', name: 'Japanese Yen' },
  { code: 'CAD', symbol: 'CA$', name: 'Canadian Dollar' },
  { code: 'AUD', symbol: 'A$', name: 'Australian Dollar' },
];

/** Normalizes a candidate ISO 4217 code; returns null when malformed. */
export function normalizeCurrencyCode(value: unknown): string | null {
  if (typeof value !== 'string') return null;
  const code = value.trim().toUpperCase();
  return /^[A-Z]{3}$/.test(code) ? code : null;
}

/** Reads the persisted display currency, null when absent or malformed. */
export function readStoredCurrency(): string | null {
  if (typeof window === 'undefined') return null;
  try {
    return normalizeCurrencyCode(window.localStorage.getItem(CURRENCY_STORAGE_KEY));
  } catch {
    return null;
  }
}

/** Persists the display currency (cross-session memory for this browser). */
export function storeCurrency(code: string): void {
  const normalized = normalizeCurrencyCode(code);
  if (!normalized) return;
  if (typeof window === 'undefined') return;
  try {
    window.localStorage.setItem(CURRENCY_STORAGE_KEY, normalized);
  } catch {
    // Storage may be unavailable (private mode); the session state still works.
  }
  notifyCurrencyPreferenceListeners();
}

type CurrencyPreferenceListener = () => void;

const currencyPreferenceListeners = new Set<CurrencyPreferenceListener>();

function notifyCurrencyPreferenceListeners(): void {
  for (const listener of currencyPreferenceListeners) {
    listener();
  }
}

/**
 * Subscribes to display-currency changes: same-tab writes through
 * {@link storeCurrency} plus cross-tab `storage` events. Feeds
 * `useSyncExternalStore`, which keeps the selected currency hydration-safe
 * (server render sees no preference; the client picks it up after mount).
 */
export function subscribeCurrencyPreference(listener: CurrencyPreferenceListener): () => void {
  currencyPreferenceListeners.add(listener);
  const onStorage = (event: StorageEvent) => {
    if (event.key === CURRENCY_STORAGE_KEY) listener();
  };
  if (typeof window !== 'undefined') {
    window.addEventListener('storage', onStorage);
  }
  return () => {
    currencyPreferenceListeners.delete(listener);
    if (typeof window !== 'undefined') {
      window.removeEventListener('storage', onStorage);
    }
  };
}

/** Whether the visitor dismissed the suggestion for the given currency. */
export function isSuggestionDismissed(code: string): boolean {
  if (typeof window === 'undefined') return true;
  try {
    return window.localStorage.getItem(SUGGESTION_DISMISSED_KEY) === code;
  } catch {
    return true;
  }
}

/** Marks the suggestion as dismissed for the given currency. */
export function dismissSuggestionFor(code: string): void {
  if (typeof window === 'undefined') return;
  try {
    window.localStorage.setItem(SUGGESTION_DISMISSED_KEY, code);
  } catch {
    // Best-effort only.
  }
}

const formatterCache = new Map<string, Intl.NumberFormat>();

function currencyFormatter(locale: string, code: string): Intl.NumberFormat {
  const key = `${locale}:${code}`;
  let formatter = formatterCache.get(key);
  if (!formatter) {
    formatter = new Intl.NumberFormat(locale, { style: 'currency', currency: code });
    formatterCache.set(key, formatter);
  }
  return formatter;
}

/** Formats an amount as a currency value ("US$ 12.34") honoring the locale. */
export function formatCurrencyAmount(amount: number, code: string, locale = 'en-US'): string {
  try {
    return currencyFormatter(locale, code).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${code}`;
  }
}
