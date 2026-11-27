// @vitest-environment node
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  update: vi.fn(),
}));

vi.mock('@/auth', () => ({ auth: mocks.auth, update: mocks.update }));

import { markSessionEmailVerified } from './email-verification-session';

const SESSION_USER = {
  id: '11111111-1111-4111-8111-111111111111',
  email: 'member@example.com',
  name: 'Member',
  roles: ['User'],
  permissions: ['courses:read'],
  emailVerified: false,
};

describe('markSessionEmailVerified (#1298 J1)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.update.mockResolvedValue(null);
  });

  it('marks the signed-in session user as verified and keeps every other field', async () => {
    mocks.auth.mockResolvedValue({ user: SESSION_USER, expires: '2026-12-01T00:00:00.000Z' });

    await markSessionEmailVerified();

    expect(mocks.update).toHaveBeenCalledTimes(1);
    expect(mocks.update).toHaveBeenCalledWith({ user: { ...SESSION_USER, emailVerified: true } });
  });

  it('leaves a session alone that is already verified', async () => {
    mocks.auth.mockResolvedValue({ user: { ...SESSION_USER, emailVerified: true }, expires: 'x' });

    await markSessionEmailVerified();

    expect(mocks.update).not.toHaveBeenCalled();
  });

  it('does nothing for a signed-out visitor, who has no session to update', async () => {
    mocks.auth.mockResolvedValue(null);

    await markSessionEmailVerified();

    expect(mocks.update).not.toHaveBeenCalled();
  });
});
