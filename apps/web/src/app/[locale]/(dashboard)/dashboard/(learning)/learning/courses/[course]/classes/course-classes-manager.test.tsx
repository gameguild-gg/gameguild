import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CourseClassesManager } from './course-classes-manager';

const createCourseClassMock = vi.fn();
const updateCourseClassStatusMock = vi.fn();
const deleteCourseClassMock = vi.fn();
const refreshMock = vi.fn();

vi.mock('@/lib/learning/actions', () => ({
  createCourseClass: (...args: unknown[]) => createCourseClassMock(...args),
  updateCourseClassStatus: (...args: unknown[]) => updateCourseClassStatusMock(...args),
  deleteCourseClass: (...args: unknown[]) => deleteCourseClassMock(...args),
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: { href: string; children: ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock('next/navigation', () => ({
  useRouter: () => ({ refresh: refreshMock }),
}));

describe('CourseClassesManager', () => {
  beforeEach(() => {
    createCourseClassMock.mockReset();
    updateCourseClassStatusMock.mockReset();
    deleteCourseClassMock.mockReset();
    refreshMock.mockReset();
    createCourseClassMock.mockResolvedValue({ success: true, data: { id: 'class-2' } });
    updateCourseClassStatusMock.mockResolvedValue({ success: true, data: null });
    deleteCourseClassMock.mockResolvedValue({ success: true, data: null });
  });

  it('schedules a cohort/live class through the dashboard form', async () => {
    render(<CourseClassesManager courseId="course-1" courseTitle="Boss AI Production" classes={[]} />);

    fireEvent.change(screen.getByLabelText(/^name$/i), {
      target: { value: 'June production cohort' },
    });
    fireEvent.change(screen.getByLabelText(/^description$/i), {
      target: { value: 'Live feedback and review sessions.' },
    });
    fireEvent.change(screen.getByLabelText(/^start$/i), {
      target: { value: '2026-07-01T13:00' },
    });
    fireEvent.change(screen.getByLabelText(/^end$/i), {
      target: { value: '2026-07-01T15:00' },
    });
    fireEvent.change(screen.getByLabelText(/^capacity$/i), {
      target: { value: '20' },
    });
    fireEvent.change(screen.getByLabelText(/meeting url or room/i), {
      target: { value: 'https://meet.example/session' },
    });

    fireEvent.click(screen.getByRole('button', { name: /schedule class/i }));

    await waitFor(() => {
      expect(createCourseClassMock).toHaveBeenCalledWith({
        courseId: 'course-1',
        name: 'June production cohort',
        description: 'Live feedback and review sessions.',
        startDate: '2026-07-01T13:00',
        endDate: '2026-07-01T15:00',
        maxCapacity: 20,
        meetingSchedule: 'https://meet.example/session',
      });
    });
    expect(refreshMock).toHaveBeenCalled();
    expect(screen.getByText('Class scheduled.')).toBeInTheDocument();
  });

  it('renders class lifecycle actions for existing cohorts', async () => {
    render(
      <CourseClassesManager
        courseId="course-1"
        courseTitle="Boss AI Production"
        classes={[
          {
            id: 'class-1',
            title: 'June production cohort',
            description: 'Live feedback.',
            status: 'scheduled',
            scheduledAt: '2026-07-01T13:00:00.000Z',
            duration: 120,
            timezone: 'UTC',
            location: { type: 'virtual', meetingUrl: 'https://meet.example/session' },
            attendeeCount: 0,
            maxAttendees: 20,
            materials: [],
            createdAt: '2026-01-01T00:00:00.000Z',
            updatedAt: '2026-01-02T00:00:00.000Z',
          },
        ]}
      />,
    );

    expect(screen.getByRole('link', { name: /june production cohort/i })).toHaveAttribute(
      'href',
      '/dashboard/learning/courses/course-1/classes/class-1',
    );

    fireEvent.click(screen.getByRole('button', { name: /^open$/i }));

    await waitFor(() => {
      expect(updateCourseClassStatusMock).toHaveBeenCalledWith('course-1', 'class-1', 'open');
    });
  });

  it('deletes empty cohort sessions from the schedule list', async () => {
    render(
      <CourseClassesManager
        courseId="course-1"
        courseTitle="Boss AI Production"
        classes={[
          {
            id: 'class-1',
            title: 'June production cohort',
            description: 'Live feedback.',
            status: 'scheduled',
            scheduledAt: '2026-07-01T13:00:00.000Z',
            duration: 120,
            timezone: 'UTC',
            location: { type: 'virtual', meetingUrl: 'https://meet.example/session' },
            attendeeCount: 0,
            maxAttendees: 20,
            materials: [],
            createdAt: '2026-01-01T00:00:00.000Z',
            updatedAt: '2026-01-02T00:00:00.000Z',
          },
        ]}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /delete june production cohort/i }));

    await waitFor(() => {
      expect(deleteCourseClassMock).toHaveBeenCalledWith('course-1', 'class-1');
    });
  });
});
