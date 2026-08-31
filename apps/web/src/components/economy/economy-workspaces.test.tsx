import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  refresh: vi.fn(),
  createBounty: vi.fn(),
  claimBounty: vi.fn(),
  reclaimBounty: vi.fn(),
  payoutOnboarding: vi.fn(),
  submitPayout: vi.fn(),
  cancelPayout: vi.fn(),
  createTopUp: vi.fn(),
  createTransfer: vi.fn(),
  stripeProps: null as Record<string, unknown> | null,
}));

vi.mock('next/navigation', () => ({ useRouter: () => ({ refresh: mocks.refresh }) }));
vi.mock('next-intl', () => ({ useTranslations: () => (key: string) => key }));
vi.mock('@/i18n/navigation', () => ({ Link: ({ children, href, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement>) => <a href={String(href)} {...props}>{children}</a> }));
vi.mock('@/lib/economy/actions', () => ({
  createBountyAction: mocks.createBounty,
  claimBountyAction: mocks.claimBounty,
  reclaimBountyAction: mocks.reclaimBounty,
  createPayoutOnboardingAction: mocks.payoutOnboarding,
  submitPayoutRequestAction: mocks.submitPayout,
  cancelPayoutRequestAction: mocks.cancelPayout,
  createTopUpAction: mocks.createTopUp,
  createTransferAction: mocks.createTransfer,
}));
vi.mock('./top-up-stripe-payment-element', () => ({
  TopUpStripePaymentElement: (props: Record<string, unknown>) => {
    mocks.stripeProps = props;
    return <div data-testid="top-up-stripe" />;
  },
}));

import { EconomyBountiesWorkspace } from './economy-bounties-workspace';
import { EconomyPayoutsWorkspace } from './economy-payouts-workspace';
import { EconomyTopUpsWorkspace } from './economy-top-ups-workspace';
import { EconomyTransfersWorkspace } from './economy-transfers-workspace';

const success = { success: true, message: 'recorded' };
const failure = { success: false, message: 'blocked' };

describe('Economy self-service workspaces', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.stripeProps = null;
    mocks.createBounty.mockResolvedValue(success);
    mocks.claimBounty.mockResolvedValue(success);
    mocks.reclaimBounty.mockResolvedValue(success);
    mocks.payoutOnboarding.mockResolvedValue(success);
    mocks.submitPayout.mockResolvedValue(success);
    mocks.cancelPayout.mockResolvedValue(success);
    mocks.createTopUp.mockResolvedValue({
      ...success,
      data: { clientSecret: 'secret', publishableKey: 'pk', topUpId: 'top-up' },
    });
    mocks.createTransfer.mockResolvedValue(success);
  });

  it('creates, claims, and reclaims tenant-scoped bounties', async () => {
    const data = {
      issue: 'bounty warning',
      bounties: [
        { id: { value: 'open' }, status: 'Open', amount: { units: 10, currency: 'HardCoin' }, expiresAt: '2026-09-01T00:00:00.000Z' },
        { id: { value: 'expired' }, status: 'Expired', amount: { units: 20, currency: 'SoftCoin' }, expiresAt: null },
        { id: undefined, status: 'Cancelled', amount: null, expiresAt: undefined },
      ],
    };
    render(<EconomyBountiesWorkspace data={data as never} />);
    fireEvent.change(screen.getByLabelText('common.amount'), { target: { value: '100' } });
    fireEvent.change(screen.getByLabelText('bounties.expires'), { target: { value: '2026-09-01T12:00' } });
    fireEvent.change(screen.getByLabelText('bounties.minimumReputation'), { target: { value: '5' } });
    fireEvent.click(screen.getByLabelText('bounties.prerequisite'));
    fireEvent.click(screen.getByLabelText('bounties.instructor'));
    fireEvent.submit(screen.getByRole('button', { name: 'bounties.create' }).closest('form')!);
    await waitFor(() => expect(mocks.createBounty).toHaveBeenCalledWith(expect.objectContaining({
      amountUnits: 100,
      minimumReputation: 5,
      requiresPrerequisite: true,
      requiresInstructorVerification: true,
    })));
    await waitFor(() => expect(screen.getAllByRole('button', { name: 'bounties.claim' })[0]).toBeEnabled());

    fireEvent.click(screen.getAllByRole('button', { name: 'bounties.claim' })[0]!);
    await waitFor(() => expect(mocks.claimBounty).toHaveBeenCalledWith('open', expect.any(String)));
    await waitFor(() => expect(screen.getAllByRole('button', { name: 'bounties.reclaim' })[1]).toBeEnabled());
    fireEvent.click(screen.getAllByRole('button', { name: 'bounties.reclaim' })[1]!);
    await waitFor(() => expect(mocks.reclaimBounty).toHaveBeenCalledWith('expired', expect.any(String)));
    expect(mocks.refresh).toHaveBeenCalled();
    expect(screen.getByRole('link', { name: 'open' })).toHaveAttribute('href', '/workspace/economy/bounties/open');
  });

  it('does not refresh bounties after a failed command', async () => {
    mocks.createBounty.mockResolvedValueOnce(failure);
    render(<EconomyBountiesWorkspace data={{ issue: null, bounties: [] }} />);
    fireEvent.change(screen.getByLabelText('common.amount'), { target: { value: '1' } });
    fireEvent.change(screen.getByLabelText('bounties.expires'), { target: { value: '2026-09-01T12:00' } });
    fireEvent.submit(screen.getByRole('button', { name: 'bounties.create' }).closest('form')!);
    await screen.findByText('blocked');
    expect(mocks.refresh).not.toHaveBeenCalled();
  });

  it('runs payout onboarding, request, cancel, and operation navigation', async () => {
    const data = {
      issue: 'payout warning',
      account: { state: 'Pending', payoutsEnabled: true },
      requests: [
        { id: 'submitted', hardCoinUnits: 10, state: 'Submitted', updatedAt: '2026-08-30T12:00:00.000Z' },
        { id: 'approved', hardCoinUnits: 20, state: 'Approved', updatedAt: null },
        { id: undefined, hardCoinUnits: 1, state: 'Submitted', updatedAt: null },
      ],
      operations: [{ id: 'operation', hardCoinUnits: 10, state: 'Ambiguous', updatedAt: null }],
    };
    render(<EconomyPayoutsWorkspace data={data as never} />);
    fireEvent.click(screen.getByRole('button', { name: 'payouts.onboarding' }));
    await waitFor(() => expect(mocks.payoutOnboarding).toHaveBeenCalled());
    await waitFor(() => expect(screen.getByRole('button', { name: 'payouts.request' })).toBeEnabled());
    fireEvent.change(screen.getByRole('spinbutton'), { target: { value: '25' } });
    fireEvent.submit(screen.getByRole('button', { name: 'payouts.request' }).closest('form')!);
    await waitFor(() => expect(mocks.submitPayout).toHaveBeenCalledWith(25, expect.any(String)));
    await waitFor(() => expect(screen.getAllByRole('button', { name: 'common.cancel' })[0]).toBeEnabled());
    fireEvent.click(screen.getAllByRole('button', { name: 'common.cancel' })[0]!);
    await waitFor(() => expect(mocks.cancelPayout).toHaveBeenCalledWith('submitted'));
    await waitFor(() => expect(screen.getAllByRole('button', { name: 'common.cancel' })[2]).toBeEnabled());
    fireEvent.click(screen.getAllByRole('button', { name: 'common.cancel' })[2]!);
    await waitFor(() => expect(mocks.cancelPayout).toHaveBeenCalledWith(''));
    expect(screen.getByRole('link', { name: 'operation' })).toHaveAttribute('href', '/workspace/economy/payouts/operation');
    expect(mocks.refresh).toHaveBeenCalled();
  });

  it('shows payout provider fallback states and avoids refresh on failure', async () => {
    mocks.payoutOnboarding.mockResolvedValueOnce(failure);
    render(<EconomyPayoutsWorkspace data={{ account: null, issue: null, requests: [], operations: [] }} />);
    expect(screen.getByText('payouts.notConnected')).toBeInTheDocument();
    expect(screen.getByText('payouts.providerNotReady')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'payouts.onboarding' }));
    await screen.findByText('blocked');
    expect(mocks.refresh).not.toHaveBeenCalled();
  });

  it('follows a server-issued payout onboarding location', async () => {
    mocks.payoutOnboarding.mockResolvedValueOnce({ success: true, message: 'redirect', data: { onboardingUri: '#stripe-onboarding' } });
    render(<EconomyPayoutsWorkspace data={{ account: null, issue: null, requests: [], operations: [] }} />);
    fireEvent.click(screen.getByRole('button', { name: 'payouts.onboarding' }));
    await waitFor(() => expect(window.location.hash).toBe('#stripe-onboarding'));
  });

  it('creates a Stripe top-up and renders durable top-up history', async () => {
    const data = {
      issue: 'top-up warning',
      topUps: [
        { topUpId: 'one', hardCoinUnits: 100, status: 'Pending', providerBoundAt: '2026-08-30T12:00:00.000Z', requestedAt: '2026-08-29T12:00:00.000Z' },
        { topUpId: 'two', hardCoinUnits: 200, status: 'Succeeded', providerBoundAt: null, requestedAt: '2026-08-29T12:00:00.000Z' },
      ],
    };
    render(<EconomyTopUpsWorkspace data={data as never} />);
    fireEvent.change(screen.getByLabelText('topUps.units'), { target: { value: '500' } });
    fireEvent.submit(screen.getByRole('button', { name: 'topUps.create' }).closest('form')!);
    await screen.findByTestId('top-up-stripe');
    expect(mocks.createTopUp).toHaveBeenCalledWith(500, expect.any(String));
    expect(mocks.stripeProps).toEqual({ clientSecret: 'secret', publishableKey: 'pk', topUpId: 'top-up' });
    expect(screen.getByRole('link', { name: 'one' })).toHaveAttribute('href', '/workspace/economy/top-ups/one');
    expect(mocks.refresh).toHaveBeenCalled();
  });

  it('shows empty top-up state and keeps failed requests local', async () => {
    mocks.createTopUp.mockResolvedValueOnce(failure);
    render(<EconomyTopUpsWorkspace data={{ issue: null, topUps: [] }} />);
    expect(screen.getByText('common.empty')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('topUps.units'), { target: { value: '10' } });
    fireEvent.submit(screen.getByRole('button', { name: 'topUps.create' }).closest('form')!);
    await screen.findByText('blocked');
    expect(screen.queryByTestId('top-up-stripe')).not.toBeInTheDocument();
    expect(mocks.refresh).not.toHaveBeenCalled();
  });

  it('submits typed transfers and renders journal history', async () => {
    render(<EconomyTransfersWorkspace transactions={[{
      journalEntryId: 'entry', amountUnits: 12, currency: 'HardCoin', templateKind: 'Transfer', recordedAt: '2026-08-30T12:00:00.000Z',
    }] as never} />);
    fireEvent.change(screen.getByLabelText('transfers.recipient'), { target: { value: 'recipient' } });
    fireEvent.change(screen.getByLabelText('common.amount'), { target: { value: '12' } });
    const selects = screen.getAllByRole('combobox');
    fireEvent.change(selects[0], { target: { value: 'SoftCoin' } });
    fireEvent.change(selects[1], { target: { value: 'Gift' } });
    fireEvent.submit(screen.getByRole('button', { name: 'transfers.send' }).closest('form')!);
    await waitFor(() => expect(mocks.createTransfer).toHaveBeenCalledWith('recipient', 12, 'SoftCoin', 'Gift', expect.any(String)));
    expect(screen.getByText('entry')).toBeInTheDocument();
    expect(mocks.refresh).toHaveBeenCalled();
  });

  it('keeps failed transfers visible without refreshing', async () => {
    mocks.createTransfer.mockResolvedValueOnce(failure);
    render(<EconomyTransfersWorkspace transactions={[]} />);
    fireEvent.change(screen.getByLabelText('transfers.recipient'), { target: { value: 'recipient' } });
    fireEvent.change(screen.getByLabelText('common.amount'), { target: { value: '1' } });
    fireEvent.submit(screen.getByRole('button', { name: 'transfers.send' }).closest('form')!);
    await screen.findByText('blocked');
    expect(mocks.refresh).not.toHaveBeenCalled();
  });
});
