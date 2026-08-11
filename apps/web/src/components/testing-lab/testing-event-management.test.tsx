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

import { CreateTestingEventDialog, TestingEventApplications } from './testing-event-management';

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

    expect(await screen.findByRole('combobox', { name: 'Testing slot' })).toBeInTheDocument();
  });

  it('uses a guarded drawer and opens with a valid event schedule', () => {
    render(<CreateTestingEventDialog />);

    fireEvent.click(screen.getByRole('button', { name: 'New event' }));
    const applicationsOpenAt = screen.getByLabelText('Applications open') as HTMLInputElement;
    const applicationsCloseAt = screen.getByLabelText('Applications close') as HTMLInputElement;
    const startsAt = screen.getByLabelText('Event starts') as HTMLInputElement;
    const endsAt = screen.getByLabelText('Event ends') as HTMLInputElement;

    expect(applicationsOpenAt.value).not.toBe('');
    expect(applicationsCloseAt.value).not.toBe('');
    expect(startsAt.value).not.toBe('');
    expect(endsAt.value).not.toBe('');
    expect(new Date(applicationsCloseAt.value).valueOf()).toBeGreaterThan(new Date(applicationsOpenAt.value).valueOf());
    expect(new Date(startsAt.value).valueOf()).toBeGreaterThanOrEqual(new Date(applicationsCloseAt.value).valueOf());
    expect(new Date(endsAt.value).valueOf()).toBeGreaterThan(new Date(startsAt.value).valueOf());

    const laterStart = new Date(new Date(startsAt.value).valueOf() + 3 * 60 * 60 * 1000);
    const laterStartValue = new Date(laterStart.valueOf() - laterStart.getTimezoneOffset() * 60_000)
      .toISOString()
      .slice(0, 16);
    fireEvent.change(startsAt, { target: { value: laterStartValue } });

    expect(new Date(endsAt.value).valueOf() - new Date(startsAt.value).valueOf()).toBe(2 * 60 * 60 * 1000);

    fireEvent.change(screen.getByLabelText('Event name'), {
      target: { value: 'Community playtest' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));

    expect(screen.getByRole('alertdialog')).toHaveTextContent('Discard testing event draft?');

    fireEvent.click(screen.getByRole('button', { name: 'Keep editing' }));

    expect(screen.getByText('Create testing event')).toBeInTheDocument();
  });
  it('opens as a controlled sheet with the calendar day prefilled', () => {
    render(
      <CreateTestingEventDialog open showTrigger={false} initialDate={new Date(2030, 7, 19)} onOpenChange={vi.fn()} />,
    );

    expect(screen.queryByRole('button', { name: 'New event' })).not.toBeInTheDocument();
    expect((screen.getByLabelText('Event starts') as HTMLInputElement).value).toMatch(/^2030-08-19T/);
  });
});
