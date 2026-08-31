import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  confirmPayment: vi.fn(), elementsEnabled: true, loadStripe: vi.fn(() => Promise.resolve({})), stripeEnabled: true,
}));

vi.mock('@stripe/stripe-js', () => ({ loadStripe: mocks.loadStripe }));
vi.mock('@stripe/react-stripe-js', () => ({
  Elements: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  PaymentElement: () => <div data-testid="payment-element" />,
  useElements: () => mocks.elementsEnabled ? {} : null,
  useStripe: () => mocks.stripeEnabled ? { confirmPayment: mocks.confirmPayment } : null,
}));
vi.mock('next-intl', () => ({
  useLocale: () => 'en-US',
  useTranslations: () => (key: string) => key,
}));

import { TopUpStripePaymentElement } from './top-up-stripe-payment-element';

describe('TopUpStripePaymentElement', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.elementsEnabled = true;
    mocks.stripeEnabled = true;
  });

  it('confirms the PaymentIntent while leaving issuance to the webhook', async () => {
    mocks.confirmPayment.mockResolvedValue({ paymentIntent: { status: 'processing' } });
    render(<TopUpStripePaymentElement clientSecret="pi_secret" publishableKey="pk_test" topUpId="topup-1" />);

    fireEvent.click(screen.getByRole('button', { name: 'topUps.confirm' }));

    expect(await screen.findByText('topUps.confirmationPending')).toBeInTheDocument();
    expect(mocks.confirmPayment).toHaveBeenCalledWith(expect.objectContaining({
      redirect: 'if_required',
      confirmParams: { return_url: expect.stringContaining('/en-US/workspace/economy/top-ups/topup-1') },
    }));
  });

  it('renders provider errors with a safe fallback', async () => {
    mocks.confirmPayment.mockResolvedValue({ error: { message: null } });
    render(<TopUpStripePaymentElement clientSecret="pi_secret" publishableKey="pk_test" topUpId="topup-2" />);
    fireEvent.click(screen.getByRole('button', { name: 'topUps.confirm' }));
    expect(await screen.findByText('topUps.confirmationFailed')).toBeInTheDocument();
  });

  it('does not submit before both Stripe and Elements are ready', () => {
    mocks.stripeEnabled = false;
    const first = render(<TopUpStripePaymentElement clientSecret="pi_secret" publishableKey="pk_test" topUpId="topup-3" />);
    expect(screen.getByRole('button', { name: 'topUps.confirm' })).toBeDisabled();
    fireEvent.submit(screen.getByRole('button', { name: 'topUps.confirm' }).closest('form')!);
    first.unmount();
    mocks.stripeEnabled = true;
    mocks.elementsEnabled = false;
    render(<TopUpStripePaymentElement clientSecret="pi_secret" publishableKey="pk_test" topUpId="topup-4" />);
    expect(screen.getByRole('button', { name: 'topUps.confirm' })).toBeDisabled();
    expect(mocks.confirmPayment).not.toHaveBeenCalled();
  });
});
