import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ClassDetailPage from './page';
import { updateCourseClass, updateCourseClassStatus } from '@/lib/learning/actions';

const mocks = vi.hoisted(() => ({
  getCourse: vi.fn(),
  getCourseClass: vi.fn(),
  notFound: vi.fn(),
  refresh: vi.fn(),
}));

vi.mock('@/lib/learning', () => ({
  getCourse: mocks.getCourse,
  getCourseClass: mocks.getCourseClass,
}));

vi.mock('@/lib/learning/actions', () => ({
  updateCourseClass: vi.fn(),
  updateCourseClassStatus: vi.fn(),
}));

vi.mock('next/navigation', () => ({
  notFound: mocks.notFound,
  useRouter: () => ({ refresh: mocks.refresh }),
}));

const course = {
  id: 'course-1',
  title: 'Advanced Game AI',
};

const classDetail = {
  id: 'class-1',
  title: 'June production cohort',
  description: 'Live feedback and review sessions.',
  status: 'scheduled',
  scheduledAt: '2026-07-01T13:00:00.000Z',
  duration: 120,
  timezone: 'UTC',
  location: { type: 'virtual', meetingUrl: 'https://meet.example/session' },
  attendeeCount: 1,
  maxAttendees: 20,
  materials: [],
  attendees: [
    {
      id: 'attendee-1',
      userId: 'student-1',
      status: 'active',
      progress: 45,
      enrolledAt: '2026-06-01T00:00:00.000Z',
      completedAt: null,
      lastActivityAt: '2026-06-15T00:00:00.000Z',
    },
  ],
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-02T00:00:00.000Z',
};

describe('ClassDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getCourse.mockResolvedValue(course);
    mocks.getCourseClass.mockResolvedValue(classDetail);
    mocks.notFound.mockImplementation(() => {
      throw new Error('not-found');
    });
    vi.mocked(updateCourseClass).mockResolvedValue({ success: true, data: { id: 'class-1' } });
    vi.mocked(updateCourseClassStatus).mockResolvedValue({ success: true, data: null });
  });

  it('renders class detail, attendee progress, and live class controls', async () => {
    render(await ClassDetailPage({ params: Promise.resolve({ course: 'course-1', classId: 'class-1' }) }));

    expect(mocks.getCourse).toHaveBeenCalledWith('course-1');
    expect(mocks.getCourseClass).toHaveBeenCalledWith('class-1');
    expect(screen.getByText('June production cohort')).toBeInTheDocument();
    expect(screen.getByText('Advanced Game AI')).toBeInTheDocument();
    expect(screen.getAllByText('Live feedback and review sessions.').length).toBeGreaterThan(0);
    expect(screen.getByText((_, element) => element?.textContent === '1/20')).toBeInTheDocument();
    expect(screen.getByText('https://meet.example/session')).toBeInTheDocument();

    const attendeeCard = screen.getByText('student-1').closest('.rounded-lg');
    expect(attendeeCard).not.toBeNull();
    expect(within(attendeeCard!).getByText('active')).toBeInTheDocument();
    expect(within(attendeeCard!).getByText('45% progress')).toBeInTheDocument();
  });

  it('saves edited class details and runs status actions', async () => {
    render(await ClassDetailPage({ params: Promise.resolve({ course: 'course-1', classId: 'class-1' }) }));

    fireEvent.change(screen.getByLabelText(/^name$/i), { target: { value: 'Updated cohort' } });
    fireEvent.change(screen.getByLabelText(/^description$/i), { target: { value: 'Updated live feedback.' } });
    fireEvent.change(screen.getByLabelText(/^capacity$/i), { target: { value: '30' } });
    fireEvent.change(screen.getByLabelText(/meeting url or room/i), { target: { value: 'https://meet.example/new' } });
    fireEvent.click(screen.getByRole('button', { name: /save class/i }));

    await waitFor(() => {
      expect(updateCourseClass).toHaveBeenCalledWith(expect.objectContaining({
        courseId: 'course-1',
        classId: 'class-1',
        name: 'Updated cohort',
        description: 'Updated live feedback.',
        maxCapacity: 30,
        meetingSchedule: 'https://meet.example/new',
      }));
    });
    expect(await screen.findByRole('status')).toHaveTextContent('Class updated.');
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /open enrollment/i })).toBeEnabled();
    });
    expect(mocks.refresh).not.toHaveBeenCalled();

  });

  it('runs class lifecycle status actions', async () => {
    render(await ClassDetailPage({ params: Promise.resolve({ course: 'course-1', classId: 'class-1' }) }));

    fireEvent.click(screen.getByRole('button', { name: /open enrollment/i }));

    await waitFor(() => {
      expect(updateCourseClassStatus).toHaveBeenCalledWith('course-1', 'class-1', 'open');
    });
    expect(await screen.findByRole('status')).toHaveTextContent('Class status updated.');
    expect(screen.getByText('live')).toBeInTheDocument();
    expect(mocks.refresh).not.toHaveBeenCalled();
  });

  it('uses not-found when the course or class cannot be loaded', async () => {
    mocks.getCourse.mockResolvedValueOnce(null);
    await expect(ClassDetailPage({ params: Promise.resolve({ course: 'missing', classId: 'class-1' }) })).rejects.toThrow('not-found');

    mocks.getCourse.mockResolvedValueOnce(course);
    mocks.getCourseClass.mockResolvedValueOnce(null);
    await expect(ClassDetailPage({ params: Promise.resolve({ course: 'course-1', classId: 'missing' }) })).rejects.toThrow('not-found');

    expect(mocks.notFound).toHaveBeenCalledTimes(2);
  });
});
