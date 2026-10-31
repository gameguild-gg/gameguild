import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { RevenueCurrenciesConsole, type RevenueCurrenciesDataset } from './revenue-currencies-console';

function dataset(overrides: Partial<RevenueCurrenciesDataset> = {}): RevenueCurrenciesDataset {
  return {
    days: 30,
    popular: {
      currencies: [
        { rank: 1, currency: 'USD', transactionCount: 450, totalNet: 125000 },
        { rank: 2, currency: 'EUR', transactionCount: 320, totalNet: 85000 },
      ],
    },
    base: {
      baseCurrency: 'USD',
      baseCreditTotal: 250000,
      baseDebitTotal: 1500,
      baseNetTotal: 248500,
      unconvertedCurrencies: 1,
      attestation: 'Converted at the period-end rate per currency.',
      entries: [
        {
          currency: 'USD',
          creditTotal: 125000,
          debitTotal: 0,
          netTotal: 125000,
          eventCount: 450,
          converted: true,
          rateUsed: 1,
          baseNetTotal: 125000,
          note: null,
        },
        {
          currency: 'EUR',
          creditTotal: 85000,
          debitTotal: 500,
          netTotal: 84500,
          eventCount: 320,
          converted: true,
          rateUsed: 0.85,
          baseNetTotal: 99411.77,
          note: null,
        },
        {
          currency: 'JPY',
          creditTotal: 8500000,
          debitTotal: 0,
          netTotal: 8500000,
          eventCount: 95,
          converted: false,
          rateUsed: null,
          baseNetTotal: null,
          note: 'no-rate',
        },
      ],
    },
    costs: {
      baseCurrency: 'USD',
      totalCostInBase: 120.5,
      entries: [
        {
          currency: 'EUR',
          netVolume: 84500,
          marketRate: 0.85,
          overrideRate: 0.84,
          costInBase: 120.5,
          note: null,
        },
      ],
    },
    ...overrides,
  };
}

describe('RevenueCurrenciesConsole', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal('fetch', vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('renders the base-currency reconciliation with per-currency rows', () => {
    render(<RevenueCurrenciesConsole initialDataset={dataset()} />);

    expect(screen.getByTestId('revenue-currency-totals')).toBeInTheDocument();
    expect(screen.getAllByText('USD').length).toBeGreaterThan(0);
    expect(screen.getByText('$248,500.00')).toBeInTheDocument();
    expect(screen.getByText('unconverted (no-rate)')).toBeInTheDocument();
    expect(screen.getByText(/1 currency\(ies\) could not be converted/)).toBeInTheDocument();
    expect(screen.getByTestId('revenue-currency-attestation').textContent).toContain('period-end rate');
  });

  it('renders the popular-currency ranking and conversion-cost analysis', () => {
    render(<RevenueCurrenciesConsole initialDataset={dataset()} />);

    expect(screen.getByTestId('revenue-popular-table')).toBeInTheDocument();
    expect(screen.getByText('#2')).toBeInTheDocument();
    expect(screen.getAllByText('320').length).toBe(2); // EUR events + EUR transactions
    expect(screen.getByTestId('revenue-conversion-costs-table')).toBeInTheDocument();
    expect(screen.getByText('$120.50')).toBeInTheDocument();
  });

  it('renders fail-closed empty states when the window has no revenue', () => {
    render(
      <RevenueCurrenciesConsole
        initialDataset={dataset({ popular: { currencies: [] }, base: null, costs: null })}
      />,
    );

    expect(screen.getByText(/No revenue events in this window/)).toBeInTheDocument();
    expect(screen.getByText(/No transacted currencies in this window/)).toBeInTheDocument();
    expect(screen.getByText(/No non-base currency volume in this window/)).toBeInTheDocument();
  });

  it('refetches through the scoped proxy when the window changes', async () => {
    (globalThis.fetch as ReturnType<typeof vi.fn>).mockResolvedValue(
      new Response(JSON.stringify(dataset({ days: 7 })), { status: 200 }),
    );
    render(<RevenueCurrenciesConsole initialDataset={dataset()} />);

    await fireEvent.click(screen.getByRole('button', { name: '7d' }));

    await waitFor(() => expect(globalThis.fetch).toHaveBeenCalledWith('/api/billing/revenue-currencies?days=7', {
      credentials: 'same-origin',
    }));
    await waitFor(() => expect(screen.getByRole('button', { name: '7d' })).toHaveAttribute('aria-pressed', 'true'));
  });

  it('surfaces proxy failures without discarding the rendered data', async () => {
    (globalThis.fetch as ReturnType<typeof vi.fn>).mockResolvedValue(
      new Response('boom', { status: 502 }),
    );
    render(<RevenueCurrenciesConsole initialDataset={dataset()} />);

    await fireEvent.click(screen.getByRole('button', { name: '90d' }));

    await waitFor(() => expect(screen.getByText('HTTP 502')).toBeInTheDocument());
    expect(screen.getByText('$248,500.00')).toBeInTheDocument();
  });
});
