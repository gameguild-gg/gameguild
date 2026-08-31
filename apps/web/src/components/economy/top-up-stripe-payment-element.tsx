'use client';

import { Elements, PaymentElement, useElements, useStripe } from '@stripe/react-stripe-js';
import { loadStripe } from '@stripe/stripe-js';
import { Button } from '@game-guild/ui/components/button';
import { useLocale, useTranslations } from 'next-intl';
import { useMemo, useState, type FormEvent } from 'react';
import { EconomyActionNotice } from './economy-ui';

function TopUpConfirmationForm({ topUpId }: { topUpId: string }) {
  const stripe = useStripe();
  const elements = useElements();
  const locale = useLocale();
  const t = useTranslations('economy');
  const [pending, setPending] = useState(false);
  const [result, setResult] = useState<{ success: boolean; message: string } | null>(null);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!stripe || !elements) return;
    setPending(true);
    const confirmation = await stripe.confirmPayment({
      elements,
      confirmParams: { return_url: `${window.location.origin}/${locale}/workspace/economy/top-ups/${topUpId}` },
      redirect: 'if_required',
    });
    setResult(confirmation.error
      ? { success: false, message: confirmation.error.message ?? t('topUps.confirmationFailed') }
      : { success: true, message: t('topUps.confirmationPending') });
    setPending(false);
  }

  return (
    <form className="space-y-4 rounded-lg border p-4" onSubmit={submit}>
      <PaymentElement />
      <Button disabled={!stripe || !elements || pending} type="submit">{t('topUps.confirm')}</Button>
      <EconomyActionNotice result={result} />
    </form>
  );
}

export function TopUpStripePaymentElement({ clientSecret, publishableKey, topUpId }: {
  clientSecret: string;
  publishableKey: string;
  topUpId: string;
}) {
  const stripe = useMemo(() => loadStripe(publishableKey), [publishableKey]);
  return (
    <Elements options={{ clientSecret, appearance: { theme: 'stripe' } }} stripe={stripe}>
      <TopUpConfirmationForm topUpId={topUpId} />
    </Elements>
  );
}
