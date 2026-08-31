'use client';

import { reconcileMarketplaceStripeOrderAction, type MarketplaceActionResult } from '@/lib/marketplace/actions';
import { Elements, PaymentElement, useElements, useStripe } from '@stripe/react-stripe-js';
import { loadStripe } from '@stripe/stripe-js';
import { Button } from '@game-guild/ui/components/button';
import { useMemo, useState, type FormEvent } from 'react';
import { useTranslations } from 'next-intl';
import { MarketplaceActionNotice } from './marketplace-action-notice';

function StripeConfirmationForm({ locale, orderId }: { locale: string; orderId: string }) {
  const stripe = useStripe();
  const elements = useElements();
  const t = useTranslations('marketplace');
  const [pending, setPending] = useState(false);
  const [result, setResult] = useState<MarketplaceActionResult | null>(null);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!stripe || !elements) return;
    setPending(true);
    const confirmation = await stripe.confirmPayment({
      elements,
      confirmParams: { return_url: `${window.location.origin}/${locale}/workspace/economy/orders/${orderId}` },
      redirect: 'if_required',
    });
    if (confirmation.error) {
      setResult({ success: false, message: confirmation.error.message ?? t('stripeConfirmationFailed') });
    } else {
      const paymentMethod = confirmation.paymentIntent?.payment_method;
      const paymentMethodId = typeof paymentMethod === 'string' ? paymentMethod : paymentMethod?.id;
      setResult(paymentMethodId
        ? await reconcileMarketplaceStripeOrderAction(orderId, paymentMethodId)
        : { success: true, message: t('stripeReconciliationPending') });
    }
    setPending(false);
  }

  return (
    <form onSubmit={submit} className="space-y-4 rounded-lg border p-4">
      <PaymentElement />
      <Button type="submit" disabled={!stripe || !elements || pending}>{t('confirmStripe')}</Button>
      <MarketplaceActionNotice result={result} />
    </form>
  );
}

export function StripePaymentElement({ clientSecret, locale, orderId, publishableKey }: {
  clientSecret: string;
  locale: string;
  orderId: string;
  publishableKey: string;
}) {
  const stripe = useMemo(() => loadStripe(publishableKey), [publishableKey]);
  return (
    <Elements stripe={stripe} options={{ clientSecret, appearance: { theme: 'stripe' } }}>
      <StripeConfirmationForm locale={locale} orderId={orderId} />
    </Elements>
  );
}
