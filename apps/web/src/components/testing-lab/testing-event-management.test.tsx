import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  beginReview: vi.fn(),
  refresh: vi.fn(),
}));

vi.mock('next/navigation', () => ({
  useRouter: () => ({ refresh: mocks.refresh }),
}));

vi.mock('@/lib/testing-lab/events-actions', () => ({
  addTestingEventCommitteeMember: vi.fn(),
  assignTestedProjectToRegistration: vi.fn(),
  approveTestingEventApplication: vi.fn(),
  beginTestingEventApplicationReview: mocks.beginReview,
  configureTestingEventLearning: vi.fn(),
  createTestingEvent: vi.fn(),
  createTestingEventSlot: vi.fn(),
  deleteTestingEvent: vi.fn(),
  deleteTestingEventSlot: vi.fn(),
  rejectTestingEventApplication: vi.fn(),
  removeTestingEventCommitteeMember: vi.fn(),
  transitionTestingEvent: vi.fn(),
  updateTestingEventAttendance: vi.fn(),
  updateTestingEvent: vi.fn(),
  updateTestingEventSlot: vi.fn(),
  voteOnTestingEventApplication: vi.fn(),
  waitlistTestingEventApplication: vi.fn(),
}));

import { TestingEventApplications } from './testing-event-management';

describe('TestingEventApplications', () => {
  it('shows human labels and refreshes the SSR view after review starts', async () => {
    mocks.beginReview.mockResolvedValue({
      success: true,
      data: { id: 'application-1' },
      message: 'Application review started.',
    });

    render(
      <TestingEventApplications
        eventId="event-1"
        applications={[
          {
            id: 'application-1',
            projectId: 'project-1',
            submittedByUserId: 'user-1',
            status: 'Pending',
          },
        ]}
        slots={[]}
        projectLabels={{ 'project-1': 'Orbit Tactics' }}
        memberLabels={{ 'user-1': 'Ana Reviewer / ana@example.test' }}
      />,
    );

    expect(screen.getByText('Orbit Tactics')).toBeInTheDocument();
    expect(screen.getByText('Submitted by Ana Reviewer / ana@example.test')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Review' }));

    await waitFor(() => {
      expect(mocks.beginReview).toHaveBeenCalledOnce();
      expect(mocks.refresh).toHaveBeenCalledOnce();
    });
    expect(screen.getByText('Application review started.')).toBeInTheDocument();
  });

  it('exposes the approval slot selector with an accessible name', async () => {
    render(
      <TestingEventApplications
        eventId="event-1"
        applications={[
          {
            id: 'application-1',
            projectId: 'project-1',
            submittedByUserId: 'user-1',
            status: 'UnderReview',
          },
        ]}
        slots={[
          {
            id: 'slot-1',
            startsAt: '2026-08-02T14:00:00.000Z',
            campusName: 'GameGuild Campus',
          },
        ]}
        projectLabels={{ 'project-1': 'Orbit Tactics' }}
        memberLabels={{ 'user-1': 'Ana Reviewer / ana@example.test' }}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Approve' }));

    expect(
      await screen.findByRole('combobox', { name: 'Testing slot' }),
    ).toBeInTheDocument();
  });
});
