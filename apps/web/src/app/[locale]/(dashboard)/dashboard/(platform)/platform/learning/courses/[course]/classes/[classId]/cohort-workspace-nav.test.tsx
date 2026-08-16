import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

import type { CourseCohortSummary } from '@/lib/learning/queries/cohorts';
import { CohortWorkspaceNav } from './cohort-workspace-nav';

const push = vi.fn();

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: { href: string; children: ReactNode }) => <a href={href} {...props}>{children}</a>,
  usePathname: () => '/dashboard/platform/learning/courses/advanced-game-ai-by-gameguild/classes/evening/schedule',
  useRouter: () => ({ push }),
}));

function cohort(id: string, name: string): CourseCohortSummary {
  return {
    id,
    courseId: 'course-1',
    name,
    description: '',
    instructor: null,
    period: { startsAt: '2026-08-12T00:00:00Z', endsAt: '2026-12-18T00:00:00Z' },
    meetingPattern: id === 'morning' ? 'Mon/Wed - 09:00' : 'Tue/Thu - 19:00',
    enrollment: { current: 0, capacity: 24 },
    nextMeetingAt: null,
    conflictCount: 0,
    status: 'scheduled',
    isOpen: false,
    schedule: null,
    createdAt: '2026-07-14T00:00:00Z',
  };
}

describe('CohortWorkspaceNav', () => {
  it('switches classes without losing the course route', async () => {
    const user = userEvent.setup();
    const morning = cohort('morning', '2026.2 - Morning');
    const evening = cohort('evening', '2026.2 - Evening');

    render(
      <CohortWorkspaceNav
        courseRoute="advanced-game-ai-by-gameguild"
        courseTitle="Advanced Game AI"
        cohort={evening}
        cohorts={[morning, evening]}
      >
        <p>Workspace content</p>
      </CohortWorkspaceNav>,
    );

    await user.click(screen.getByRole('button', { name: 'Switch class' }));
    await user.click(screen.getByRole('menuitem', { name: /2026.2 - Morning/ }));

    expect(push).toHaveBeenCalledWith('/dashboard/platform/learning/courses/advanced-game-ai-by-gameguild/classes/morning/schedule');
  });

  it('keeps the six class workspace sections visible', () => {
    const evening = cohort('evening', '2026.2 - Evening');
    render(
      <CohortWorkspaceNav courseRoute="course-1" courseTitle="Advanced Game AI" cohort={evening} cohorts={[evening]}>
        <p>Workspace content</p>
      </CohortWorkspaceNav>,
    );

    for (const label of ['Overview', 'Schedule & content', 'Students', 'Assessments', 'Gradebook', 'Settings']) {
      expect(screen.getAllByRole('link', { name: label }).length).toBeGreaterThan(0);
    }
  });
});
