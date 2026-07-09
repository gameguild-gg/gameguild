import { render, screen, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

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

function buildMemberAccessRow(overrides: Partial<Record<string, unknown>> = {}) {
  const id = String(overrides.id ?? 'user-member');
  const role = String(overrides.role ?? 'Member');
  const tenantId = overrides.tenantId === null ? null : String(overrides.tenantId ?? 'tenant-1');
  const tenantName = overrides.tenantName === null ? null : String(overrides.tenantName ?? 'GameGuild');
  const primaryMembership = tenantId
    ? {
        tenantId,
        tenantName,
        tenantSlug: tenantName?.toLowerCase(),
        role,
        isActive: true,
      }
    : null;

  return {
    member: {
      id,
      username: String(overrides.username ?? id),
      displayName: String(overrides.displayName ?? id),
      email: String(overrides.email ?? `${id}@game-guild.com`),
      role: String(overrides.memberRole ?? role.toLowerCase()),
      status: String(overrides.status ?? 'active'),
      joinedAt: '2026-01-01T00:00:00.000Z',
      lastActiveAt: '2026-06-01T00:00:00.000Z',
    },
    memberships: primaryMembership ? [primaryMembership] : [],
    primaryMembership,
    role,
    isSuperAdmin: Boolean(overrides.isSuperAdmin ?? role === 'SystemAdmin'),
    isCurrentUser: Boolean(overrides.isCurrentUser ?? false),
    membershipLoadError: overrides.membershipLoadError ?? null,
  };
}

import RolesPage from './page';

describe('platform roles page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders role catalog and assignment controls outside the community users table', async () => {
    mocks.getMemberAccessDirectory.mockResolvedValue({
      total: 3,
      currentUserId: 'user-super',
      error: null,
      members: [
        buildMemberAccessRow({
          id: 'user-super',
          displayName: 'Super Admin',
          email: 'admin@game-guild.com',
          role: 'SystemAdmin',
          isCurrentUser: true,
        }),
        buildMemberAccessRow({
          id: 'user-admin',
          displayName: 'Platform Admin',
          role: 'TenantAdmin',
        }),
        buildMemberAccessRow({
          id: 'user-member',
          displayName: 'Community Member',
          role: 'Member',
          tenantId: null,
        }),
      ],
    });

    render(await RolesPage({ searchParams: Promise.resolve({ message: 'Role changed.' }) }));

    expect(screen.getByRole('heading', { name: 'Roles' })).toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent('Role changed.');
    expect(screen.getByText('Role assignments')).toBeInTheDocument();
    expect(screen.getByText('Role catalog')).toBeInTheDocument();
    expect(screen.getByText('Full platform access.')).toBeInTheDocument();

    const superAdminRow = screen.getByText('Super Admin').closest('tr');
    expect(superAdminRow).not.toBeNull();
    expect(within(superAdminRow!).getByText('You')).toBeInTheDocument();
    expect(within(superAdminRow!).getByRole('button', { name: 'Save' })).toBeInTheDocument();

    const memberRow = screen.getByText('Community Member').closest('tr');
    expect(memberRow).not.toBeNull();
    expect(within(memberRow!).getByText('No membership')).toBeInTheDocument();
  });
});
