import { act, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  currenciesPayload: null as unknown,
  convertPayload: null as unknown,
}));

vi.mock('next-intl', () => ({
  useLocale: () => 'en-US',
  useTranslations: () => (key: string, values?: Record<string, string | number>) =>
    values ? `${key}:${Object.values(values).join(',')}` : key,
}));

const fetchMock = vi.fn(async (input: string | URL, init?: RequestInit) => {
  const url = String(input);
  if (url === '/api/currencies') {
    return new Response(JSON.stringify(mocks.currenciesPayload ?? {}), { status: 200 });
  }
  if (url === '/api/currency/convert') {
    if (!mocks.convertPayload) {
      return new Response(JSON.stringify({ error: 'unavailable' }), { status: 502 });
    }
    return new Response(JSON.stringify(mocks.convertPayload), { status: 200 });
  }
  throw new Error(`unexpected fetch: ${url} ${init?.method ?? 'GET'}`);
});
vi.stubGlobal('fetch', fetchMock);

import {
  CURRENCY_STORAGE_KEY,
  formatCurrencyAmount,
  normalizeCurrencyCode,
  readStoredCurrency,
  storeCurrency,
} from '@/lib/currency/currency-preference';
import { CurrencyProvider, useCurrency } from './currency-provider';
import { CurrencyPriceDisplay } from './currency-price-display';
import { CurrencySwitcher } from './currency-switcher';

/** Renders the provider plus a probe that mirrors the context for assertions. */
function Probe() {
  const context = useCurrency();
  return (
    <div>
      <span data-testid="probe-currency">{context.currency}</span>
      <span data-testid="probe-suggestion">{context.suggestion?.code ?? 'none'}</span>
      <span data-testid="probe-count">{context.currencies.length}</span>
      <button type="button" data-testid="probe-select" onClick={() => context.selectCurrency('JPY')}>
        select
      </button>
      <button type="button" data-testid="probe-accept" onClick={() => context.acceptSuggestion()}>
        accept
      </button>
      <span data-testid="probe-format">{context.format(12.5)}</span>
    </div>
  );
}

describe('currency preference helpers', () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it('normalizes ISO codes and rejects malformed values', () => {
    expect(normalizeCurrencyCode('eur')).toBe('EUR');
    expect(normalizeCurrencyCode(' D ')).toBeNull();
    expect(normalizeCurrencyCode('EURO')).toBeNull();
    expect(normalizeCurrencyCode(42)).toBeNull();
  });

  it('remembers the preference across sessions through local storage', () => {
    expect(readStoredCurrency()).toBeNull();
    storeCurrency('brl');
    expect(readStoredCurrency()).toBe('BRL');
    expect(window.localStorage.getItem(CURRENCY_STORAGE_KEY)).toBe('BRL');
  });

  it('formats amounts as currency values', () => {
    expect(formatCurrencyAmount(12.34, 'USD', 'en-US')).toContain('12.34');
    // Well-formed but unknown 3-letter codes format with the code shown by Intl.
    expect(formatCurrencyAmount(12.34, 'ZZZ', 'en-US')).toContain('12.34');
    // Malformed codes (non 3-letter) make Intl throw; the helper falls back.
    expect(formatCurrencyAmount(12.34, 'EURO', 'en-US')).toBe('12.34 EURO');
  });
});

describe('CurrencyProvider', () => {
  beforeEach(() => {
    window.localStorage.clear();
    mocks.currenciesPayload = {
      currencies: [
        { currencyCode: 'USD', displaySymbol: '$', englishName: 'US Dollar', isEnabled: true },
        { currencyCode: 'EUR', displaySymbol: '€', englishName: 'Euro', isEnabled: true },
        { currencyCode: 'JPY', displaySymbol: '¥', englishName: 'Japanese Yen', isEnabled: true },
      ],
      suggestion: { suggestedCurrency: 'EUR', privacyNotice: 'nothing stored' },
    };
    mocks.convertPayload = null;
    fetchMock.mockClear();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('loads the tenant currencies and the location-based suggestion', async () => {
    render(
      <CurrencyProvider>
        <Probe />
      </CurrencyProvider>,
    );

    await waitFor(() => expect(screen.getByTestId('probe-count').textContent).toBe('3'));
    expect(screen.getByTestId('probe-suggestion').textContent).toBe('EUR');
    expect(screen.getByTestId('probe-currency').textContent).toBe('USD');
  });

  it('switches the display currency without a reload and persists it', async () => {
    render(
      <CurrencyProvider>
        <Probe />
      </CurrencyProvider>,
    );
    await waitFor(() => expect(screen.getByTestId('probe-count').textContent).toBe('3'));

    await act(async () => {
      screen.getByTestId('probe-select').click();
    });

    expect(screen.getByTestId('probe-currency').textContent).toBe('JPY');
    expect(readStoredCurrency()).toBe('JPY');
    // Selecting a currency also resolves the suggestion.
    expect(screen.getByTestId('probe-suggestion').textContent).toBe('none');
  });

  it('accepts the suggestion in one click', async () => {
    render(
      <CurrencyProvider>
        <Probe />
      </CurrencyProvider>,
    );
    await waitFor(() => expect(screen.getByTestId('probe-suggestion').textContent).toBe('EUR'));

    await act(async () => {
      screen.getByTestId('probe-accept').click();
    });

    expect(screen.getByTestId('probe-currency').textContent).toBe('EUR');
    expect(readStoredCurrency()).toBe('EUR');
  });

  it('restores the cross-session preference from local storage', async () => {
    storeCurrency('JPY');
    render(
      <CurrencyProvider>
        <Probe />
      </CurrencyProvider>,
    );

    await waitFor(() => expect(screen.getByTestId('probe-currency').textContent).toBe('JPY'));
  });

  it('falls back to the static list when the proxy is unavailable', async () => {
    mocks.currenciesPayload = null;
    render(
      <CurrencyProvider>
        <Probe />
        <CurrencySwitcher />
      </CurrencyProvider>,
    );

    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    expect(screen.getByTestId('probe-count').textContent).toBe(String(6)); // static fallback
  });

  it('renders the navigation switcher with an accessible currency trigger', async () => {
    render(
      <CurrencyProvider>
        <CurrencySwitcher />
      </CurrencyProvider>,
    );

    const trigger = await screen.findByTestId('currency-switcher-trigger');
    expect(trigger).toBeInTheDocument();
    expect(trigger.textContent).toContain('USD');
  });
});

describe('CurrencyPriceDisplay (checkout transparency)', () => {
  beforeEach(() => {
    window.localStorage.clear();
    mocks.currenciesPayload = {
      currencies: [
        { currencyCode: 'USD', displaySymbol: '$', englishName: 'US Dollar', isEnabled: true },
        { currencyCode: 'EUR', displaySymbol: '€', englishName: 'Euro', isEnabled: true },
      ],
      suggestion: null,
    };
    mocks.convertPayload = null;
    fetchMock.mockClear();
  });

  it('shows the original price and the converted amount in the selected currency', async () => {
    storeCurrency('EUR');
    mocks.convertPayload = {
      targetCurrency: 'EUR',
      items: [{ sourceAmount: 10, sourceCurrency: 'USD', convertedAmount: 9.1, targetCurrency: 'EUR', rate: 0.91 }],
    };

    render(
      <CurrencyProvider>
        <CurrencyPriceDisplay amount={10} currency="USD" />
      </CurrencyProvider>,
    );

    expect(screen.getByTestId('currency-price-display').textContent).toContain('10');
    await waitFor(() => expect(screen.getByTestId('currency-price-converted')).toBeInTheDocument());
    expect(screen.getByTestId('currency-price-converted').textContent).toContain('9.10');
  });

  it('keeps the original price when a conversion is unavailable (fail visible, convert hidden)', async () => {
    storeCurrency('EUR');

    render(
      <CurrencyProvider>
        <CurrencyPriceDisplay amount={10} currency="USD" />
      </CurrencyProvider>,
    );

    await waitFor(() =>
      expect(screen.getByTestId('currency-price-display').textContent).toContain('10.00'),
    );
    expect(screen.queryByTestId('currency-price-converted')).toBeNull();
  });
});
