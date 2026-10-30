'use client';

import { useCurrency } from '@/components/currency/currency-provider';
import { normalizeCurrencyCode } from '@/lib/currency/currency-preference';
import * as React from 'react';
import { useTranslations } from 'next-intl';

/**
 * Checkout/price transparency (issue #412 spec E): renders the original
 * listing price exactly as published plus the approximate value in the
 * visitor's selected display currency (converted server-side through the
 * platform FX seam). Hides the converted line — never the original — when a
 * conversion is unavailable.
 */
export function CurrencyPriceDisplay({
  amount,
  currency,
  className,
}: {
  amount: number;
  currency: string;
  className?: string;
}) {
  const { currency: target, format } = useCurrency();
  const t = useTranslations('currency');
  const source = normalizeCurrencyCode(currency);
  // The latest conversion response, keyed by the inputs it answers for: stale
  // responses for previous amount/currency pairs are ignored on render.
  const [result, setResult] = React.useState<{
    key: string;
    converted: number | null;
    unavailable: boolean;
  } | null>(null);
  const key = `${source ?? ''}|${target}|${amount}`;

  React.useEffect(() => {
    if (!source || source === target) {
      return () => {};
    }

    let cancelled = false;
    fetch('/api/currency/convert', {
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ targetCurrency: target, items: [{ amount, currency: source }] }),
    })
      .then((response) => (response.ok ? response.json() : null))
      .then((payload: { items?: Array<{ convertedAmount?: number | null; error?: string | null }> } | null) => {
        if (cancelled) return;
        const value = payload?.items?.[0]?.convertedAmount;
        setResult({
          key,
          converted: typeof value === 'number' ? value : null,
          unavailable: typeof value !== 'number',
        });
      })
      .catch(() => {
        if (!cancelled) setResult({ key, converted: null, unavailable: true });
      });
    return () => {
      cancelled = true;
    };
  }, [amount, key, source, target]);

  const current = result?.key === key ? result : null;
  const original = `${new Intl.NumberFormat('en-US', { style: 'currency', currency: source ?? 'USD' }).format(amount)} ${source}`;

  return (
    <span className={`inline-flex flex-col text-xs leading-tight ${className ?? ''}`} data-testid="currency-price-display">
      <span>{original}</span>
      {current?.converted != null ? (
        <span className="text-muted-foreground" data-testid="currency-price-converted">
          {t('conversionNote', { converted: format(current.converted) })}
        </span>
      ) : null}
      {current?.unavailable && source !== target ? (
        <span className="text-muted-foreground">{t('conversionUnavailable', { code: target })}</span>
      ) : null}
    </span>
  );
}
