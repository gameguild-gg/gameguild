'use client';

import { Button } from '@game-guild/ui/components/button';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@game-guild/ui/components/table';
import { Download, RefreshCw } from 'lucide-react';
import * as React from 'react';

/**
 * Shape of the scoped dashboard payload (issue #412 spec F): the API DTOs of
 * the popular-currency ranking, the base-currency revenue report and the
 * conversion-cost analysis, narrowed to the fields rendered here.
 */
export interface RevenueCurrenciesDataset {
  readonly days: number;
  readonly popular: {
    currencies?: Array<{
      rank?: number;
      currency?: string;
      transactionCount?: number;
      totalNet?: number;
    }>;
  } | null;
  readonly base: {
    baseCurrency?: string;
    baseCreditTotal?: number;
    baseDebitTotal?: number;
    baseNetTotal?: number;
    unconvertedCurrencies?: number;
    attestation?: string;
    entries?: Array<{
      currency?: string;
      creditTotal?: number;
      debitTotal?: number;
      netTotal?: number;
      eventCount?: number;
      converted?: boolean;
      rateUsed?: number | null;
      baseNetTotal?: number | null;
      note?: string | null;
    }>;
  } | null;
  readonly costs: {
    baseCurrency?: string;
    totalCostInBase?: number;
    entries?: Array<{
      currency?: string;
      netVolume?: number;
      marketRate?: number | null;
      overrideRate?: number | null;
      costInBase?: number | null;
      note?: string | null;
    }>;
  } | null;
}

const WINDOWS = [7, 30, 90] as const;

function formatAmount(amount: number | null | undefined, currency: string | undefined): string {
  if (typeof amount !== 'number' || Number.isNaN(amount)) return '—';
  try {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency ?? 'USD',
      maximumFractionDigits: 2,
    }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency ?? ''}`.trim();
  }
}

function formatRate(rate: number | null | undefined): string {
  return typeof rate === 'number' ? rate.toFixed(4) : '—';
}

/**
 * Multi-currency revenue dashboard (issue #412 spec F): per-currency revenue
 * with the base-currency reconciliation, the popular-currency ranking and the
 * conversion-cost analysis, over a selectable window. CSV export hands the
 * base-currency report to accounting. Amounts are never consolidated across
 * currencies except through the labeled base-currency conversion.
 */
export function RevenueCurrenciesConsole({ initialDataset }: { initialDataset: RevenueCurrenciesDataset }) {
  const [dataset, setDataset] = React.useState(initialDataset);
  const [pending, startTransition] = React.useTransition();
  const [error, setError] = React.useState<string | null>(null);

  const load = React.useCallback((days: number) => {
    startTransition(async () => {
      try {
        const response = await fetch(`/api/billing/revenue-currencies?days=${days}`, {
          credentials: 'same-origin',
        });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const payload = (await response.json()) as RevenueCurrenciesDataset;
        setDataset(payload);
        setError(null);
      } catch (cause) {
        setError(cause instanceof Error ? cause.message : 'unknown error');
      }
    });
  }, []);

  function downloadCsv() {
    const url = `/api/billing/revenue-currencies?days=${dataset.days}&format=csv`;
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.rel = 'noopener';
    document.body.append(anchor);
    anchor.click();
    anchor.remove();
  }

  const base = dataset.base;
  const popular = dataset.popular?.currencies ?? [];
  const costs = dataset.costs?.entries ?? [];
  const baseCurrency = base?.baseCurrency ?? 'USD';

  return (
    <div className="space-y-6" data-testid="revenue-currencies-console">
      <div className="flex flex-wrap items-center gap-2">
        {WINDOWS.map((window) => (
          <Button
            key={window}
            type="button"
            size="sm"
            variant={dataset.days === window ? 'default' : 'outline'}
            disabled={pending}
            onClick={() => load(window)}
            aria-pressed={dataset.days === window}
          >
            {window}d
          </Button>
        ))}
        <Button type="button" size="sm" variant="outline" disabled={pending} onClick={() => load(dataset.days)}>
          <RefreshCw className="size-4" aria-hidden="true" />
          Refresh
        </Button>
        <Button type="button" size="sm" variant="outline" onClick={downloadCsv}>
          <Download className="size-4" aria-hidden="true" />
          Export CSV (base currency)
        </Button>
        {error ? <span className="text-sm text-destructive">{error}</span> : null}
      </div>

      <section className="grid gap-3 sm:grid-cols-4" data-testid="revenue-currency-totals">
        <div className="rounded-lg border p-3">
          <p className="text-xs text-muted-foreground">Base currency</p>
          <p className="text-lg font-semibold">{baseCurrency}</p>
        </div>
        <div className="rounded-lg border p-3">
          <p className="text-xs text-muted-foreground">Credits ({baseCurrency})</p>
          <p className="text-lg font-semibold">{formatAmount(base?.baseCreditTotal, baseCurrency)}</p>
        </div>
        <div className="rounded-lg border p-3">
          <p className="text-xs text-muted-foreground">Debits ({baseCurrency})</p>
          <p className="text-lg font-semibold">{formatAmount(base?.baseDebitTotal, baseCurrency)}</p>
        </div>
        <div className="rounded-lg border p-3">
          <p className="text-xs text-muted-foreground">Net ({baseCurrency})</p>
          <p className="text-lg font-semibold">{formatAmount(base?.baseNetTotal, baseCurrency)}</p>
        </div>
      </section>
      {base?.unconvertedCurrencies ? (
        <p className="text-sm text-muted-foreground">
          {base.unconvertedCurrencies} currency(ies) could not be converted at the period-end rate and are excluded
          from the base totals (listed fail-closed below).
        </p>
      ) : null}

      <section className="space-y-2">
        <h2 className="text-lg font-semibold">Revenue by currency (reconciled to {baseCurrency})</h2>
        <Table data-testid="revenue-currency-table">
          <TableHeader>
            <TableRow>
              <TableHead>Currency</TableHead>
              <TableHead className="text-right">Events</TableHead>
              <TableHead className="text-right">Credits</TableHead>
              <TableHead className="text-right">Debits</TableHead>
              <TableHead className="text-right">Net</TableHead>
              <TableHead className="text-right">Rate used</TableHead>
              <TableHead className="text-right">Net in {baseCurrency}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(base?.entries ?? []).map((entry) => (
              <TableRow key={entry.currency}>
                <TableCell className="font-medium">{entry.currency ?? '—'}</TableCell>
                <TableCell className="text-right">{entry.eventCount ?? 0}</TableCell>
                <TableCell className="text-right">{formatAmount(entry.creditTotal, entry.currency)}</TableCell>
                <TableCell className="text-right">{formatAmount(entry.debitTotal, entry.currency)}</TableCell>
                <TableCell className="text-right">{formatAmount(entry.netTotal, entry.currency)}</TableCell>
                <TableCell className="text-right" title={entry.note ?? undefined}>
                  {formatRate(entry.rateUsed)}
                </TableCell>
                <TableCell className="text-right">
                  {entry.converted ? formatAmount(entry.baseNetTotal, baseCurrency) : `unconverted (${entry.note ?? 'no rate'})`}
                </TableCell>
              </TableRow>
            ))}
            {(base?.entries ?? []).length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="text-muted-foreground">
                  No revenue events in this window.
                </TableCell>
              </TableRow>
            ) : null}
          </TableBody>
        </Table>
      </section>

      <section className="space-y-2">
        <h2 className="text-lg font-semibold">Most popular currencies by transactions</h2>
        <Table data-testid="revenue-popular-table">
          <TableHeader>
            <TableRow>
              <TableHead>Rank</TableHead>
              <TableHead>Currency</TableHead>
              <TableHead className="text-right">Transactions</TableHead>
              <TableHead className="text-right">Net volume</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {popular.map((entry) => (
              <TableRow key={`${entry.rank}-${entry.currency}`}>
                <TableCell>#{entry.rank ?? '—'}</TableCell>
                <TableCell className="font-medium">{entry.currency ?? '—'}</TableCell>
                <TableCell className="text-right">{entry.transactionCount ?? 0}</TableCell>
                <TableCell className="text-right">{formatAmount(entry.totalNet, entry.currency)}</TableCell>
              </TableRow>
            ))}
            {popular.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="text-muted-foreground">
                  No transacted currencies in this window.
                </TableCell>
              </TableRow>
            ) : null}
          </TableBody>
        </Table>
      </section>

      <section className="space-y-2">
        <h2 className="text-lg font-semibold">
          Conversion costs (market vs override rate, {baseCurrency})
        </h2>
        <Table data-testid="revenue-conversion-costs-table">
          <TableHeader>
            <TableRow>
              <TableHead>Currency</TableHead>
              <TableHead className="text-right">Net volume</TableHead>
              <TableHead className="text-right">Market rate</TableHead>
              <TableHead className="text-right">Override rate</TableHead>
              <TableHead className="text-right">Cost in {baseCurrency}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {costs.map((entry) => (
              <TableRow key={entry.currency}>
                <TableCell className="font-medium">{entry.currency ?? '—'}</TableCell>
                <TableCell className="text-right">{formatAmount(entry.netVolume, entry.currency)}</TableCell>
                <TableCell className="text-right" title={entry.note ?? undefined}>
                  {formatRate(entry.marketRate)}
                </TableCell>
                <TableCell className="text-right">{formatRate(entry.overrideRate)}</TableCell>
                <TableCell className="text-right">{formatAmount(entry.costInBase, baseCurrency)}</TableCell>
              </TableRow>
            ))}
            {costs.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-muted-foreground">
                  No non-base currency volume in this window.
                </TableCell>
              </TableRow>
            ) : null}
          </TableBody>
        </Table>
      </section>

      {base?.attestation ? (
        <p className="text-xs text-muted-foreground" data-testid="revenue-currency-attestation">
          {base.attestation}
        </p>
      ) : null}
    </div>
  );
}
