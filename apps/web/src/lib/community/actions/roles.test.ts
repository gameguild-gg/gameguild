import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getToken: vi.fn(),
  createServerClient: vi.fn(),
  request: vi.fn(),
  redirect: vi.fn((href: string) => {
    throw new Error(`redirect:${href}`);
  }),
  revalidatePath: vi.fn(),
}));

vi.mock('@/auth', () => ({
  getToken: mocks.getToken,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
}));

vi.mock('next/cache', () => ({
  revalidatePath: mocks.revalidatePath,
}));

vi.mock('next/navigation', () => ({
  redirect: mocks.redirect,
}));

import { assignPlatformRole, createPlatformRole, deletePlatformRole, removePlatformRole, updatePlatformRole } from './roles';

describe('platform role actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getToken.mockResolvedValue('access-token');
    mocks.createServerClient.mockReturnValue({ request: mocks.request });
    mocks.request.mockResolvedValue({ ok: true, data: { id: 'role-1', name: 'Course Operator' } });
  });

  it('creates a global role with selected permissions', async () => {
    const formData = new FormData();
    formData.set('name', 'Course Operator');
    formData.set('description', 'Runs learning operations');
    formData.append('permissions', 'courses:read');
    formData.append('permissions', 'courses:update');
    formData.append('permissions', 'roles:read');

    await expect(createPlatformRole(formData)).rejects.toThrow(
      'redirect:/dashboard/platform/roles?message=Created+role+Course+Operator.',
    );

    expect(mocks.request).toHaveBeenCalledWith({
      method: 'POST',
      path: '/v1/roles',
      body: {
        name: 'Course Operator',
        description: 'Runs learning operations',
        permissions: ['courses:read', 'courses:update', 'roles:read'],
        tenantId: null,
      },
      requiresAuth: true,
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/platform/roles');
  });

  it('updates a role permission matrix and active state', async () => {
    const formData = new FormData();
    formData.set('roleId', 'role-1');
    formData.set('name', 'Course Operator');
    formData.set('description', 'Updated description');
    formData.set('isActive', 'on');
    formData.append('permissions', 'courses:read');

    await expect(updatePlatformRole(formData)).rejects.toThrow(
      'redirect:/dashboard/platform/roles?message=Updated+role+Course+Operator.',
    );

    expect(mocks.request).toHaveBeenCalledWith({
      method: 'PUT',
      path: '/v1/roles/role-1',
      body: {
        name: 'Course Operator',
        description: 'Updated description',
        permissions: ['courses:read'],
        isActive: true,
      },
      requiresAuth: true,
    });
  });

  it('deletes a role from the platform catalog', async () => {
    const formData = new FormData();
    formData.set('roleId', 'role-1');
    formData.set('name', 'Course Operator');
    mocks.request.mockResolvedValue({ ok: true, data: undefined });

    await expect(deletePlatformRole(formData)).rejects.toThrow(
      'redirect:/dashboard/platform/roles?message=Deleted+role+Course+Operator.',
    );

    expect(mocks.request).toHaveBeenCalledWith({
      method: 'DELETE',
      path: '/v1/roles/role-1',
      requiresAuth: true,
    });
  });

  it('assigns a custom role to a platform user', async () => {
    const formData = new FormData();
    formData.set('userId', 'user-1');
    formData.set('roleId', 'role-1');
    formData.set('roleName', 'Course Operator');

    await expect(assignPlatformRole(formData)).rejects.toThrow(
      'redirect:/dashboard/platform/roles?message=Assigned+Course+Operator.',
    );

    expect(mocks.request).toHaveBeenCalledWith({
      method: 'POST',
      path: '/v1/roles/:assign',
      body: {
        userId: 'user-1',
        roleId: 'role-1',
        expiresAt: null,
      },
      requiresAuth: true,
    });
  });

  it('removes a custom role from a platform user', async () => {
    const formData = new FormData();
    formData.set('userId', 'user-1');
    formData.set('roleId', 'role-1');
    formData.set('roleName', 'Course Operator');

    await expect(removePlatformRole(formData)).rejects.toThrow(
      'redirect:/dashboard/platform/roles?message=Removed+Course+Operator.',
    );

    expect(mocks.request).toHaveBeenCalledWith({
      method: 'POST',
      path: '/v1/roles/:remove',
      body: {
        userId: 'user-1',
        roleId: 'role-1',
      },
      requiresAuth: true,
    });
  });
});
