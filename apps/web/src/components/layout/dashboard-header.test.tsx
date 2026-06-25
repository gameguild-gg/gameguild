import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { DashboardHeader } from './dashboard-header';

const mocks = vi.hoisted(() => ({
  push: vi.fn(),
  signOut: vi.fn(),
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
  usePathname: () => '/dashboard/learning/courses',
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

describe('DashboardHeader', () => {
  beforeEach(() => {
    mocks.push.mockReset();
    mocks.signOut.mockReset();
    mocks.signOut.mockResolvedValue(undefined);
  });

  it('renders the signed-in user menu and routes sign-out through auth', async () => {
    const user = userEvent.setup();

    render(
      <DashboardHeader
        user={{
          id: 'user-123',
          name: 'Ada Lovelace',
          email: 'ada@gameguild.gg',
          image: null,
        }}
        notifications={{ items: [], unreadCount: 0 }}
      />,
    );

    const menuTrigger = screen.getByRole('button', { name: 'Open Ada Lovelace account menu' });
    expect(menuTrigger).toBeInTheDocument();

    await user.click(menuTrigger);

    expect(screen.getByText('ada@gameguild.gg')).toBeInTheDocument();
    expect(screen.getByRole('menuitem', { name: /my profile/i })).toHaveAttribute(
      'href',
      '/dashboard/community/members/users/user-123',
    );

    await user.click(screen.getByRole('menuitem', { name: /sign out/i }));

    await waitFor(() => {
      expect(mocks.signOut).toHaveBeenCalledWith({ redirect: false });
    });
    expect(mocks.push).toHaveBeenCalledWith('/sign-in');
  });

  it('keeps dashboard search responsive across desktop and mobile header layouts', () => {
    render(
      <DashboardHeader
        user={{
          id: 'user-123',
          name: 'Ada Lovelace',
          email: 'ada@gameguild.gg',
          image: null,
        }}
        notifications={{ items: [], unreadCount: 0 }}
      />,
    );

    const searchButtons = screen.getAllByRole('button', { name: 'Search dashboard' });
    expect(searchButtons).toHaveLength(2);
    expect(searchButtons[0]).toHaveClass('max-w-sm');
    expect(searchButtons[0]).toHaveClass('lg:max-w-md');
    expect(searchButtons[1]).toHaveClass('sm:hidden');
  });
});
