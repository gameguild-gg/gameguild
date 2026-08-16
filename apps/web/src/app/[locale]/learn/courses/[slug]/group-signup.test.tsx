import '@testing-library/jest-dom/vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { LearnCourseGroups } from './groups-section';
import type { CourseGroupSetView } from '@/lib/learning/queries/assessments';
import { joinGroup, leaveGroup } from '@/lib/learning/actions';

const actionsMock = vi.hoisted(() => ({
  join: vi.fn(),
  leave: vi.fn(),
}));

const routerMocks = vi.hoisted(() => ({
  refresh: vi.fn(),
}));

vi.mock('@/lib/learning/actions', () => ({
  joinGroup: actionsMock.join,
  leaveGroup: actionsMock.leave,
}));

vi.mock('next/navigation', () => ({
  useRouter: () => routerMocks,
}));

// --- Fixtures ---------------------------------------------------------------

/** Actor ("user-me") is NOT a member of any group — Join buttons available. */
const openSets: CourseGroupSetView[] = [
  {
    id: 'set-open',
    name: 'Project teams',
    groups: [
      {
        id: 'group-3',
        name: 'Team Gamma',
        capacity: 3,
        memberCount: 0,
        members: [],
      },
      {
        id: 'group-4',
        name: 'Team Delta',
        capacity: 4,
        memberCount: 3,
        members: [
          { userId: 'user-1', displayName: 'Ada Lovelace' },
          { userId: 'user-2', displayName: 'Grace Hopper' },
          { userId: 'user-3', displayName: 'Alan Turing' },
        ],
      },
    ],
  },
];

/** Actor IS a member of group-1; group-2 is full. */
const memberSets: CourseGroupSetView[] = [
  {
    id: 'set-member',
    name: 'Lab pairs',
    groups: [
      {
        id: 'group-1',
        name: 'Team Alpha',
        capacity: 4,
        memberCount: 2,
        members: [
          { userId: 'user-me', displayName: 'Me Myself' },
          { userId: 'user-1', displayName: 'Ada Lovelace' },
        ],
      },
      {
        id: 'group-2',
        name: 'Team Beta',
        capacity: 3,
        memberCount: 3,
        members: [
          { userId: 'user-2', displayName: 'Grace Hopper' },
          { userId: 'user-3', displayName: 'Alan Turing' },
          { userId: 'user-4', displayName: 'Katherine Johnson' },
        ],
      },
    ],
  },
];

// --- Tests ------------------------------------------------------------------

describe('LearnCourseGroups', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    actionsMock.join.mockResolvedValue({ success: true, data: null });
    actionsMock.leave.mockResolvedValue({ success: true, data: null });
  });

  it('renders group names, member names, and capacity counts', () => {
    render(<LearnCourseGroups courseId="course-1" currentUserId="user-me" sets={memberSets} />);

    expect(screen.getByText('Lab pairs')).toBeInTheDocument();
    expect(screen.getByText('Team Alpha')).toBeInTheDocument();
    expect(screen.getByText('Team Beta')).toBeInTheDocument();
    expect(screen.getByText('Ada Lovelace')).toBeInTheDocument();
    expect(screen.getByText('2/4')).toBeInTheDocument();
    expect(screen.getByText('3/3')).toBeInTheDocument();
  });

  it('shows Leave on the own group and no Join anywhere in the set', () => {
    render(<LearnCourseGroups courseId="course-1" currentUserId="user-me" sets={memberSets} />);

    expect(screen.getByRole('button', { name: /leave team alpha/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^join/i })).not.toBeInTheDocument();
  });

  it('calls joinGroup with course + group ids and refreshes', async () => {
    const user = userEvent.setup();
    render(<LearnCourseGroups courseId="course-1" currentUserId="user-me" sets={openSets} />);

    await user.click(screen.getByRole('button', { name: /join team gamma/i }));

    await waitFor(() => expect(actionsMock.join).toHaveBeenCalledWith('course-1', 'group-3'));
    await waitFor(() => expect(routerMocks.refresh).toHaveBeenCalled());
  });

  it('calls leaveGroup with course + group ids', async () => {
    const user = userEvent.setup();
    render(<LearnCourseGroups courseId="course-1" currentUserId="user-me" sets={memberSets} />);

    await user.click(screen.getByRole('button', { name: /leave team alpha/i }));

    await waitFor(() => expect(actionsMock.leave).toHaveBeenCalledWith('course-1', 'group-1'));
    await waitFor(() => expect(routerMocks.refresh).toHaveBeenCalled());
  });

  it('disables Join on a full group', () => {
    render(<LearnCourseGroups courseId="course-1" currentUserId="other-user" sets={memberSets} />);

    expect(screen.getByRole('button', { name: /join team beta/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /join team alpha/i })).toBeEnabled();
  });

  it('disables both Join and Leave with a tooltip when the server reports the set locked', async () => {
    const user = userEvent.setup();
    actionsMock.join.mockResolvedValue({
      success: false,
      error: 'Group membership is locked because a linked assessment is due.',
    });
    render(<LearnCourseGroups courseId="course-1" currentUserId="user-me" sets={openSets} />);

    await user.click(screen.getByRole('button', { name: /join team gamma/i }));

    expect(await screen.findByText('Locked at deadline')).toBeInTheDocument();
    const lockedSection = screen.getByTestId('group-set-set-open');
    expect(lockedSection).toHaveTextContent('Locked at deadline');
    const joinButtons = lockedSection.querySelectorAll('button');
    joinButtons.forEach((button) => expect(button).toBeDisabled());
  });

  it('surfaces the one-group-per-set error as a message', async () => {
    const user = userEvent.setup();
    actionsMock.join.mockResolvedValue({
      success: false,
      error: 'You are already in a group in this set.',
    });
    render(<LearnCourseGroups courseId="course-1" currentUserId="user-me" sets={openSets} />);

    await user.click(screen.getByRole('button', { name: /join team gamma/i }));

    expect(await screen.findByText('You are already in a group in this set.')).toBeInTheDocument();
  });
});
