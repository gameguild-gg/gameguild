'use client';

import { checkoutMarketplaceEconomyAction, prepareMarketplaceStripeCheckoutAction, type MarketplaceActionResult } from '@/lib/marketplace/actions';
import type { CommerceOrdersMarketplaceCart } from '@game-guild/client';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { useState, useTransition } from 'react';
import { useTranslations } from 'next-intl';
import { MarketplaceActionNotice } from './marketplace-action-notice';
import { StripePaymentElement } from './stripe-payment-element';

export function MarketplaceCheckoutWorkspace({ cart, labels, locale, stripePublishableKey }: {
  cart: CommerceOrdersMarketplaceCart | null;
  labels: { economy: string; empty: string; stripe: string; title: string };
  locale: string;
  stripePublishableKey?: string;
}) {
  const [pending, startTransition] = useTransition();
  const t = useTranslations('marketplace');
  const [result, setResult] = useState<MarketplaceActionResult<unknown> | null>(null);
  const [currencyChoice, setCurrencyChoice] = useState<'Hard' | 'Soft' | 'FixedMix'>('Hard');
  const [stripeOrders, setStripeOrders] = useState<Array<{ clientSecret: string; orderId: string }>>([]);
  if (!cart?.items?.length) return <p className="text-sm text-muted-foreground">{labels.empty}</p>;

  return (
    <Card>
      <CardHeader><CardTitle>{labels.title}</CardTitle></CardHeader>
      <CardContent className="space-y-5">
        <MarketplaceActionNotice result={result} />
        <div className="grid gap-3 sm:grid-cols-3">
          {(['Hard', 'Soft', 'FixedMix'] as const).map((choice) => (
            <Button key={choice} type="button" variant={currencyChoice === choice ? 'default' : 'outline'} onClick={() => setCurrencyChoice(choice)}>
              {choice}
            </Button>
          ))}
        </div>
        <Button disabled={pending} onClick={() => startTransition(async () => setResult(await checkoutMarketplaceEconomyAction(
          cart.version ?? 0, currencyChoice, crypto.randomUUID(),
        )))}>{labels.economy}</Button>
        <div className="grid gap-3 border-t pt-5">
          <Button type="button" variant="outline" disabled={pending || !stripePublishableKey} onClick={() => startTransition(async () => {
            const prepared = await prepareMarketplaceStripeCheckoutAction(cart.version ?? 0, crypto.randomUUID());
            setResult(prepared);
            if (prepared.success && prepared.data) {
              setStripeOrders(prepared.data.orderIds.flatMap((orderId, index) => {
                const clientSecret = prepared.data?.clientActionTokens[index];
                return clientSecret ? [{ orderId, clientSecret }] : [];
              }));
            }
          })}>{labels.stripe}</Button>
          {!stripePublishableKey ? <p className="text-sm text-muted-foreground">{t('stripeBlocked')}</p> : null}
          {stripePublishableKey ? stripeOrders.map((order) => (
            <StripePaymentElement
              key={order.orderId}
              clientSecret={order.clientSecret}
              locale={locale}
              orderId={order.orderId}
              publishableKey={stripePublishableKey}
            />
          )) : null}
        </div>
      </CardContent>
    </Card>
  );
}
