import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ review: vi.fn(), refresh: vi.fn() }));
vi.mock('@/lib/economy/admin-actions', () => ({ reviewPayoutRequestAction: mocks.review }));
vi.mock('next/navigation', () => ({ useRouter: () => ({ refresh: mocks.refresh }) }));

import { EconomyPayoutReviewWorkspace } from './economy-payout-review-workspace';

describe('EconomyPayoutReviewWorkspace', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.review
      .mockResolvedValueOnce({ success: true, message: 'approved' })
      .mockResolvedValueOnce({ success: false, message: 'rejected by policy' });
  });

  it('renders tenant evidence and records approve and reject outcomes', async () => {
    render(<EconomyPayoutReviewWorkspace data={{
      issue: 'review feed degraded',
      reviewAudits: {
        approve: [{ id: 'audit', actorId: 'actor', outcome: 'Approved', occurredAt: '2026-08-30T12:34:00.000Z', reason: 'valid' }],
        rejected: [{ id: '', actorId: 'actor-2', outcome: undefined, occurredAt: undefined, reason: undefined }],
      },
      requests: [
        { id: 'approve', payeeId: 'payee', hardCoinUnits: 10, state: 'Submitted', createdAt: 'now' },
        { id: 'reject', payeeId: 'payee-2', hardCoinUnits: 20, state: 'Submitted', createdAt: 'now' },
        { id: 'rejected', payeeId: '', hardCoinUnits: undefined, state: 'Rejected', createdAt: 'now' },
        { id: 'approved', payeeId: 'p', hardCoinUnits: 1, state: 'Approved', createdAt: 'now' },
        { id: 'cancelled', payeeId: 'p', hardCoinUnits: 1, state: 'Cancelled', createdAt: 'now' },
        { id: '', payeeId: '', hardCoinUnits: undefined, state: undefined, createdAt: 'fallback' },
      ],
    } as never} />);

    expect(screen.getByText('review feed degraded')).toBeInTheDocument();
    expect(screen.getByText(/Approved · 2026-08-30 12:34 UTC · valid/)).toBeInTheDocument();
    expect(screen.getByText(/Review · — · No reason/)).toBeInTheDocument();
    expect(screen.getAllByText('No prior review').length).toBeGreaterThan(0);

    fireEvent.change(screen.getByLabelText('Reason for payout request approve'), { target: { value: 'good' } });
    fireEvent.click(screen.getAllByRole('button', { name: 'Approve' })[0]!);
    await waitFor(() => expect(mocks.review).toHaveBeenCalledWith('approve', 'approve', 'good'));
    await screen.findByText('Decision recorded');
    await waitFor(() => expect(screen.getAllByRole('button', { name: 'Reject' })[1]).toBeEnabled());

    fireEvent.click(screen.getAllByRole('button', { name: 'Reject' })[1]!);
    await waitFor(() => expect(mocks.review).toHaveBeenCalledWith('reject', 'reject', ''));
    await screen.findByText('rejected by policy');
    expect(mocks.refresh).toHaveBeenCalledOnce();
  });

  it('renders an empty review queue without an issue', () => {
    render(<EconomyPayoutReviewWorkspace data={{ issue: null, requests: [], reviewAudits: {} }} />);
    expect(screen.getByText('There are no payout requests available to your tenant.')).toBeInTheDocument();
  });
});
