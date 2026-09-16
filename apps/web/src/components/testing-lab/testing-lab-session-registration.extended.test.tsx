import { render, screen } from '@testing-library/react';
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

describe('TestingLabSessionRegistration extended behavior', () => {
  beforeEach(() => vi.clearAllMocks());

  it('disables registration when the session is closed', () => {
    render(
      <TestingLabSessionRegistration
        sessionId="session-1"
        canRegister={false}
        availableSpots={2}
        isAuthenticated
      />,
    );
    expect(screen.getByRole('button', { name: 'Registration closed' })).toBeDisabled();
  });

  it('keeps the idle action after an unsuccessful registration response', async () => {
    const user = userEvent.setup();
    mocks.register.mockResolvedValueOnce({ success: false, error: 'Registration denied.' });
    render(
      <TestingLabSessionRegistration
        sessionId="session-1"
        canRegister
        availableSpots={1}
        isAuthenticated
      />,
    );
    await user.click(screen.getByRole('button', { name: 'Reserve a tester seat' }));
    expect(await screen.findByText('Registration denied.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Reserve a tester seat' })).toBeInTheDocument();
  });

  it.each([
    [new Error('Registration service offline'), 'Registration service offline'],
    ['failure', 'The Testing Lab operation failed.'],
  ])('turns thrown registration failures into visible errors', async (failure, message) => {
    const user = userEvent.setup();
    mocks.register.mockRejectedValueOnce(failure);
    render(
      <TestingLabSessionRegistration
        sessionId="session-1"
        canRegister
        availableSpots={1}
        isAuthenticated
      />,
    );
    await user.click(screen.getByRole('button', { name: 'Reserve a tester seat' }));
    expect(await screen.findByText(message)).toBeInTheDocument();
  });
});
