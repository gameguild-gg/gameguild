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

    await waitFor(() => expect(screen.getByRole('tab', { name: 'Timeline' })).toHaveAttribute('data-state', 'active'));
  });
});
