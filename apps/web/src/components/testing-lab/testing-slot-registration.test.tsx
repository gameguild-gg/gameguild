import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  register: vi.fn(),
  cancel: vi.fn(),
}));

vi.mock('@/lib/testing-lab/events-actions', () => ({
  registerForTestingEventSlot: mocks.register,
  cancelTestingEventRegistration: mocks.cancel,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...rest }: { children: ReactNode; href: string }) => (
    <a href={href} {...rest}>{children}</a>
  ),
}));

import { TestingSlotRegistration } from './testing-slot-registration';

const slot = {
  id: 'slot-1',
  eventId: 'event-1',
  mode: 'InPerson' as const,
  startsAt: '2026-08-12T13:00:00.000Z',
  endsAt: '2026-08-12T15:00:00.000Z',
  maxTesters: 10,
  maxProjects: 3,
  campusName: 'Downtown campus',
  roomName: 'Lab 4',
  registeredTesterCount: 10,
  approvedProjectCount: 2,
  availableTesterCount: 0,
  availableProjectCount: 1,
};

describe('TestingSlotRegistration', () => {
  beforeEach(() => vi.clearAllMocks());

  it('explains that a full slot creates a waitlist registration', () => {
    render(
      <TestingSlotRegistration
        eventId="event-1"
        isAuthenticated
        slot={slot}
      />,
    );

    expect(screen.getByRole('button', { name: /join the waitlist/i })).toBeInTheDocument();
    expect(screen.getByText('Waitlist available')).toBeInTheDocument();
    expect(screen.queryByText(/seats claimed|game slots filled/i)).not.toBeInTheDocument();
    expect(screen.getAllByText('Aug 12, 2026')).toHaveLength(2);
    expect(screen.getByText('1:00 PM')).toBeInTheDocument();
    expect(screen.getByText('3:00 PM')).toBeInTheDocument();
    expect(screen.getAllByText('UTC')).toHaveLength(1);
  });

  it('shows the tester current registration instead of a duplicate form', () => {
    render(
      <TestingSlotRegistration
        eventId="event-1"
        isAuthenticated
        slot={slot}
        registration={{
          id: 'registration-1',
          status: 'Waitlisted',
          waitlistPosition: 2,
        }}
      />,
    );

    expect(screen.getByText(/waitlist position 2/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /cancel registration/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /join the waitlist/i })).not.toBeInTheDocument();
  });

  it('allows a tester to register again after cancelling', () => {
    render(
      <TestingSlotRegistration
        eventId="event-1"
        isAuthenticated
        slot={{ ...slot, registeredTesterCount: 9, availableTesterCount: 1 }}
        registration={{
          id: 'registration-1',
          status: 'Cancelled',
        }}
      />,
    );

    expect(screen.getByRole('button', { name: /join this playtest/i })).toBeInTheDocument();
    expect(screen.queryByText(/^cancelled$/i)).not.toBeInTheDocument();
  });

  it('directs anonymous testers to authentication', () => {
    render(
      <TestingSlotRegistration
        eventId="event-1"
        isAuthenticated={false}
        slot={{ mode: null, startsAt: null }}
      />,
    );

    expect(screen.getByRole('link', { name: /sign in or create a free account/i })).toHaveAttribute(
      'href',
      '/sign-in?redirectTo=%2Ftesting-lab%2Fevents%2Fevent-1%3FslotId%3D%23session-',
    );
    expect(screen.getByText('Online')).toBeInTheDocument();
    expect(screen.getByText('Schedule pending')).toBeInTheDocument();
    expect(screen.queryByText(/Downtown campus/)).not.toBeInTheDocument();
  });

  it('uses a schedule fallback without adding redundant capacity details', () => {
    render(
      <TestingSlotRegistration
        eventId="event-1"
        isAuthenticated
        slot={{ id: 'slot-1', mode: 'Hybrid', startsAt: 'invalid', availableTesterCount: 1 }}
      />,
    );
    expect(screen.getByText('Hybrid')).toBeInTheDocument();
    expect(screen.getByText('Schedule pending')).toBeInTheDocument();
    expect(screen.queryByText(/testers|game limit/i)).not.toBeInTheDocument();
  });

  it.each(['Completed', 'NoShow'])('does not offer cancellation after %s', (status) => {
    render(
      <TestingSlotRegistration
        eventId="event-1"
        isAuthenticated
        slot={slot}
        registration={{ id: 'registration-1', status }}
      />,
    );
    expect(screen.getByText(status)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Cancel registration' })).not.toBeInTheDocument();
  });

  it('uses a safe registration label and hides an absent waitlist position', () => {
    render(
      <TestingSlotRegistration
        eventId="event-1"
        isAuthenticated
        slot={slot}
        registration={{ id: 'registration-1', status: null, waitlistPosition: null }}
      />,
    );
    expect(screen.getByText('Registered')).toBeInTheDocument();
    expect(screen.queryByText(/Waitlist position/)).not.toBeInTheDocument();
  });

  it('does not offer cancellation without a registration identifier', () => {
    render(
      <TestingSlotRegistration
        eventId="event-1"
        isAuthenticated
        slot={slot}
        registration={{ status: 'Registered' }}
      />,
    );
    expect(screen.queryByRole('button', { name: 'Cancel registration' })).not.toBeInTheDocument();
  });

  it('submits an accepted seat reservation and renders success', async () => {
    const user = userEvent.setup();
    mocks.register.mockResolvedValueOnce({ success: true, message: 'Tester seat reserved.' });
    render(
      <TestingSlotRegistration
        eventId="event-1"
        isAuthenticated
        slot={{ ...slot, registeredTesterCount: 8, availableTesterCount: 2 }}
        generalRules="Respect everyone."
        testerInstructions="Bring headphones."
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Join this playtest' }));
    expect(screen.getByText('Bring headphones.')).toBeInTheDocument();
    expect(screen.getByText('Respect everyone.')).toBeInTheDocument();
    await user.click(screen.getByRole('checkbox', { name: /I’ve read and agree/ }));
    await user.click(screen.getByRole('button', { name: 'Join playtest' }));

    await waitFor(() => expect(mocks.register).toHaveBeenCalledOnce());
    const submitted = mocks.register.mock.calls[0]![0] as FormData;
    expect(Object.fromEntries(submitted.entries())).toEqual({
      eventId: 'event-1',
      slotId: 'slot-1',
      registrationResponseJson: '{"answers":[]}',
      acceptedRules: 'true',
    });
    expect(await screen.findByText('Tester seat reserved.')).toBeInTheDocument();
  });

  it('requires questionnaire completion again after an answer changes', async () => {
    const user = userEvent.setup();
    render(
      <TestingSlotRegistration
        eventId="event-1"
        isAuthenticated
        slot={{ ...slot, availableTesterCount: 2 }}
        registrationSchema={{
          title: 'Registration',
          questions: [{ id: 'needs', prompt: 'Accessibility needs', type: 'FreeText', required: true, options: [] }],
        }}
      />,
    );
    await user.click(screen.getByRole('button', { name: 'Join this playtest' }));
    const reserve = screen.getByRole('button', { name: 'Join playtest' });
    await user.click(screen.getByRole('checkbox', { name: /I’ve read and agree/ }));
    expect(reserve).toBeDisabled();
    await user.type(screen.getByLabelText('Accessibility needs'), 'Captions');
    await user.click(screen.getByRole('button', { name: 'Confirm registration answers' }));
    expect(reserve).toBeEnabled();
    await user.type(screen.getByLabelText('Accessibility needs'), ' please');
    expect(reserve).toBeDisabled();
  });

  it('cancels the current registration and renders a server failure', async () => {
    const user = userEvent.setup();
    mocks.cancel.mockResolvedValueOnce({ success: false, error: 'Cancellation denied.' });
    render(
      <TestingSlotRegistration
        eventId="event-1"
        isAuthenticated
        slot={slot}
        registration={{ id: 'registration-1', status: 'Registered' }}
      />,
    );
    await user.click(screen.getByRole('button', { name: 'Cancel registration' }));
    await waitFor(() => expect(mocks.cancel).toHaveBeenCalledOnce());
    expect(Object.fromEntries((mocks.cancel.mock.calls[0]![0] as FormData).entries())).toEqual({
      eventId: 'event-1',
      registrationId: 'registration-1',
    });
    expect(await screen.findByText('Cancellation denied.')).toBeInTheDocument();
  });

  it.each([
    ['register', new Error('Registration offline'), 'Registration offline'],
    ['register', 'failure', 'The Testing Lab operation failed.'],
    ['cancel', new Error('Cancellation offline'), 'Cancellation offline'],
    ['cancel', 'failure', 'The Testing Lab operation failed.'],
  ] as const)('renders thrown %s failures', async (operation, failure, message) => {
    const user = userEvent.setup();
    if (operation === 'register') mocks.register.mockRejectedValueOnce(failure);
    else mocks.cancel.mockRejectedValueOnce(failure);
    render(
      <TestingSlotRegistration
        eventId="event-1"
        isAuthenticated
        slot={{ ...slot, availableTesterCount: 2 }}
        registration={operation === 'cancel' ? { id: 'registration-1', status: 'Registered' } : undefined}
      />,
    );
    if (operation === 'register') {
      await user.click(screen.getByRole('button', { name: 'Join this playtest' }));
      await user.click(screen.getByRole('checkbox', { name: /I’ve read and agree/ }));
      await user.click(screen.getByRole('button', { name: 'Join playtest' }));
    } else {
      await user.click(screen.getByRole('button', { name: 'Cancel registration' }));
    }
    expect(await screen.findByText(message)).toBeInTheDocument();
  });
});
