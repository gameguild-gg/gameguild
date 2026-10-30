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
  const [converted, setConverted] = React.useState<number | null>(null);
  const [unavailable, setUnavailable] = React.useState(false);

  React.useEffect(() => {
    let cancelled = false;
    setUnavailable(false);
    if (!source || source === target) {
      setConverted(null);
      return () => {
        cancelled = true;
      };
    }

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
        if (typeof value === 'number') {
          setConverted(value);
        } else {
          setUnavailable(true);
        }
      })
      .catch(() => {
        if (!cancelled) setUnavailable(true);
      });
    return () => {
      cancelled = true;
    };
  }, [amount, source, target]);

  const original = `${new Intl.NumberFormat('en-US', { style: 'currency', currency: source ?? 'USD' }).format(amount)} ${source}`;

  return (
    <span className={`inline-flex flex-col text-xs leading-tight ${className ?? ''}`} data-testid="currency-price-display">
      <span>{original}</span>
      {converted !== null ? (
        <span className="text-muted-foreground" data-testid="currency-price-converted">
          {t('conversionNote', { converted: format(converted) })}
        </span>
      ) : null}
      {unavailable && source !== target ? (
        <span className="text-muted-foreground">{t('conversionUnavailable', { code: target })}</span>
      ) : null}
    </span>
  );
}
