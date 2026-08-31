import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  confirmPayment: vi.fn(),
  elementsEnabled: true,
  reconcile: vi.fn(),
  loadStripe: vi.fn(() => Promise.resolve({})),
  stripeEnabled: true,
}));

vi.mock('@/lib/marketplace/actions', () => ({ reconcileMarketplaceStripeOrderAction: mocks.reconcile }));
vi.mock('@stripe/stripe-js', () => ({ loadStripe: mocks.loadStripe }));
vi.mock('@stripe/react-stripe-js', () => ({
  Elements: ({ children }: { children: React.ReactNode }) => <div data-testid="elements">{children}</div>,
  PaymentElement: () => <div data-testid="payment-element" />,
  useElements: () => mocks.elementsEnabled ? {} : null,
  useStripe: () => mocks.stripeEnabled ? { confirmPayment: mocks.confirmPayment } : null,
}));
vi.mock('next-intl', () => ({
  useTranslations: () => (key: string) => ({
    confirmStripe: 'Confirm Stripe payment', completed: 'Completed', notCompleted: 'Not completed',
    stripeConfirmationFailed: 'Stripe confirmation failed.',
    stripeReconciliationPending: 'Stripe reconciliation pending.',
  }[key] ?? key),
}));

import { StripePaymentElement } from './stripe-payment-element';

describe('StripePaymentElement', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.elementsEnabled = true;
    mocks.stripeEnabled = true;
    mocks.reconcile.mockResolvedValue({ success: true, message: 'reconciled' });
  });

  it('confirms through Stripe Elements and sends only the provider payment-method ID to reconciliation', async () => {
    mocks.confirmPayment.mockResolvedValue({ paymentIntent: { payment_method: { id: 'pm_verified' } } });
    render(<StripePaymentElement clientSecret="pi_secret" locale="pt-BR" orderId="order-1" publishableKey="pk_test" />);

    fireEvent.click(screen.getByRole('button', { name: 'Confirm Stripe payment' }));

    await waitFor(() => expect(mocks.reconcile).toHaveBeenCalledWith('order-1', 'pm_verified'));
    expect(mocks.confirmPayment).toHaveBeenCalledWith(expect.objectContaining({
      redirect: 'if_required',
      confirmParams: { return_url: expect.stringContaining('/pt-BR/workspace/economy/orders/order-1') },
    }));
  });

  it('renders a client-safe Stripe error without reconciling', async () => {
    mocks.confirmPayment.mockResolvedValue({ error: { message: 'Card declined.' } });
    render(<StripePaymentElement clientSecret="pi_secret" locale="en-US" orderId="order-2" publishableKey="pk_test" />);
    fireEvent.click(screen.getByRole('button', { name: 'Confirm Stripe payment' }));

    expect(await screen.findByText('Card declined.')).toBeInTheDocument();
    expect(mocks.reconcile).not.toHaveBeenCalled();
  });

  it('supports string payment methods and pending reconciliation', async () => {
    mocks.confirmPayment
      .mockResolvedValueOnce({ paymentIntent: { payment_method: 'pm_string' } })
      .mockResolvedValueOnce({ paymentIntent: {} });
    const first = render(<StripePaymentElement clientSecret="one" locale="en-US" orderId="order-3" publishableKey="pk" />);
    fireEvent.click(screen.getByRole('button', { name: 'Confirm Stripe payment' }));
    await waitFor(() => expect(mocks.reconcile).toHaveBeenCalledWith('order-3', 'pm_string'));
    first.unmount();

    render(<StripePaymentElement clientSecret="two" locale="en-US" orderId="order-4" publishableKey="pk" />);
    fireEvent.click(screen.getByRole('button', { name: 'Confirm Stripe payment' }));
    expect(await screen.findByText('Stripe reconciliation pending.')).toBeInTheDocument();
  });

  it('uses the safe error fallback and waits for both Stripe hooks', async () => {
    mocks.confirmPayment.mockResolvedValueOnce({ error: { message: null } });
    const first = render(<StripePaymentElement clientSecret="one" locale="en-US" orderId="order-5" publishableKey="pk" />);
    fireEvent.click(screen.getByRole('button', { name: 'Confirm Stripe payment' }));
    expect(await screen.findByText('Stripe confirmation failed.')).toBeInTheDocument();
    first.unmount();

    mocks.stripeEnabled = false;
    const second = render(<StripePaymentElement clientSecret="two" locale="en-US" orderId="order-6" publishableKey="pk" />);
    expect(screen.getByRole('button', { name: 'Confirm Stripe payment' })).toBeDisabled();
    fireEvent.submit(screen.getByRole('button', { name: 'Confirm Stripe payment' }).closest('form')!);
    second.unmount();
    mocks.stripeEnabled = true;
    mocks.elementsEnabled = false;
    render(<StripePaymentElement clientSecret="three" locale="en-US" orderId="order-7" publishableKey="pk" />);
    expect(screen.getByRole('button', { name: 'Confirm Stripe payment' })).toBeDisabled();
  });
});
