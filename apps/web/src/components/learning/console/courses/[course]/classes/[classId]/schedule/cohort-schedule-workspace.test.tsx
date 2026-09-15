import '@testing-library/jest-dom/vitest';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { shiftCohortScheduleItem, updateCohortScheduleItem } from '@/lib/learning/actions/cohorts';
import { CohortScheduleWorkspace } from './cohort-schedule-workspace';
import { cohortFixture, scheduleFixture } from './schedule-test-fixtures';

vi.mock('@/lib/learning/actions/cohorts', async () => {
  const actual = await vi.importActual<typeof import('@/lib/learning/actions/cohorts')>('@/lib/learning/actions/cohorts');
  return { ...actual, shiftCohortScheduleItem: vi.fn(), updateCohortScheduleItem: vi.fn() };
});

describe('CohortScheduleWorkspace', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(shiftCohortScheduleItem).mockResolvedValue({ success: true, data: scheduleFixture });
    vi.mocked(updateCohortScheduleItem).mockResolvedValue({ success: true, data: scheduleFixture });
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: vi.fn().mockImplementation((query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        addListener: vi.fn(),
        removeListener: vi.fn(),
        dispatchEvent: vi.fn(),
      })),
    });
  });

  it('CompletedCohort_IsReadOnly', () => {
    render(
      <CohortScheduleWorkspace
        courseId="course-1"
        cohort={{ ...cohortFixture, status: 'completed' }}
        initialSchedule={scheduleFixture}
      />,
    );

    expect(screen.getByText('Completed classes are read only.')).toBeVisible();
    expect(screen.queryByRole('button', { name: 'Edit schedule' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Shift Foundations/i })).not.toBeInTheDocument();
  });

  it.each([
    ['ShiftSingle_ChangesOneItem', 'Single'],
    ['ShiftFollowing_ChangesSelectedAndLaterItems', 'Following'],
  ] as const)('%s', async (_name, scope) => {
    const user = userEvent.setup();
    render(<CohortScheduleWorkspace courseId="course-1" cohort={cohortFixture} initialSchedule={scheduleFixture} />);

    await user.click(screen.getByRole('button', { name: 'Shift Foundations' }));
    await user.clear(screen.getByLabelText('Days to shift'));
    await user.type(screen.getByLabelText('Days to shift'), '7');
    await user.click(screen.getByLabelText(scope === 'Single' ? 'Only this item' : 'This and following items'));
    await user.click(screen.getByRole('button', { name: 'Shift schedule item' }));

    await waitFor(() => expect(shiftCohortScheduleItem).toHaveBeenCalledWith(
      'course-1',
      'cohort-1',
      'release-1',
      { expectedVersion: 3, days: 7, scope },
    ));
  });

  it('edits one schedule item in a dialog', async () => {
    const user = userEvent.setup();
    render(<CohortScheduleWorkspace courseId="course-1" cohort={cohortFixture} initialSchedule={scheduleFixture} />);

    await user.click(screen.getByRole('button', { name: 'Edit Foundations' }));
    fireEvent.change(screen.getByLabelText('Schedule item title'), { target: { value: 'Foundations and tools' } });
    await user.click(screen.getByRole('button', { name: 'Save schedule item' }));

    await waitFor(() => expect(updateCohortScheduleItem).toHaveBeenCalledWith(
      'course-1',
      'cohort-1',
      'release-1',
      expect.objectContaining({
        expectedVersion: 3,
        item: expect.objectContaining({ title: 'Foundations and tools' }),
      }),
    ));
  });

  it('Mobile_DefaultsToTimeline', async () => {
    vi.mocked(window.matchMedia).mockReturnValue({
      matches: true,
      media: '(max-width: 767px)',
      onchange: null,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn(),
    });

    await act(async () => {
      render(<CohortScheduleWorkspace courseId="course-1" cohort={cohortFixture} initialSchedule={scheduleFixture} />);
    });

    await waitFor(() => expect(screen.getByRole('tab', { name: 'Timeline' })).toHaveAttribute('data-active'));
  });

  it('shows the empty schedule state and builder action', () => {
    render(
      <CohortScheduleWorkspace
        courseId="course-1"
        cohort={cohortFixture}
        initialSchedule={null}
      />,
    );

    expect(screen.getByText('Not configured')).toBeVisible();
    expect(screen.getByRole('button', { name: 'Build schedule' })).toBeVisible();
    expect(screen.getByText('This class does not have a schedule yet')).toBeVisible();
  });

  it('renders schedule fallbacks, curriculum drift, and switches views', async () => {
    const user = userEvent.setup();
    const sparseSchedule = {
      ...scheduleFixture,
      version: undefined,
      timezoneId: '',
      meetingDays: [],
      items: undefined,
      unscheduledContentIds: ['content-a', 'content-b'],
    } as typeof scheduleFixture;
    render(
      <CohortScheduleWorkspace
        courseId="course-1"
        cohort={cohortFixture}
        initialSchedule={sparseSchedule}
      />,
    );

    expect(screen.getByText('Version 0')).toBeVisible();
    expect(screen.getByText('UTC')).toBeVisible();
    expect(screen.getByText('Manual')).toBeVisible();
    expect(screen.getByText('2 course items still need dates. Generate a new preview before the class starts.')).toBeVisible();

    await user.click(screen.getByRole('tab', { name: 'Calendar' }));
    expect(screen.getByRole('tab', { name: 'Calendar' })).toHaveAttribute('data-active');
    await user.click(screen.getByRole('tab', { name: 'Timeline' }));
    expect(screen.getByRole('tab', { name: 'Timeline' })).toHaveAttribute('data-active');
  });

  it('treats cancelled cohorts as read-only', () => {
    render(
      <CohortScheduleWorkspace
        courseId="course-1"
        cohort={{ ...cohortFixture, status: 'cancelled' }}
        initialSchedule={scheduleFixture}
      />,
    );

    expect(screen.getByText('Completed classes are read only.')).toBeVisible();
    expect(screen.queryByRole('button', { name: 'Edit schedule' })).not.toBeInTheDocument();
  });

  it('validates shifts, reports API failures, and clears dialogs on dismissal', async () => {
    vi.mocked(shiftCohortScheduleItem).mockResolvedValue({
      success: false,
      error: 'Shift conflicted with the class end.',
    });
    const user = userEvent.setup();
    render(
      <CohortScheduleWorkspace
        courseId="course-1"
        cohort={cohortFixture}
        initialSchedule={scheduleFixture}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Shift Foundations' }));
    const days = screen.getByLabelText('Days to shift');
    await user.clear(days);
    await user.type(days, '1.5');
    await user.click(screen.getByRole('button', { name: 'Shift schedule item' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Enter a non-zero whole number of days.');

    await user.clear(days);
    await user.type(days, '0');
    await user.click(screen.getByRole('button', { name: 'Shift schedule item' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Enter a non-zero whole number of days.');

    await user.clear(days);
    await user.type(days, '-2');
    await user.click(screen.getByLabelText('This and following items'));
    await user.click(screen.getByRole('button', { name: 'Shift schedule item' }));
    expect(await screen.findByText('Shift conflicted with the class end.')).toBeVisible();
    expect(shiftCohortScheduleItem).toHaveBeenCalledWith(
      'course-1',
      'cohort-1',
      'release-1',
      { expectedVersion: 3, days: -2, scope: 'Following' },
    );

    await user.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(screen.getByText('Schedule update failed')).toBeVisible();

    await user.click(screen.getByRole('button', { name: 'Shift Foundations' }));
    await user.keyboard('{Escape}');
    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: /Shift Foundations/i })).not.toBeInTheDocument();
    });
  });

  it('validates and submits every editable item field while preserving dates', async () => {
    vi.mocked(updateCohortScheduleItem).mockResolvedValue({
      success: false,
      error: 'Item update was rejected.',
    });
    const user = userEvent.setup();
    render(
      <CohortScheduleWorkspace
        courseId="course-1"
        cohort={cohortFixture}
        initialSchedule={scheduleFixture}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Edit Foundations' }));
    const title = screen.getByLabelText('Schedule item title');
    await user.clear(title);
    await user.click(screen.getByRole('button', { name: 'Save schedule item' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Schedule item title is required.');

    await user.type(title, '  Updated foundations  ');
    await user.type(screen.getByLabelText('Location'), '  Studio A  ');
    await user.type(screen.getByLabelText('Meeting URL'), '  https://meet.example.test  ');
    await user.click(screen.getByLabelText('Status'));
    await user.click(screen.getByRole('option', { name: 'Completed' }));
    await user.click(screen.getByLabelText('Student visibility'));
    await user.click(screen.getByRole('option', { name: 'Hide from students' }));
    await user.click(screen.getByRole('button', { name: 'Save schedule item' }));

    expect(await screen.findByText('Item update was rejected.')).toBeVisible();
    expect(updateCohortScheduleItem).toHaveBeenCalledWith(
      'course-1',
      'cohort-1',
      'release-1',
      expect.objectContaining({
        expectedVersion: 3,
        item: expect.objectContaining({
          title: 'Updated foundations',
          location: 'Studio A',
          meetingUrl: 'https://meet.example.test',
          status: 'Completed',
          visibilityOverride: 'Hidden',
          availableFrom: '2026-08-12T11:00:00Z',
        }),
      }),
    );

    await user.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(screen.getByText('Schedule update failed')).toBeVisible();

    await user.click(screen.getByRole('button', { name: 'Edit Foundations' }));
    await user.keyboard('{Escape}');
    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Edit schedule item' })).not.toBeInTheDocument();
    });
  });

  it('uses defaults when opening an incomplete schedule item', async () => {
    const user = userEvent.setup();
    const incompleteSchedule = {
      ...scheduleFixture,
      items: [
        {
          ...scheduleFixture.items?.[0],
          id: 'incomplete-item',
          title: '   ',
          status: undefined,
          visibilityOverride: undefined,
        },
      ],
    } as typeof scheduleFixture;
    render(
      <CohortScheduleWorkspace
        courseId="course-1"
        cohort={cohortFixture}
        initialSchedule={incompleteSchedule}
      />,
    );

    await user.click(
      screen.getByRole('button', { name: 'Edit Untitled schedule item' }),
    );
    expect(screen.getByLabelText('Schedule item title')).toHaveValue('');
    expect(screen.getByLabelText('Status')).toHaveTextContent('Scheduled');
    expect(screen.getByLabelText('Student visibility')).toHaveTextContent('Follow course content');
  });

  it('uses version zero for shifts and edits when a schedule version is absent', async () => {
    const user = userEvent.setup();
    const versionlessSchedule = {
      ...scheduleFixture,
      version: undefined,
      unscheduledContentIds: undefined,
    } as typeof scheduleFixture;
    vi.mocked(shiftCohortScheduleItem).mockResolvedValue({
      success: true,
      data: versionlessSchedule,
    });
    render(
      <CohortScheduleWorkspace
        courseId="course-1"
        cohort={cohortFixture}
        initialSchedule={versionlessSchedule}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Shift Foundations' }));
    await user.click(screen.getByRole('button', { name: 'Shift schedule item' }));
    await waitFor(() => expect(shiftCohortScheduleItem).toHaveBeenCalledOnce());
    expect(shiftCohortScheduleItem).toHaveBeenCalledWith(
      'course-1',
      'cohort-1',
      'release-1',
      expect.objectContaining({ expectedVersion: 0 }),
    );

    await user.click(screen.getByRole('button', { name: 'Edit Foundations' }));
    await user.click(screen.getByRole('button', { name: 'Save schedule item' }));

    await waitFor(() => expect(updateCohortScheduleItem).toHaveBeenCalledOnce());
    expect(updateCohortScheduleItem).toHaveBeenCalledWith(
      'course-1',
      'cohort-1',
      'release-1',
      expect.objectContaining({ expectedVersion: 0 }),
    );
  });

  it('shows pending states and prevents duplicate shift and edit submissions', async () => {
    let resolveShift!: (value: typeof scheduleFixture) => void;
    let resolveEdit!: (value: typeof scheduleFixture) => void;
    vi.mocked(shiftCohortScheduleItem).mockReturnValueOnce(
      new Promise((resolve) => {
        resolveShift = (data) => resolve({ success: true, data });
      }),
    );
    vi.mocked(updateCohortScheduleItem).mockReturnValueOnce(
      new Promise((resolve) => {
        resolveEdit = (data) => resolve({ success: true, data });
      }),
    );
    const user = userEvent.setup();
    render(
      <CohortScheduleWorkspace
        courseId="course-1"
        cohort={cohortFixture}
        initialSchedule={scheduleFixture}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Shift Foundations' }));
    const shiftButton = screen.getByRole('button', { name: 'Shift schedule item' });
    await user.click(shiftButton);
    expect(shiftButton).toBeDisabled();
    await act(async () => resolveShift(scheduleFixture));

    await user.click(screen.getByRole('button', { name: 'Edit Foundations' }));
    const editButton = screen.getByRole('button', { name: 'Save schedule item' });
    await user.click(editButton);
    expect(editButton).toBeDisabled();
    await act(async () => resolveEdit(scheduleFixture));

    expect(shiftCohortScheduleItem).toHaveBeenCalledOnce();
    expect(updateCohortScheduleItem).toHaveBeenCalledOnce();
  });
});
