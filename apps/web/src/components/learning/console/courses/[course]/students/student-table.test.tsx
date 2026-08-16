import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { StudentTable } from './student-table';
import { manualEnrollStudent, removeCourseStudents, sendCourseStudentMessage } from '@/lib/learning/actions';

const refreshMock = vi.fn();

vi.mock('next/navigation', () => ({
  useRouter: () => ({ refresh: refreshMock }),
}));

Object.defineProperties(HTMLElement.prototype, {
  hasPointerCapture: { value: vi.fn(() => false) },
  setPointerCapture: { value: vi.fn() },
  releasePointerCapture: { value: vi.fn() },
  scrollIntoView: { value: vi.fn() },
});

beforeAll(() => {
  global.ResizeObserver = class ResizeObserver {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
});

vi.mock('@/lib/learning/actions', () => ({
  manualEnrollStudent: vi.fn(),
  removeCourseStudents: vi.fn(),
  sendCourseStudentMessage: vi.fn(),
}));

const students = [
  {
    id: 'student-1',
    userId: 'user-1',
    name: 'Ada Learner',
    email: 'ada@example.com',
    completionPercent: 100,
    isActive: true,
    enrolledAt: '2026-06-01T00:00:00.000Z',
    lastActivity: '2026-06-10T00:00:00.000Z',
  },
  {
    id: 'student-2',
    userId: 'user-2',
    name: 'Grace Builder',
    email: 'grace@example.com',
    completionPercent: 60,
    isActive: true,
    enrolledAt: '2026-06-02T00:00:00.000Z',
    lastActivity: '2026-06-11T00:00:00.000Z',
  },
  {
    id: 'student-3',
    userId: 'user-3',
    name: 'Alan Inactive',
    email: 'alan@example.com',
    completionPercent: 20,
    isActive: false,
    enrolledAt: '2026-06-03T00:00:00.000Z',
    lastActivity: '2026-06-04T00:00:00.000Z',
  },
];

describe('StudentTable', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(manualEnrollStudent).mockResolvedValue({ success: true, data: { id: 'enrollment-1' } });
    vi.mocked(removeCourseStudents).mockResolvedValue({ success: true, data: { removed: 1 } });
    vi.mocked(sendCourseStudentMessage).mockResolvedValue({ success: true, data: { sent: 1 } });
  });

  it('filters enrolled students by search and status', async () => {
    const user = userEvent.setup();

    render(<StudentTable courseId="course-1" students={students} total={students.length} />);

    expect(screen.getByText('3 students enrolled')).toBeInTheDocument();
    expect(screen.getByText('Ada Learner')).toBeInTheDocument();
    expect(screen.getByText('Grace Builder')).toBeInTheDocument();
    expect(screen.getByText('Alan Inactive')).toBeInTheDocument();

    await user.type(screen.getByPlaceholderText(/search by name or email/i), 'grace');

    expect(screen.queryByText('Ada Learner')).not.toBeInTheDocument();
    expect(screen.getByText('Grace Builder')).toBeInTheDocument();

    await user.clear(screen.getByPlaceholderText(/search by name or email/i));
    await user.click(screen.getByRole('combobox'));
    await user.click(screen.getByRole('option', { name: /inactive/i }));

    expect(screen.getByText('Alan Inactive')).toBeInTheDocument();
    expect(screen.queryByText('Ada Learner')).not.toBeInTheDocument();
  });

  it('supports bulk selection and row action menus', async () => {
    const user = userEvent.setup();

    render(<StudentTable courseId="course-1" students={students} total={students.length} />);

    const table = screen.getByRole('table');
    await user.click(within(table).getAllByRole('checkbox')[0]);

    expect(screen.getByText('3 student(s) selected')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /send message/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^remove$/i })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Actions for Ada Learner' }));
    expect(screen.getByText('View profile')).toBeInTheDocument();
    expect(screen.getByText('View progress')).toBeInTheDocument();
    expect(screen.getByText('Remove from Course')).toBeInTheDocument();
  });

  it('supports individual selection, bulk clearing, filtered empty states, and enrollment cancel', async () => {
    const user = userEvent.setup();

    render(<StudentTable courseId="course-1" students={students} total={students.length} />);

    const table = screen.getByRole('table');
    const checkboxes = within(table).getAllByRole('checkbox');

    await user.click(checkboxes[1]);
    expect(screen.getByText('1 student(s) selected')).toBeInTheDocument();

    await user.click(checkboxes[1]);
    expect(screen.queryByText(/student\(s\) selected/i)).not.toBeInTheDocument();

    await user.click(checkboxes[0]);
    expect(screen.getByText('3 student(s) selected')).toBeInTheDocument();
    await user.click(checkboxes[0]);
    expect(screen.queryByText(/student\(s\) selected/i)).not.toBeInTheDocument();

    await user.type(screen.getByPlaceholderText(/search by name or email/i), 'nobody');
    expect(screen.getByText('No students found')).toBeInTheDocument();
    expect(screen.getByText('Try adjusting your search or filter criteria.')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /enroll student/i }));
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /cancel/i }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });

  it('manually enrolls an existing student and closes the dialog on success', async () => {
    render(<StudentTable courseId="course-1" students={[]} total={0} />);

    fireEvent.click(screen.getByRole('button', { name: /enroll student/i }));
    fireEvent.change(screen.getByLabelText(/^student$/i), { target: { value: 'new-student@example.com' } });
    fireEvent.change(screen.getByLabelText(/cohort id/i), { target: { value: 'cohort-1' } });
    fireEvent.click(screen.getByRole('button', { name: /enroll student$/i }));

    await waitFor(() => {
      expect(manualEnrollStudent).toHaveBeenCalledWith({
        courseId: 'course-1',
        userId: 'new-student@example.com',
        cohortId: 'cohort-1',
      });
    });
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    expect(screen.getByRole('status')).toHaveTextContent('Student enrolled successfully.');
    expect(refreshMock).toHaveBeenCalled();
  });

  it('renders the refreshed roster after a server refresh supplies newly enrolled students', async () => {
    const view = render(<StudentTable courseId="course-1" students={[]} total={0} />);

    view.rerender(<StudentTable courseId="course-1" students={[students[0]!]} total={1} />);

    expect(await screen.findByText('Ada Learner')).toBeInTheDocument();
    expect(screen.getByText('1 student enrolled')).toBeInTheDocument();
  });

  it('keeps the manual enrollment dialog open when the API returns a validation error', async () => {
    vi.mocked(manualEnrollStudent).mockResolvedValueOnce({ success: false, error: 'Student was not found.' });

    render(<StudentTable courseId="course-1" students={[]} total={0} />);

    fireEvent.click(screen.getByRole('button', { name: /enroll student/i }));
    fireEvent.change(screen.getByLabelText(/^student$/i), { target: { value: 'missing@example.com' } });
    fireEvent.click(screen.getByRole('button', { name: /enroll student$/i }));

    expect(await screen.findByText('Student was not found.')).toBeInTheDocument();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('removes selected students after explicit confirmation', async () => {
    const user = userEvent.setup();
    render(<StudentTable courseId="course-1" students={students} total={students.length} />);

    await user.click(within(screen.getByRole('table')).getAllByRole('checkbox')[1]);
    await user.click(screen.getByRole('button', { name: /^remove$/i }));
    expect(screen.getByRole('dialog', { name: 'Remove students' })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Confirm removal' }));

    await waitFor(() => expect(removeCourseStudents).toHaveBeenCalledWith('course-1', ['user-1']));
    expect(await screen.findByRole('status')).toHaveTextContent('1 student removed.');
  });

  it('sends a message to selected students through the course notification flow', async () => {
    const user = userEvent.setup();
    render(<StudentTable courseId="course-1" students={students} total={students.length} />);

    await user.click(within(screen.getByRole('table')).getAllByRole('checkbox')[2]);
    await user.click(screen.getByRole('button', { name: /send message/i }));
    fireEvent.change(screen.getByLabelText('Subject'), { target: { value: 'Milestone update' } });
    fireEvent.change(screen.getByLabelText('Message'), { target: { value: 'The critique session moved to Friday.' } });
    await user.click(screen.getByRole('button', { name: 'Send message' }));

    await waitFor(() => expect(sendCourseStudentMessage).toHaveBeenCalledWith({
      courseId: 'course-1',
      userIds: ['user-2'],
      subject: 'Milestone update',
      message: 'The critique session moved to Friday.',
    }));
    expect(await screen.findByRole('status')).toHaveTextContent('Message sent to 1 student.');
  });

  it('links to the member profile and opens course progress details', async () => {
    const user = userEvent.setup();
    render(<StudentTable courseId="course-1" students={students} total={students.length} />);

    await user.click(screen.getAllByRole('button', { name: 'Actions for Ada Learner' })[0]);
    expect(screen.getByRole('menuitem', { name: 'View profile' })).toHaveAttribute(
      'href',
      '/console/community/members/users/user-1',
    );
    await user.click(screen.getByRole('menuitem', { name: 'View progress' }));

    expect(screen.getByRole('dialog', { name: 'Ada Learner progress' })).toBeInTheDocument();
    expect(screen.getByText('100% complete')).toBeInTheDocument();
  });
});
