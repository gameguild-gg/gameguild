import '@testing-library/jest-dom/vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { applyCohortSchedule, previewCohortSchedule } from '@/lib/learning/actions/cohorts';
import { cohortFixture, previewFixture, scheduleFixture } from './schedule-test-fixtures';
import { ScheduleBuilderSheet } from './schedule-builder-sheet';

vi.mock('@/lib/learning/actions/cohorts', () => ({
  applyCohortSchedule: vi.fn(),
  previewCohortSchedule: vi.fn(),
}));

describe('ScheduleBuilderSheet', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(previewCohortSchedule).mockResolvedValue({ success: true, data: previewFixture() });
    vi.mocked(applyCohortSchedule).mockResolvedValue({ success: true, data: scheduleFixture });
  });

  async function openBuilder() {
    const user = userEvent.setup();
    render(
      <ScheduleBuilderSheet
        courseId="course-1"
        cohort={cohortFixture}
        schedule={scheduleFixture}
        onApplied={vi.fn()}
      />,
    );
    await user.click(screen.getByRole('button', { name: 'Edit schedule' }));
    return user;
  }

  it('does not apply before confirmation', async () => {
    const user = await openBuilder();
    await user.click(screen.getByRole('button', { name: 'Generate preview' }));

    await waitFor(() => expect(previewCohortSchedule).toHaveBeenCalledOnce());
    expect(applyCohortSchedule).not.toHaveBeenCalled();

    await user.click(screen.getByRole('button', { name: 'Apply schedule' }));
    await waitFor(() => expect(applyCohortSchedule).toHaveBeenCalledOnce());
  });

  it('BlockingConflict_DisablesApply', async () => {
    vi.mocked(previewCohortSchedule).mockResolvedValue({
      success: true,
      data: previewFixture({
        hasBlockingConflicts: true,
        conflicts: [{ code: 'OUTSIDE_COHORT', severity: 'Blocking', message: 'Content exceeds the class end date.' }],
      }),
    });
    const user = await openBuilder();
    await user.click(screen.getByRole('button', { name: 'Generate preview' }));

    expect(await screen.findByText('Content exceeds the class end date.')).toBeVisible();
    expect(screen.getByRole('button', { name: 'Apply schedule' })).toBeDisabled();
  });

  it('AdvisoryConflict_RequiresConfirmation', async () => {
    vi.mocked(previewCohortSchedule).mockResolvedValue({
      success: true,
      data: previewFixture({
        conflicts: [{ code: 'HOLIDAY', severity: 'Advisory', message: 'A meeting overlaps a holiday.' }],
      }),
    });
    const user = await openBuilder();
    await user.click(screen.getByRole('button', { name: 'Generate preview' }));

    expect(await screen.findByText('A meeting overlaps a holiday.')).toBeVisible();
    expect(screen.getByRole('button', { name: 'Apply schedule' })).toBeDisabled();
    await user.click(screen.getByRole('checkbox', { name: 'I reviewed the advisory conflicts' }));
    expect(screen.getByRole('button', { name: 'Apply schedule' })).toBeEnabled();
  });

  it('FailedPreview_PreservesRules', async () => {
    vi.mocked(previewCohortSchedule).mockResolvedValue({ success: false, error: 'Preview service unavailable.' });
    const user = await openBuilder();
    const units = screen.getByLabelText('Units per period');
    await user.clear(units);
    await user.type(units, '3');
    await user.click(screen.getByRole('button', { name: 'Generate preview' }));

    expect(await screen.findByText('Preview service unavailable.')).toBeVisible();
    expect(screen.getByLabelText('Units per period')).toHaveValue(3);
    expect(screen.getByRole('button', { name: 'Generate preview' })).toBeVisible();
  });
});
