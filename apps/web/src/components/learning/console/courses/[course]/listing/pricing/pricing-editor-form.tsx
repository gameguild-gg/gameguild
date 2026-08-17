'use client';

import { updateCoursePricing } from '@/lib/learning/actions';
import type { CoursePricing } from '@/lib/learning/queries/listing';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Switch } from '@game-guild/ui/components/switch';
import { Loader2, Save } from 'lucide-react';
import type { FormEvent } from 'react';
import { useState, useTransition } from 'react';

interface PricingEditorFormProps {
  courseId: string;
  pricing: CoursePricing;
}

function getInitialInterval(pricing: CoursePricing): 'one-time' | 'monthly' | 'yearly' {
  return pricing.tiers[0]?.interval ?? 'one-time';
}

export function PricingEditorForm({ courseId, pricing }: PricingEditorFormProps) {
  const tier = pricing.tiers[0];
  const initialInterval = getInitialInterval(pricing);
  const [isPending, startTransition] = useTransition();
  const [isEnabled, setIsEnabled] = useState(pricing.tiers.length > 0);
  const [price, setPrice] = useState(tier?.price.toString() ?? '0');
  const [currency, setCurrency] = useState(tier?.currency ?? 'USD');
  const [interval, setInterval] = useState<'one-time' | 'monthly' | 'yearly'>(initialInterval);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSuccess(false);

    const parsedPrice = Number.parseFloat(price);
    const normalizedCurrency = currency.trim().toUpperCase() || 'USD';
    const subscriptionDurationDays = interval === 'yearly' ? 365 : interval === 'monthly' ? 30 : null;

    startTransition(async () => {
      const result = await updateCoursePricing({
        courseId,
        isMonetizationEnabled: isEnabled,
        price: Number.isFinite(parsedPrice) ? parsedPrice : 0,
        currency: normalizedCurrency,
        isSubscription: interval !== 'one-time',
        subscriptionDurationDays,
      });

      if (!result.success) {
        setError(result.error);
        return;
      }

      setSuccess(true);
    });
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      <div className="flex items-center justify-between gap-4 rounded-lg border p-4">
        <div>
          <Label htmlFor="pricing-enabled">Enable monetization</Label>
          <p className="mt-1 text-sm text-muted-foreground">When enabled, the storefront displays the configured paid access tier.</p>
        </div>
        <Switch id="pricing-enabled" aria-label="Enable monetization" checked={isEnabled} onCheckedChange={setIsEnabled} />
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <div className="grid gap-2">
          <Label htmlFor="pricing-price">Price</Label>
          <Input
            id="pricing-price"
            type="number"
            min="0"
            step="0.01"
            disabled={!isEnabled}
            value={price}
            onChange={(event) => {
              setPrice(event.target.value);
              setSuccess(false);
            }}
          />
        </div>

        <div className="grid gap-2">
          <Label htmlFor="pricing-currency">Currency</Label>
          <Input
            id="pricing-currency"
            maxLength={3}
            disabled={!isEnabled}
            value={currency}
            onChange={(event) => {
              setCurrency(event.target.value);
              setSuccess(false);
            }}
          />
        </div>

        <div className="grid gap-2">
          <Label>Billing interval</Label>
          <Select
            disabled={!isEnabled}
            value={interval}
            onValueChange={(value) => {
              setInterval(value as 'one-time' | 'monthly' | 'yearly');
              setSuccess(false);
            }}
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="one-time">One-time</SelectItem>
              <SelectItem value="monthly">Monthly</SelectItem>
              <SelectItem value="yearly">Yearly</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300">{error}</div>
      ) : null}
      {success ? (
        <div className="rounded-md border border-green-200 bg-green-50 p-3 text-sm text-green-700 dark:border-green-900 dark:bg-green-950 dark:text-green-300">
          Pricing updated successfully.
        </div>
      ) : null}

      <Button type="submit" disabled={isPending}>
        {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Save className="mr-2 size-4" />}
        Save pricing
      </Button>
    </form>
  );
}
