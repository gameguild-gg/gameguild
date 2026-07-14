import '@testing-library/jest-dom/vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { createCohort } from '@/lib/learning/actions/cohorts';
import { NewClassSheet } from './new-class-sheet';

const refresh = vi.fn();
const push = vi.fn();

vi.mock('@/i18n/navigation', () => ({ useRouter: () => ({ refresh, push }) }));
vi.mock('@/lib/learning/actions/cohorts', () => ({ createCohort: vi.fn() }));

describe('NewClassSheet', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(createCohort).mockResolvedValue({ success: true, data: { id: 'cohort-1' } });
  });

  it('creates a cohort period and continues to its schedule', async () => {
    const user = userEvent.setup();
    render(<NewClassSheet courseId="course-1" />);

    await user.click(screen.getByRole('button', { name: 'New class' }));
    await user.type(screen.getByLabelText('Class name'), '2026.2 - Evening');
    await user.clear(screen.getByLabelText('Start date'));
    await user.type(screen.getByLabelText('Start date'), '2026-08-12');
    await user.clear(screen.getByLabelText('End date'));
    await user.type(screen.getByLabelText('End date'), '2026-12-18');
    await user.clear(screen.getByLabelText('Capacity'));
    await user.type(screen.getByLabelText('Capacity'), '24');
    await user.type(screen.getByLabelText('Meeting pattern'), 'Tue/Thu - 19:00');
    await user.click(screen.getByRole('button', { name: 'Create and build schedule' }));

    await waitFor(() => {
      expect(createCohort).toHaveBeenCalledWith(expect.objectContaining({
        courseId: 'course-1',
        name: '2026.2 - Evening',
        startDate: '2026-08-12T00:00:00',
        endDate: '2026-12-18T23:59:59',
        maxCapacity: 24,
        meetingSchedule: 'Tue/Thu - 19:00',
      }));
    });
    expect(push).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/classes/cohort-1/schedule');
  });
});
