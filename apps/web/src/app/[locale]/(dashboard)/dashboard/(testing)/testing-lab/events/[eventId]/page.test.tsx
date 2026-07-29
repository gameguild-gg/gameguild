import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getTestingEventManagerData: vi.fn(),
  getMembers: vi.fn(),
  getCourses: vi.fn(),
  getCourseContent: vi.fn(),
}));

vi.mock('@/lib/testing-lab/events-queries', () => ({
  getTestingEventManagerData: mocks.getTestingEventManagerData,
}));

vi.mock('@/lib/community/queries/members', () => ({
  getMembers: mocks.getMembers,
}));

vi.mock('@/lib/courses/services/course.service', () => ({
  getCourses: mocks.getCourses,
}));

vi.mock('@/lib/learning/queries/course', () => ({
  getCourseContent: mocks.getCourseContent,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...rest }: { children: ReactNode; href: string }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

import TestingEventDetailPage from './page';

describe('Testing Event detail page', () => {
  it('renders the manager workflow from live event projections', async () => {
    mocks.getMembers.mockResolvedValue({
      members: [{ id: 'user-1', displayName: 'Ana Reviewer', email: 'ana@example.com' }],
    });
    mocks.getCourses.mockResolvedValue([{ id: 'course-1', title: 'Playtesting craft', slug: 'playtesting-craft' }]);
    mocks.getCourseContent.mockResolvedValue({ items: [{ id: 'lesson-1', title: 'Feedback practice', type: 'Assignment' }], total: 1 });
    mocks.getTestingEventManagerData.mockResolvedValue({
      accessIssues: [],
      event: {
        id: 'event-1',
        name: 'August campus playtest',
        description: 'Moderated testing for community projects.',
        mode: 'InPerson',
        status: 'ApplicationsOpen',
        approvalMode: 'Committee',
        startsAt: '2026-08-12T18:00:00.000Z',
        endsAt: '2026-08-12T22:00:00.000Z',
        requiresFeedback: true,
      },
      slots: [
        {
          id: 'slot-1',
          mode: 'InPerson',
          startsAt: '2026-08-12T18:00:00.000Z',
          endsAt: '2026-08-12T19:00:00.000Z',
          campusName: 'Downtown campus',
          roomName: 'Lab 204',
          maxTesters: 10,
          maxProjects: 3,
          registeredTesterCount: 2,
          approvedProjectCount: 1,
        },
      ],
      applications: [
        {
          id: 'application-1',
          projectId: 'project-1',
          submittedByUserId: 'owner-1',
          status: 'Pending',
        },
      ],
      committee: [
        {
          id: 'committee-1',
          userId: 'user-1',
          userName: 'Ana Reviewer',
          userEmail: 'ana@example.com',
          isChair: true,
          isActive: true,
        },
      ],
      registrationsBySlot: {
        'slot-1': [
          {
            id: 'registration-1',
            userId: 'tester-1',
            status: 'Registered',
            pendingFeedbackCount: 1,
          },
        ],
      },
    });

    render(
      await TestingEventDetailPage({
        params: Promise.resolve({ eventId: 'event-1' }),
        searchParams: Promise.resolve({}),
      }),
    );

    expect(screen.getByRole('heading', { name: 'August campus playtest' })).toBeInTheDocument();
    expect(screen.getByText('Applications Open')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Schedule and capacity' })).toBeInTheDocument();
    expect(screen.getByText('Downtown campus · Lab 204')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Project applications' })).toBeInTheDocument();
    expect(screen.getByText('project-1')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Review committee' })).toBeInTheDocument();
    expect(screen.getByText('Ana Reviewer')).toBeInTheDocument();
    expect(screen.getByText(/1 pending feedback/i)).toBeInTheDocument();
  });
});
