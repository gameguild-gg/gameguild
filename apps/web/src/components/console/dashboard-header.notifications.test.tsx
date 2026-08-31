import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { DashboardHeader } from './dashboard-header';

const mocks = vi.hoisted(() => ({
  pathname: '/workspace/learning/courses',
  push: vi.fn(),
  signOut: vi.fn(),
  toastError: vi.fn(),
  setNotificationReadAction: vi.fn(),
  markAllNotificationsReadAction: vi.fn(),
}));

vi.mock('@/components/ui/theme-toggle', () => ({
  ThemeToggle: () => <button type="button">Toggle theme</button>,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...props }: { children: ReactNode; href: string }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
  usePathname: () => mocks.pathname,
  useRouter: () => ({ push: mocks.push }),
}));

vi.mock('@game-guild/client/react', () => ({
  useAuth: () => ({
    signOut: mocks.signOut,
    isLoading: false,
  }),
}));

vi.mock('@game-guild/ui/components/sidebar', () => ({
  SidebarTrigger: () => <button type="button">Toggle sidebar</button>,
}));

vi.mock('sonner', () => ({
  toast: { error: mocks.toastError },
}));

vi.mock('@/lib/notifications/mark-read-action', () => ({
  setNotificationReadAction: mocks.setNotificationReadAction,
  markAllNotificationsReadAction: mocks.markAllNotificationsReadAction,
}));

const USER = { id: 'user-123', name: 'Ada Lovelace', email: 'ada@gameguild.gg', image: null };

const NOTIFICATIONS = {
  items: [
    {
      id: 'notif-unread',
      title: 'Project review ready',
      message: 'Your project received a new review.',
      createdLabel: 'Jun 14, 10:30 AM UTC',
      isRead: false,
    },
    {
      id: 'notif-read',
      title: 'Welcome to GameGuild',
      message: 'Thanks for signing up.',
      createdLabel: 'Jun 13, 9:00 AM UTC',
      isRead: true,
    },
  ],
  unreadCount: 1,
};

async function renderHeaderWithBellOpen() {
  const user = userEvent.setup();
  render(<DashboardHeader user={USER} notifications={NOTIFICATIONS} />);
  await user.click(screen.getByRole('button', { name: /notifications/i }));
  await screen.findByRole('menu');
  return user;
}

async function ensureBellDropdownOpen(user: ReturnType<typeof userEvent.setup>) {
  if (!screen.queryByRole('menu')) {
    await user.click(screen.getByRole('button', { name: /notifications/i }));
    await screen.findByRole('menu');
  }
}

describe('DashboardHeader bell dropdown mark-read wiring', () => {
  beforeEach(() => {
    mocks.pathname = '/workspace/learning/courses';
    mocks.push.mockReset();
    mocks.signOut.mockReset();
    mocks.toastError.mockReset();
    mocks.setNotificationReadAction.mockReset();
    mocks.markAllNotificationsReadAction.mockReset();
    mocks.setNotificationReadAction.mockResolvedValue({ success: true, status: 'success' });
    mocks.markAllNotificationsReadAction.mockResolvedValue({ success: true, status: 'success' });
  });

  it('marks an unread item read on click and clears the badge optimistically', async () => {
    const user = await renderHeaderWithBellOpen();

    expect(screen.getByLabelText('Unread')).toBeInTheDocument();

    await user.click(screen.getByText('Project review ready'));

    expect(mocks.setNotificationReadAction).toHaveBeenCalledWith('notif-unread', true);

    // Base UI closes the dropdown on item activation; reopen to inspect state.
    await ensureBellDropdownOpen(user);
    await waitFor(() => {
      expect(screen.queryByLabelText('Unread')).not.toBeInTheDocument();
    });
    expect(screen.getByRole('button', { name: /mark all read/i })).toBeDisabled();
  });

  it('marks a read item unread from the hover-revealed affordance', async () => {
    const user = await renderHeaderWithBellOpen();

    await user.click(screen.getByRole('button', { name: 'Mark Welcome to GameGuild unread' }));

    expect(mocks.setNotificationReadAction).toHaveBeenCalledWith('notif-read', false);

    // The affordance stops propagation, so the menu may stay open; reopen only if Base UI closed it.
    await ensureBellDropdownOpen(user);
    await waitFor(() => {
      expect(screen.getAllByLabelText('Unread')).toHaveLength(2);
    });
  });

  it('rolls the optimistic state back and toasts when the action fails', async () => {
    mocks.setNotificationReadAction.mockResolvedValue({ success: false, status: 'error' });
    const user = await renderHeaderWithBellOpen();

    await user.click(screen.getByText('Project review ready'));

    expect(mocks.toastError).toHaveBeenCalledWith('Failed to update notifications. Please try again.');

    await ensureBellDropdownOpen(user);
    expect(await screen.findByLabelText('Unread')).toBeInTheDocument();
  });

  it('marks everything read from the footer button and clears the badge', async () => {
    const user = await renderHeaderWithBellOpen();

    expect(screen.getByText('1')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /mark all read/i }));

    expect(mocks.markAllNotificationsReadAction).toHaveBeenCalledTimes(1);
    expect(mocks.setNotificationReadAction).not.toHaveBeenCalled();
    await waitFor(() => {
      expect(screen.queryByLabelText('Unread')).not.toBeInTheDocument();
      expect(screen.queryByText('1')).not.toBeInTheDocument();
    });
    expect(screen.getByRole('button', { name: /mark all read/i })).toBeDisabled();
  });

  it('keeps a read linked item navigate-only instead of toggling it unread', async () => {
    const user = userEvent.setup();
    render(
      <DashboardHeader
        user={USER}
        notifications={{
          items: [
            {
              id: 'notif-linked',
              title: 'Course invitation',
              message: 'You were invited to a course.',
              createdLabel: 'Jun 14, 8:00 AM UTC',
              isRead: true,
              actionUrl: '/workspace/learning/courses/course-1',
            },
          ],
          unreadCount: 0,
        }}
      />,
    );
    await user.click(screen.getByRole('button', { name: /notifications/i }));
    await screen.findByRole('menu');

    await user.click(screen.getByText('Course invitation'));

    expect(mocks.setNotificationReadAction).not.toHaveBeenCalled();
  });

  it('does not fire mark-all when nothing shown is unread', async () => {
    const user = userEvent.setup();
    render(
      <DashboardHeader
        user={USER}
        notifications={{ items: [NOTIFICATIONS.items[1]], unreadCount: 0 }}
      />,
    );
    await user.click(screen.getByRole('button', { name: /notifications/i }));
    await screen.findByRole('menu');

    expect(screen.getByRole('button', { name: /mark all read/i })).toBeDisabled();

    await user.click(screen.getByRole('button', { name: /mark all read/i }));

    expect(mocks.markAllNotificationsReadAction).not.toHaveBeenCalled();
  });
});
