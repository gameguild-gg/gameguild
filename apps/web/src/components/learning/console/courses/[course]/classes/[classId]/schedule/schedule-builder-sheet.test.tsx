import '@testing-library/jest-dom/vitest';
import { act, render, screen, waitFor } from '@testing-library/react';
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

  it('returns nothing in read-only mode', () => {
    const { container } = render(
      <ScheduleBuilderSheet
        courseId="course-1"
        cohort={cohortFixture}
        schedule={scheduleFixture}
        readOnly
        onApplied={vi.fn()}
      />,
    );

    expect(container).toBeEmptyDOMElement();
  });

  it('builds a new schedule from defaults and sends every edited rule', async () => {
    const user = userEvent.setup();
    const onApplied = vi.fn();
    const cohortWithoutDates = {
      ...cohortFixture,
      period: { startsAt: null, endsAt: null },
    } as typeof cohortFixture;
    vi.mocked(previewCohortSchedule).mockResolvedValue({
      success: true,
      data: previewFixture({ items: undefined, calculatedEndDate: '' }),
    });
    render(
      <ScheduleBuilderSheet
        courseId="course-1"
        cohort={cohortWithoutDates}
        schedule={null}
        onApplied={onApplied}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Build schedule' }));
    expect(screen.getByText('Build class schedule')).toBeInTheDocument();
    expect(screen.getByLabelText('First instructional date')).toHaveValue('');
    expect(screen.getByLabelText('Class end date')).toHaveValue('');
    expect(screen.getByLabelText('Timezone')).toHaveValue('UTC');
    expect(screen.getByLabelText('Meeting start time')).toHaveValue('09:00');
    expect(screen.getByLabelText('Meeting duration (minutes)')).toHaveValue(90);
    expect(screen.getByLabelText('Units per period')).toHaveValue(1);

    await user.click(screen.getByRole('checkbox', { name: 'Mon' }));
    await user.click(screen.getByRole('button', { name: 'Generate preview' }));
    expect(await screen.findByText('Select at least one meeting day.')).toBeVisible();
    expect(previewCohortSchedule).not.toHaveBeenCalled();

    await user.click(screen.getByRole('checkbox', { name: 'Tue' }));
    await user.type(screen.getByLabelText('First instructional date'), '2026-09-01');
    await user.type(screen.getByLabelText('Class end date'), '2026-12-01');
    await user.clear(screen.getByLabelText('Timezone'));
    await user.type(screen.getByLabelText('Timezone'), ' America/Sao_Paulo ');
    await user.clear(screen.getByLabelText('Meeting start time'));
    await user.type(screen.getByLabelText('Meeting start time'), '10:30');
    await user.clear(screen.getByLabelText('Meeting duration (minutes)'));
    await user.type(screen.getByLabelText('Meeting duration (minutes)'), '120');
    await user.click(screen.getByLabelText('Pacing mode'));
    await user.click(screen.getByRole('option', { name: 'Manual' }));
    await user.clear(screen.getByLabelText('Units per period'));
    await user.type(screen.getByLabelText('Units per period'), '3');
    await user.click(screen.getByLabelText('Release policy'));
    await user.click(screen.getByRole('option', { name: 'Immediately' }));
    await user.clear(screen.getByLabelText('Assessment due offset (days)'));
    await user.type(screen.getByLabelText('Assessment due offset (days)'), '2');
    await user.type(
      screen.getByLabelText('Skipped dates'),
      '2026-09-07,  2026-10-12',
    );

    await user.click(screen.getByRole('button', { name: 'Generate preview' }));
    await waitFor(() => expect(previewCohortSchedule).toHaveBeenCalledOnce());
    expect(previewCohortSchedule).toHaveBeenCalledWith(
      'course-1',
      'cohort-1',
      expect.objectContaining({
        firstInstructionalDate: '2026-09-01',
        cohortEndDate: '2026-12-01',
        timezoneId: 'America/Sao_Paulo',
        meetingDays: ['Tuesday'],
        meetingStartTime: '10:30:00',
        meetingDurationMinutes: 120,
        pacingMode: 'Manual',
        unitsPerPeriod: 3,
        releasePolicy: 'Immediately',
        skippedDates: ['2026-09-07', '2026-10-12'],
        assessmentDueOffsetDays: 2,
      }),
    );
    expect(screen.getByText('Ends 2026-12-01')).toBeInTheDocument();
    expect(screen.getByText('No schedule conflicts were found.')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Back to rules' }));
    expect(screen.getByLabelText('Units per period')).toHaveValue(3);
    await user.click(screen.getByRole('button', { name: 'Generate preview' }));
    await user.click(await screen.findByRole('button', { name: 'Apply schedule' }));

    await waitFor(() => expect(applyCohortSchedule).toHaveBeenCalledOnce());
    expect(applyCohortSchedule).toHaveBeenCalledWith(
      'course-1',
      'cohort-1',
      expect.objectContaining({ expectedVersion: 0, confirmAdvisories: false }),
    );
    expect(onApplied).toHaveBeenCalledWith(scheduleFixture);
  });

  it('shows apply failures without closing the preview', async () => {
    vi.mocked(applyCohortSchedule).mockResolvedValue({
      success: false,
      error: 'Schedule version is stale.',
    });
    const user = await openBuilder();
    await user.click(screen.getByRole('button', { name: 'Generate preview' }));
    await user.click(await screen.findByRole('button', { name: 'Apply schedule' }));

    expect(await screen.findByText('Schedule version is stale.')).toBeVisible();
    expect(screen.getByRole('button', { name: 'Apply schedule' })).toBeVisible();
  });

  it('renders pending preview and apply states without duplicate requests', async () => {
    let resolvePreview!: (value: ReturnType<typeof previewFixture>) => void;
    let resolveApply!: (value: typeof scheduleFixture) => void;
    vi.mocked(previewCohortSchedule).mockReturnValueOnce(
      new Promise((resolve) => {
        resolvePreview = (data) => resolve({ success: true, data });
      }),
    );
    vi.mocked(applyCohortSchedule).mockReturnValueOnce(
      new Promise((resolve) => {
        resolveApply = (data) => resolve({ success: true, data });
      }),
    );
    const user = await openBuilder();

    await user.click(screen.getByRole('button', { name: 'Generate preview' }));
    expect(screen.getByRole('button', { name: 'Generate preview' })).toBeDisabled();
    await act(async () => resolvePreview(previewFixture()));

    const applyButton = await screen.findByRole('button', { name: 'Apply schedule' });
    await user.click(applyButton);
    expect(applyButton).toBeDisabled();
    await act(async () => resolveApply(scheduleFixture));

    expect(previewCohortSchedule).toHaveBeenCalledOnce();
    expect(applyCohortSchedule).toHaveBeenCalledOnce();
  });

  it('renders sparse preview items, overflow, and mixed conflicts safely', async () => {
    const items = Array.from({ length: 9 }, (_, index) => {
      if (index === 0) {
        return {
          type: 'ContentRelease',
          sortOrder: index,
          title: ' ',
          instructionalWeek: undefined,
        };
      }
      if (index === 1) {
        return {
          assessmentId: 'assessment-sparse',
          type: 'AssessmentWindow',
          sortOrder: index,
          title: 'Assessment',
          instructionalWeek: 0,
          dueAt: '2026-08-18T23:59:00Z',
        };
      }
      if (index === 2) {
        return {
          programContentId: 'content-sparse',
          type: 'LiveSession',
          sortOrder: index,
          title: 'Session',
          instructionalWeek: 2,
          startsAt: '2026-08-19T11:00:00Z',
        };
      }
      return {
        programContentId: `content-${index}`,
        type: 'ContentRelease',
        sortOrder: index,
        title: `Release ${index}`,
        instructionalWeek: index,
        availableFrom: '2026-08-20T11:00:00Z',
      };
    });
    vi.mocked(previewCohortSchedule).mockResolvedValue({
      success: true,
      data: previewFixture({
        items: items as ReturnType<typeof previewFixture>['items'],
        calculatedEndDate: '',
        hasBlockingConflicts: false,
        conflicts: [
          { severity: 'Advisory', message: '' },
          { severity: 'Blocking', message: '' },
        ],
      }),
    });
    const user = await openBuilder();
    await user.click(screen.getByRole('button', { name: 'Generate preview' }));

    expect(await screen.findByText('Untitled schedule item')).toBeVisible();
    expect(screen.getByText('Date not set')).toBeVisible();
    expect(screen.getByText('And 1 more items.')).toBeVisible();
    expect(screen.getAllByText('Review this schedule conflict.')).toHaveLength(2);
    expect(screen.getByRole('button', { name: 'Apply schedule' })).toBeDisabled();
    expect(
      screen.queryByRole('checkbox', { name: 'I reviewed the advisory conflicts' }),
    ).not.toBeInTheDocument();
  });
});
