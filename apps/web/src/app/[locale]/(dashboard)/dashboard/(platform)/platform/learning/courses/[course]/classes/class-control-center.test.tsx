import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

import type { CourseCohortSummary } from '@/lib/learning/queries/cohorts';
import { ClassControlCenter } from './class-control-center';

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: { href: string; children: ReactNode }) => <a href={href} {...props}>{children}</a>,
  useRouter: () => ({ refresh: vi.fn(), push: vi.fn() }),
}));

vi.mock('@/lib/learning/actions/cohorts', () => ({
  createCohort: vi.fn(),
}));

function cohort(overrides: Partial<CourseCohortSummary>): CourseCohortSummary {
  return {
    id: 'cohort-1',
    courseId: 'course-1',
    name: '2026.2 - Morning',
    description: '',
    instructor: null,
    period: { startsAt: '2026-08-12T00:00:00Z', endsAt: '2026-12-18T00:00:00Z' },
    meetingPattern: 'Mon/Wed - 09:00',
    enrollment: { current: 8, capacity: 24 },
    nextMeetingAt: '2026-08-12T12:00:00Z',
    conflictCount: 0,
    status: 'scheduled',
    isOpen: true,
    schedule: null,
    createdAt: '2026-07-14T00:00:00Z',
    ...overrides,
  };
}

describe('ClassControlCenter', () => {
  it('shows independent morning and evening classes', () => {
    render(
      <ClassControlCenter
        courseId="course-1"
        cohorts={[
          cohort({ id: 'morning', name: '2026.2 - Morning', meetingPattern: 'Mon/Wed - 09:00' }),
          cohort({ id: 'evening', name: '2026.2 - Evening', meetingPattern: 'Tue/Thu - 19:00' }),
        ]}
      />,
    );

    expect(screen.getAllByText('2026.2 - Morning').length).toBeGreaterThan(0);
    expect(screen.getAllByText('2026.2 - Evening').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Mon/Wed - 09:00').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Tue/Thu - 19:00').length).toBeGreaterThan(0);
  });

  it('opens class creation in a sheet', async () => {
    const user = userEvent.setup();
    render(<ClassControlCenter courseId="course-1" cohorts={[]} />);

    expect(screen.queryByLabelText('Class name')).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'New class' }));

    expect(screen.getByRole('dialog', { name: 'Create class' })).toBeVisible();
    expect(screen.getByLabelText('Class name')).toBeVisible();
  });
});
