import { render, screen, within } from '@testing-library/react';
import type React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getGroups: vi.fn(),
  createCommunityGroup: vi.fn(),
}));

vi.mock('@/lib/community', () => ({
  getGroups: mocks.getGroups,
}));

vi.mock('@/lib/community/actions/groups', () => ({
  createCommunityGroup: mocks.createCommunityGroup,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: { href: string; children: React.ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

import GroupsPage from './page';

describe('community groups page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getGroups.mockResolvedValue({
      total: 1,
      groups: [
        {
          id: 'group-1',
          name: 'Pixel Art Mentors',
          description: 'Mentor-led critiques',
          memberCount: 3,
          pendingMemberCount: 1,
          createdAt: '2026-01-01T00:00:00.000Z',
          isPublic: false,
          visibility: 'InviteOnly',
          status: 'Active',
          type: 'StudyGroup',
        },
      ],
    });
  });

  it('links every group to its member-management detail page', async () => {
    render(await GroupsPage({ searchParams: Promise.resolve({}) }));

    expect(screen.getByRole('heading', { name: 'Groups' })).toBeInTheDocument();

    const row = screen.getByText('Pixel Art Mentors').closest('tr');
    expect(row).not.toBeNull();
    expect(within(row!).getByRole('link', { name: 'Pixel Art Mentors' })).toHaveAttribute(
      'href',
      '/dashboard/community/members/groups/group-1',
    );
    expect(within(row!).getByText('InviteOnly')).toBeInTheDocument();
    expect(within(row!).getByText('1 pending')).toBeInTheDocument();
  });
});
