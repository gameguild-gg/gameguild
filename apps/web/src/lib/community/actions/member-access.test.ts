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

import { updateMemberAccessRole } from './member-access';

describe('updateMemberAccessRole', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getToken.mockResolvedValue('access-token');
    mocks.createServerClient.mockReturnValue({ request: mocks.request });
    mocks.request.mockResolvedValue({ ok: true, data: { success: true } });
  });

  it('patches the user membership role and revalidates the dashboard routes', async () => {
    const formData = new FormData();
    formData.set('userId', 'user-1');
    formData.set('tenantId', 'tenant-1');
    formData.set('role', 'SystemAdmin');

    await expect(updateMemberAccessRole(formData)).rejects.toThrow(
      'redirect:/dashboard/community/members/users?message=Updated+member+role+to+SystemAdmin.',
    );

    expect(mocks.request).toHaveBeenCalledWith({
      method: 'PATCH',
      path: '/v1/users/user-1/memberships/tenant-1/role',
      body: { role: 'SystemAdmin' },
      requiresAuth: true,
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/community');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/community/members/users');
  });

  it('redirects with an error when required fields are missing', async () => {
    const formData = new FormData();
    formData.set('userId', 'user-1');

    await expect(updateMemberAccessRole(formData)).rejects.toThrow(
      'redirect:/dashboard/community/members/users?error=User%2C+tenant%2C+and+role+are+required+to+update+access.',
    );

    expect(mocks.request).not.toHaveBeenCalled();
  });
});
