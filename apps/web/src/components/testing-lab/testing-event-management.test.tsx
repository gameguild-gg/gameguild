import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  beginReview: vi.fn(),
  refresh: vi.fn(),
}));

vi.mock('next/navigation', () => ({
  usePathname: () => '/workspace/learning',
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
  archiveTestingEvent: vi.fn(),
  deleteTestingEvent: vi.fn(),
  deleteTestingEventSlot: vi.fn(),
  rejectTestingEventApplication: vi.fn(),
  removeTestingEventCommitteeMember: vi.fn(),
  restoreTestingEvent: vi.fn(),
  transitionTestingEvent: vi.fn(),
  updateTestingEventAttendance: vi.fn(),
  updateTestingEvent: vi.fn(),
  updateTestingEventSlot: vi.fn(),
  voteOnTestingEventApplication: vi.fn(),
  waitlistTestingEventApplication: vi.fn(),
}));

import {
  CreateTestingEventDialog,
  EditTestingEventDialog,
  ManageTestingEventSlotDialog,
  TestingEventApplications,
} from './testing-event-management';

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

  it('uses a guarded drawer and keeps the shadcn date-time schedule chronological', () => {
    render(<CreateTestingEventDialog />);

    fireEvent.click(screen.getByRole('button', { name: 'New event' }));
    const field = (name: string) => document.querySelector<HTMLInputElement>(`input[name="${name}"]`)!;
    const applicationsOpenAt = field('applicationsOpenAt');
    const applicationsCloseAt = field('applicationsCloseAt');
    const startsAt = field('startsAt');
    const endsAt = field('endsAt');

    expect(document.querySelector('input[type="datetime-local"]')).not.toBeInTheDocument();
    expect(applicationsOpenAt.value).not.toBe('');
    expect(applicationsCloseAt.value).not.toBe('');
    expect(startsAt.value).not.toBe('');
    expect(endsAt.value).not.toBe('');
    expect(new Date(applicationsCloseAt.value).valueOf()).toBeGreaterThan(new Date(applicationsOpenAt.value).valueOf());
    expect(new Date(startsAt.value).valueOf()).toBeGreaterThanOrEqual(new Date(applicationsCloseAt.value).valueOf());
    expect(new Date(endsAt.value).valueOf()).toBeGreaterThan(new Date(startsAt.value).valueOf());

    fireEvent.click(screen.getByRole('button', { name: 'Event starts' }));
    const nextMinute = String(new Date(startsAt.value).getMinutes() + 1).padStart(2, '0');
    fireEvent.change(screen.getByLabelText('Minute'), { target: { value: nextMinute } });
    fireEvent.click(screen.getByRole('button', { name: 'Apply date and time' }));

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
    expect(document.querySelector<HTMLInputElement>('input[name="startsAt"]')?.value).toMatch(/^2030-08-19T/);
  });

  it('preserves API wall-clock values when editing an event', () => {
    const timezoneOffset = vi.spyOn(Date.prototype, 'getTimezoneOffset').mockReturnValue(180);
    render(
      <EditTestingEventDialog
        event={{
          id: 'event-1',
          name: 'Timezone-safe playtest',
          applicationsOpenAt: '2026-08-11T17:00:00Z',
          applicationsCloseAt: '2026-08-13T16:00:00Z',
          startsAt: '2026-08-13T17:00:00Z',
          endsAt: '2026-08-13T19:00:00Z',
          mode: 'Online',
          approvalMode: 'ManagerOnly',
          status: 'Draft',
          requiresFeedback: true,
        }}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Edit' }));

    expect(document.querySelector<HTMLInputElement>('input[name="applicationsOpenAt"]')?.value).toBe('2026-08-11T17:00');
    expect(document.querySelector<HTMLInputElement>('input[name="applicationsCloseAt"]')?.value).toBe('2026-08-13T16:00');
    expect(document.querySelector<HTMLInputElement>('input[name="startsAt"]')?.value).toBe('2026-08-13T17:00');
    expect(document.querySelector<HTMLInputElement>('input[name="endsAt"]')?.value).toBe('2026-08-13T19:00');
    timezoneOffset.mockRestore();
  });

  it('preserves API wall-clock values when editing a slot', () => {
    const timezoneOffset = vi.spyOn(Date.prototype, 'getTimezoneOffset').mockReturnValue(180);
    render(
      <ManageTestingEventSlotDialog
        eventId="event-1"
        slot={{
          id: 'slot-1',
          eventId: 'event-1',
          mode: 'InPerson',
          startsAt: '2026-08-13T17:30:00Z',
          endsAt: '2026-08-13T18:30:00Z',
          campusName: 'QA Campus',
          roomName: 'QA Room 101',
        }}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Edit slot' }));

    expect(document.querySelector<HTMLInputElement>('input[name="startsAt"]')?.value).toBe('2026-08-13T17:30');
    expect(document.querySelector<HTMLInputElement>('input[name="endsAt"]')?.value).toBe('2026-08-13T18:30');
    timezoneOffset.mockRestore();
  });
});
