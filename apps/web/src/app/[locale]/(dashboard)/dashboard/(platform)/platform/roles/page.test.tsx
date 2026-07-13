import { render, screen, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getMemberAccessDirectory: vi.fn(),
  getPlatformRoles: vi.fn(),
  getPermissionTemplates: vi.fn(),
  getUserPlatformRoles: vi.fn(),
  updateMemberAccessRole: vi.fn(),
  createPlatformRole: vi.fn(),
  updatePlatformRole: vi.fn(),
  deletePlatformRole: vi.fn(),
  assignPlatformRole: vi.fn(),
  removePlatformRole: vi.fn(),
}));

vi.mock('@/lib/community', () => ({
  COMMUNITY_ACCESS_ROLES: [
    { value: 'Member', label: 'Member', description: 'Community access.' },
    { value: 'Moderator', label: 'Moderator', description: 'Moderation access.' },
    { value: 'TenantAdmin', label: 'Platform admin', description: 'Tenant operations access.' },
    { value: 'SystemAdmin', label: 'Super admin', description: 'Full platform access.' },
  ],
  getMemberAccessDirectory: mocks.getMemberAccessDirectory,
  getPlatformRoles: mocks.getPlatformRoles,
  getPermissionTemplates: mocks.getPermissionTemplates,
  getUserPlatformRoles: mocks.getUserPlatformRoles,
  PLATFORM_PERMISSION_MATRIX: [
    {
      area: 'Learning',
      description: 'Learning operations',
      permissions: [
        { value: 'courses:read', label: 'View courses' },
        { value: 'courses:update', label: 'Edit courses' },
      ],
    },
    {
      area: 'Platform',
      description: 'Platform operations',
      permissions: [{ value: 'roles:read', label: 'View roles' }],
    },
  ],
}));

vi.mock('@/lib/community/actions/member-access', () => ({
  updateMemberAccessRole: mocks.updateMemberAccessRole,
}));

vi.mock('@/lib/community/actions/roles', () => ({
  createPlatformRole: mocks.createPlatformRole,
  updatePlatformRole: mocks.updatePlatformRole,
  deletePlatformRole: mocks.deletePlatformRole,
  assignPlatformRole: mocks.assignPlatformRole,
  removePlatformRole: mocks.removePlatformRole,
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
    mocks.getPlatformRoles.mockResolvedValue({
      roles: [
        {
          id: 'role-course-operator',
          name: 'Course Operator',
          description: 'Runs learning operations.',
          permissions: ['courses:read', 'courses:update'],
          isActive: true,
          tenantId: null,
          createdAt: '2026-01-01T00:00:00.000Z',
          updatedAt: '2026-01-02T00:00:00.000Z',
        },
      ],
      error: null,
    });
    mocks.getPermissionTemplates.mockResolvedValue({
      templates: [
        {
          id: 'template-learning',
          name: 'Learning manager',
          description: 'Recommended permissions for learning operators.',
          category: 'Learning',
          permissions: ['courses:read', 'courses:update'],
          isSystemTemplate: true,
          isActive: true,
          createdAt: '2026-01-01T00:00:00.000Z',
        },
      ],
      error: null,
    });
    mocks.getUserPlatformRoles.mockImplementation(async (userId: string) => ({
      roles:
        userId === 'user-admin'
          ? [
              {
                id: 'role-course-operator',
                name: 'Course Operator',
                description: 'Runs learning operations.',
                permissions: ['courses:read', 'courses:update'],
                isActive: true,
                tenantId: null,
                createdAt: '2026-01-01T00:00:00.000Z',
                updatedAt: '2026-01-02T00:00:00.000Z',
              },
            ]
          : [],
      error: null,
    }));
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
    expect(screen.getByText('Workspace access roles')).toBeInTheDocument();
    expect(screen.getByText('Full platform access.')).toBeInTheDocument();
    expect(screen.getByText('Custom roles')).toBeInTheDocument();
    expect(screen.getAllByText('Course Operator').length).toBeGreaterThan(0);
    expect(screen.getByText('Permission matrix')).toBeInTheDocument();
    expect(screen.getAllByText('View courses').length).toBeGreaterThan(0);
    expect(screen.getByText('Learning manager')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Create role' })).toBeInTheDocument();

    const workspaceAssignments = screen.getByText('Role assignments').closest('[data-slot="card"]');
    expect(workspaceAssignments).not.toBeNull();
    const superAdminRow = within(workspaceAssignments!).getByText('Super Admin').closest('tr');
    expect(superAdminRow).not.toBeNull();
    expect(within(superAdminRow!).getByText('You')).toBeInTheDocument();
    expect(within(superAdminRow!).queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
    expect(within(superAdminRow!).getByText('Transfer super admin before changing this account.')).toBeInTheDocument();

    const customAssignments = screen.getByText('Custom role assignments').closest('[data-slot="card"]');
    expect(customAssignments).not.toBeNull();
    const customSuperAdminRow = within(customAssignments!).getByText('Super Admin').closest('tr');
    expect(customSuperAdminRow).not.toBeNull();
    expect(within(customSuperAdminRow!).getByRole('button', { name: 'Assign custom role' })).toBeInTheDocument();

    const platformAdminRow = within(customAssignments!).getByText('Platform Admin').closest('tr');
    expect(platformAdminRow).not.toBeNull();
    expect(within(platformAdminRow!).getByText('Course Operator')).toBeInTheDocument();
    expect(within(platformAdminRow!).getByRole('button', { name: 'Remove Course Operator from Platform Admin' })).toBeInTheDocument();

    const memberRow = within(workspaceAssignments!).getByText('Community Member').closest('tr');
    expect(memberRow).not.toBeNull();
    expect(within(memberRow!).getByText('No membership')).toBeInTheDocument();
  });
});
