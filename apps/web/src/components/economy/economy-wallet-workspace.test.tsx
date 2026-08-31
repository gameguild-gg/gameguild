import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  cancel: vi.fn(),
  convert: vi.fn(),
  refresh: vi.fn(),
  submit: vi.fn(),
}));

vi.mock('@/lib/economy/actions', () => ({
  cancelPayoutRequestAction: mocks.cancel,
  convertHardToSoftAction: mocks.convert,
  submitPayoutRequestAction: mocks.submit,
}));
vi.mock('next/navigation', () => ({ useRouter: () => ({ refresh: mocks.refresh }) }));

import { EconomyWalletWorkspace } from './economy-wallet-workspace';

const baseData = {
  capabilities: [
    { capability: 'ConvertHardToSoft', state: 'Ready', diagnostics: ['signed policy', ''] },
    { capability: 'PayoutExecution', state: 'Disabled', diagnostics: ['provider blocked'] },
  ],
  issue: 'safe reads are degraded',
  wallet: { withdrawableHard: 10, availableHardToSpend: 8, soft: 3, pendingHard: 2, heldHard: 1 },
  payoutRequests: [
    { id: 'submitted', hardCoinUnits: 10, state: 'Submitted', updatedAt: '2026-08-30T12:34:00.000Z' },
    { id: 'rejected', hardCoinUnits: 2, state: 'Rejected', updatedAt: null },
    { id: 'approved', hardCoinUnits: 3, state: 'Approved', updatedAt: null },
    { id: 'cancelled', hardCoinUnits: 4, state: 'Cancelled', updatedAt: null },
    { id: '', hardCoinUnits: null, state: undefined, updatedAt: undefined },
  ],
  payoutOperations: [
    { id: 'failed', hardCoinUnits: 1, state: 'Failed', updatedAt: null },
    { id: 'succeeded', hardCoinUnits: 2, state: 'Succeeded', updatedAt: null },
    { id: 'cancelled-op', hardCoinUnits: 3, state: 'Cancelled', updatedAt: null },
    { id: '', hardCoinUnits: null, state: undefined, updatedAt: undefined },
  ],
  transactions: [
    { journalEntryId: 'journal', journalSequence: 1, recordedAt: '2026-08-30T12:34:00.000Z', templateKind: 'Transfer', provenance: 'root', side: 'Debit', amountUnits: 5, currency: 'HardCoin' },
    { journalEntryId: 'fallback', journalSequence: 2, recordedAt: null, templateKind: null, provenance: null, side: null, amountUnits: null, currency: null },
  ],
};

describe('EconomyWalletWorkspace', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.submit.mockResolvedValue({ success: true, message: 'request recorded' });
    mocks.convert.mockResolvedValue({ success: false, message: 'conversion blocked' });
    mocks.cancel.mockResolvedValue({ success: true, message: 'cancelled' });
  });

  it('renders every durable state and runs payout, conversion, and cancellation commands', async () => {
    render(<EconomyWalletWorkspace data={baseData as never} />);

    expect(screen.getByText('safe reads are degraded')).toBeInTheDocument();
    expect(screen.getByText('provider blocked')).toBeInTheDocument();
    expect(screen.getByText('signed policy')).toBeInTheDocument();
    expect(screen.getAllByText('Unknown').length).toBeGreaterThanOrEqual(2);

    fireEvent.change(screen.getByLabelText('HardCoin units', { selector: '#economy-payout-amount' }), { target: { value: '25' } });
    fireEvent.submit(screen.getByRole('button', { name: 'Record payout request' }).closest('form')!);
    await waitFor(() => expect(mocks.submit).toHaveBeenCalledWith(25, expect.any(String)));
    await screen.findByText('request recorded');
    await waitFor(() => expect(screen.getByRole('button', { name: 'Convert HardCoin' })).toBeEnabled());

    fireEvent.change(screen.getByLabelText('HardCoin units', { selector: '#economy-conversion-amount' }), { target: { value: '7' } });
    fireEvent.submit(screen.getByRole('button', { name: 'Convert HardCoin' }).closest('form')!);
    await waitFor(() => expect(mocks.convert).toHaveBeenCalledWith(7, expect.any(String)));
    await screen.findByText('conversion blocked');
    await waitFor(() => expect(screen.getByRole('button', { name: 'Cancel' })).toBeEnabled());

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    await waitFor(() => expect(mocks.cancel).toHaveBeenCalledWith('submitted'));
    expect(mocks.refresh).toHaveBeenCalledTimes(2);
  });

  it('renders fail-closed empty states and capability fallbacks', () => {
    render(<EconomyWalletWorkspace data={{
      capabilities: [], issue: null, wallet: null, payoutRequests: [], payoutOperations: [], transactions: [],
    }} />);

    expect(screen.getByRole('button', { name: 'Conversion disabled' })).toBeDisabled();
    expect(screen.getByText('Execution is assessed independently after review.')).toBeInTheDocument();
    expect(screen.getByText('Capability readiness has not been reported.')).toBeInTheDocument();
    expect(screen.getByText('No payout requests have been recorded.')).toBeInTheDocument();
    expect(screen.getByText('No payout operations have been created.')).toBeInTheDocument();
    expect(screen.getByText('No wallet journal entries are available.')).toBeInTheDocument();
  });
});
