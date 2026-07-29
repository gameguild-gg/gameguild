import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  register: vi.fn(),
  unregister: vi.fn(),
  joinWaitlist: vi.fn(),
  leaveWaitlist: vi.fn(),
}));
vi.mock('@/lib/testing-lab/actions', () => ({
  registerForTestingSession: mocks.register,
  unregisterFromTestingSession: mocks.unregister,
  joinTestingSessionWaitlist: mocks.joinWaitlist,
  leaveTestingSessionWaitlist: mocks.leaveWaitlist,
}));

import { TestingLabSessionRegistration } from './testing-lab-session-registration';

describe('TestingLabSessionRegistration', () => {
  beforeEach(() => vi.clearAllMocks());

  it('requires authentication before registration', () => {
    render(<TestingLabSessionRegistration sessionId="session-1" canRegister availableSpots={2} isAuthenticated={false} />);
    expect(screen.getByRole('link', { name: /sign in to join/i })).toHaveAttribute('href', '/sign-in?callbackUrl=%2Ftesting-lab');
  });

  it('registers and can cancel a reserved seat', async () => {
    mocks.register.mockResolvedValue({ success: true, data: { id: 'registration-1' }, message: 'Registered for testing session.' });
    mocks.unregister.mockResolvedValue({ success: true, data: null, message: 'Registration cancelled.' });
    render(<TestingLabSessionRegistration sessionId="session-1" canRegister availableSpots={2} isAuthenticated />);

    await userEvent.click(screen.getByRole('button', { name: /reserve a tester seat/i }));
    expect(await screen.findByText('Registered for testing session.')).toBeVisible();
    await userEvent.click(screen.getByRole('button', { name: /cancel registration/i }));
    await waitFor(() => expect(mocks.unregister).toHaveBeenCalledOnce());
    expect(await screen.findByText('Registration cancelled.')).toBeVisible();
  });

  it('joins and leaves a full session waitlist', async () => {
    mocks.joinWaitlist.mockResolvedValue({ success: true, data: { id: 'waitlist-1' }, message: 'Added to session waitlist.' });
    mocks.leaveWaitlist.mockResolvedValue({ success: true, data: null, message: 'Removed from session waitlist.' });
    render(<TestingLabSessionRegistration sessionId="session-1" canRegister availableSpots={0} isAuthenticated />);

    await userEvent.click(screen.getByRole('button', { name: /join waitlist/i }));
    expect(await screen.findByText('Added to session waitlist.')).toBeVisible();
    await userEvent.click(screen.getByRole('button', { name: /leave waitlist/i }));
    await waitFor(() => expect(mocks.leaveWaitlist).toHaveBeenCalledOnce());
  });
});
