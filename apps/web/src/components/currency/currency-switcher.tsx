'use client';

import { useCurrency } from '@/components/currency/currency-provider';
import { Button } from '@game-guild/ui/components/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@game-guild/ui/components/select';
import { MapPin, X } from 'lucide-react';
import { useTranslations } from 'next-intl';

/**
 * Navigation currency indicator + selector (issue #412 spec E): shows the
 * active display currency, switches it without a page reload (every price
 * display re-renders through the provider) and surfaces the location-based
 * suggestion with a one-click apply. Responsive: the trigger keeps a compact
 * width in the header and stretches to full width inside mobile menus.
 */
export function CurrencySwitcher({ className }: { className?: string }) {
  const { currency, currencies, suggestion, selectCurrency, acceptSuggestion, ignoreSuggestion } = useCurrency();
  const t = useTranslations('currency');
  const selectable = currencies.some((option) => option.code === currency);

  return (
    <div className={`flex flex-col gap-1 ${className ?? ''}`} data-testid="currency-switcher">
      <div className="flex items-center gap-1.5">
        <span className="sr-only">{t('switchLabel')}</span>
        <Select value={currency} onValueChange={(value) => selectCurrency(value ?? currency)}>
          <SelectTrigger
            aria-label={t('label')}
            data-testid="currency-switcher-trigger"
            className="h-9 w-full gap-1 px-2 text-xs font-medium sm:w-auto sm:min-w-24"
          >
            <SelectValue />
          </SelectTrigger>
          <SelectContent className="max-h-72">
            {!selectable && (
              <SelectItem value={currency}>{currency}</SelectItem>
            )}
            {currencies.map((option) => (
              <SelectItem key={option.code} value={option.code}>
                {option.symbol ? `${option.symbol} ${option.code}` : option.code}
                {option.name ? <span className="text-muted-foreground"> — {option.name}</span> : null}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      {suggestion ? (
        <div
          className="flex items-center gap-1 rounded-md border border-dashed px-1.5 py-1 text-[11px] leading-tight text-muted-foreground"
          data-testid="currency-suggestion"
        >
          <MapPin className="size-3 shrink-0" aria-hidden="true" />
          <span>{t('suggested', { code: suggestion.code })}</span>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="h-5 px-1.5 text-[11px]"
            onClick={acceptSuggestion}
          >
            {t('apply', { code: suggestion.code })}
          </Button>
          <button
            type="button"
            aria-label={t('dismiss')}
            className="rounded-sm p-0.5 hover:text-foreground"
            onClick={ignoreSuggestion}
          >
            <X className="size-3" aria-hidden="true" />
          </button>
        </div>
      ) : null}
    </div>
  );
}
