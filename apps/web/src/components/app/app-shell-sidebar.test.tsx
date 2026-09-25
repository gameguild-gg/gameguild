import '@testing-library/jest-dom/vitest';
import * as React from 'react';
import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/i18n/navigation', () => ({
  Link: 'a',
  usePathname: () => '/feed',
}));
vi.mock('next/navigation', () => ({ useSearchParams: () => new URLSearchParams() }));
vi.mock('@/components/console/tenant-switcher', () => ({ TenantSwitcher: () => null }));
vi.mock('@/components/console/workspace-sidebar', () => ({
  flattenWorkspaceNavigationItems: () => [],
}));
vi.mock('@/components/ui/github-issue-modal', () => ({
  GitHubIssueModal: ({ isOpen }: { isOpen: boolean }) =>
    isOpen ? <div role="dialog">Bug report options</div> : null,
}));
vi.mock('@game-guild/ui/components/sidebar', () => {
  const Wrapper = ({ children }: { children: React.ReactNode }) => <div>{children}</div>;
  const Footer = ({ children }: { children: React.ReactNode }) => <footer>{children}</footer>;
  const Menu = ({ children }: { children: React.ReactNode }) => <ul>{children}</ul>;
  const Item = ({ children }: { children: React.ReactNode }) => <li>{children}</li>;
  const Button = ({
    render,
    tooltip,
    ...props
  }: React.ComponentProps<'button'> & { render?: React.ReactElement; tooltip?: string }) =>
    {
      void tooltip;
      return render ? React.cloneElement(render, props, props.children) : <button {...props} />;
    };

  return {
    Sidebar: Wrapper,
    SidebarContent: Wrapper,
    SidebarFooter: Footer,
    SidebarGroup: Wrapper,
    SidebarGroupContent: Wrapper,
    SidebarGroupLabel: Wrapper,
    SidebarHeader: Wrapper,
    SidebarMenu: Menu,
    SidebarMenuButton: Button,
    SidebarMenuItem: Item,
    SidebarMenuSub: Menu,
    SidebarMenuSubButton: Button,
    SidebarMenuSubItem: Item,
    SidebarRail: () => null,
    SidebarSeparator: () => <hr />,
    SidebarTrigger: () => null,
  };
});

import { AppShellSidebar } from './app-shell-sidebar';

describe('AppShellSidebar utility footer', () => {
  afterEach(() => {
    cleanup();
  });

  it('keeps only the bug-report action', async () => {
    const user = userEvent.setup();
    render(<AppShellSidebar navigation={[]} />);

    expect(screen.queryByRole('link', { name: 'Settings' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Search' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Help and community' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Contact support' })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Report a bug' }));
    expect(screen.getByRole('dialog')).toHaveTextContent('Bug report options');
  });
});
