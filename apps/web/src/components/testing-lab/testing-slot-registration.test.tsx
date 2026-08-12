import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/testing-lab/events-actions', () => ({
  registerForTestingEventSlot: vi.fn(),
  cancelTestingEventRegistration: vi.fn(),
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
  it('explains that a full slot creates a waitlist registration', () => {
    render(
      <TestingSlotRegistration
        eventId="event-1"
        isAuthenticated
        slot={slot}
      />,
    );

    expect(screen.getByRole('button', { name: /join waitlist/i })).toBeInTheDocument();
    expect(screen.getByText(/approved projects use 2 of 3 slots/i)).toBeInTheDocument();
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
    expect(screen.queryByRole('button', { name: /join waitlist/i })).not.toBeInTheDocument();
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

    expect(screen.getByRole('button', { name: /reserve tester seat/i })).toBeInTheDocument();
    expect(screen.queryByText(/^cancelled$/i)).not.toBeInTheDocument();
  });
});
