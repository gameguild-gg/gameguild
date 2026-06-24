import { render, screen, within } from '@testing-library/react';
import type React from 'react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getMemberAccessDirectory: vi.fn(),
  updateMemberAccessRole: vi.fn(),
}));

vi.mock('@/lib/community', () => ({
  COMMUNITY_ACCESS_ROLES: [
    { value: 'Member', label: 'Member', description: 'Community access.' },
    { value: 'Moderator', label: 'Moderator', description: 'Moderation access.' },
    { value: 'TenantAdmin', label: 'Platform admin', description: 'Tenant operations access.' },
    { value: 'SystemAdmin', label: 'Super admin', description: 'Full platform access.' },
  ],
  getMemberAccessDirectory: mocks.getMemberAccessDirectory,
}));

vi.mock('@/lib/community/actions/member-access', () => ({
  updateMemberAccessRole: mocks.updateMemberAccessRole,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: { href: string; children: React.ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

import UsersAndRolesPage from './page';

describe('community users and roles page', () => {
  it('renders member counts and promotion controls from the access directory', async () => {
    mocks.getMemberAccessDirectory.mockResolvedValue({
      total: 2,
      currentUserId: 'user-admin',
      error: null,
      members: [
        {
          member: {
            id: 'user-admin',
            username: 'admin',
            displayName: 'Admin User',
            email: 'admin@game-guild.com',
            role: 'admin',
            status: 'active',
            joinedAt: '2026-01-01T00:00:00.000Z',
            lastActiveAt: '2026-06-01T00:00:00.000Z',
          },
          memberships: [{ tenantId: 'tenant-1', tenantName: 'GameGuild', role: 'SystemAdmin', isActive: true }],
          primaryMembership: { tenantId: 'tenant-1', tenantName: 'GameGuild', role: 'SystemAdmin', isActive: true },
          role: 'SystemAdmin',
          isSuperAdmin: true,
          isCurrentUser: true,
          membershipLoadError: null,
        },
        {
          member: {
            id: 'user-member',
            username: 'member',
            displayName: 'Member User',
            email: 'member@game-guild.com',
            role: 'member',
            status: 'active',
            joinedAt: '2026-02-01T00:00:00.000Z',
            lastActiveAt: '2026-06-02T00:00:00.000Z',
          },
          memberships: [{ tenantId: 'tenant-1', tenantName: 'GameGuild', role: 'Member', isActive: true }],
          primaryMembership: { tenantId: 'tenant-1', tenantName: 'GameGuild', role: 'Member', isActive: true },
          role: 'Member',
          isSuperAdmin: false,
          isCurrentUser: false,
          membershipLoadError: null,
        },
      ],
    });

    render(await UsersAndRolesPage({ searchParams: Promise.resolve({}) }));

    expect(screen.getByRole('heading', { name: 'Users and roles' })).toBeInTheDocument();
    expect(screen.getByText('Loaded from the identity API')).toBeInTheDocument();
    expect(screen.getByText('Full platform authority')).toBeInTheDocument();
    expect(screen.getByText('Admin User')).toBeInTheDocument();
    expect(screen.getByText('Member User')).toBeInTheDocument();
    expect(screen.getAllByText('GameGuild')).toHaveLength(2);
    expect(screen.getByText('You')).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: 'Save' })).toHaveLength(2);

    const row = screen.getByText('Member User').closest('tr');
    expect(row).not.toBeNull();
    expect(within(row!).getAllByText('Member').length).toBeGreaterThan(0);
  });
});
