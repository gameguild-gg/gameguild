import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import type { LearningCohortsCohortCalendarEntry } from '@game-guild/client';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

import type { CourseCohortSummary } from '@/lib/learning/queries/cohorts';
import { GeneralCohortCalendar } from './general-cohort-calendar';

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: { href: string; children: ReactNode }) => <a href={href} {...props}>{children}</a>,
}));

function cohort(id: string, name: string): CourseCohortSummary {
  return {
    id,
    courseId: 'course-1',
    name,
    description: '',
    instructor: null,
    period: { startsAt: '2026-08-12T00:00:00Z', endsAt: '2026-12-18T00:00:00Z' },
    meetingPattern: null,
    enrollment: { current: 0, capacity: 24 },
    nextMeetingAt: null,
    conflictCount: 0,
    status: 'scheduled',
    isOpen: false,
    schedule: null,
    createdAt: '2026-07-14T00:00:00Z',
  };
}

describe('GeneralCohortCalendar', () => {
  it('renders concurrent classes as separate calendar lanes', () => {
    const entries: LearningCohortsCohortCalendarEntry[] = [
      { cohortId: 'morning', itemId: 'item-1', title: 'Module 1', type: 'ContentRelease', startsAt: '2026-08-12T12:00:00Z' },
      { cohortId: 'evening', itemId: 'item-2', title: 'Module 1', type: 'ContentRelease', startsAt: '2026-08-14T22:00:00Z' },
    ];

    render(
      <GeneralCohortCalendar
        courseId="course-1"
        cohorts={[cohort('morning', '2026.2 - Morning'), cohort('evening', '2026.2 - Evening')]}
        entries={entries}
      />,
    );

    expect(screen.getByLabelText('2026.2 - Morning calendar lane')).toBeVisible();
    expect(screen.getByLabelText('2026.2 - Evening calendar lane')).toBeVisible();
    expect(screen.getAllByText('Module 1')).toHaveLength(2);
  });
});
