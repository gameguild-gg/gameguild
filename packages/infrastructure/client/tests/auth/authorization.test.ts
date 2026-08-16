/**
 * Tests for Authorization Utilities
 */
import { describe, it, expect } from 'vitest';
import { hasRole, hasAllRoles, hasAnyRole, hasPermission, hasAllPermissions, hasAnyPermission, can } from '../../src/runtime/auth/authorization.js';
import type { Session, SessionUser } from '../../src/runtime/auth/types.js';

const adminUser: SessionUser = {
  id: 'u1',
  email: 'admin@test.com',
  name: 'Admin',
  roles: ['admin', 'editor'],
  permissions: ['content:read', 'content:write', 'content:delete', 'user:manage'],
};

const viewerUser: SessionUser = {
  id: 'u2',
  email: 'viewer@test.com',
  name: 'Viewer',
  roles: ['viewer'],
  permissions: ['content:read'],
};

const noRolesUser: SessionUser = {
  id: 'u3',
  email: 'noroles@test.com',
};

function makeSession(user: SessionUser): Session {
  return {
    user,
    expires: new Date(Date.now() + 86400000).toISOString(),
  };
}

describe('Authorization Utilities', () => {
  describe('hasRole', () => {
    it('returns true when user has the role', () => {
      expect(hasRole(adminUser, 'admin')).toBe(true);
      expect(hasRole(adminUser, 'editor')).toBe(true);
    });

    it('returns false when user does not have the role', () => {
      expect(hasRole(adminUser, 'superadmin')).toBe(false);
      expect(hasRole(viewerUser, 'admin')).toBe(false);
    });

    it('returns false for null/undefined input', () => {
      expect(hasRole(null, 'admin')).toBe(false);
      expect(hasRole(undefined, 'admin')).toBe(false);
    });

    it('returns false when user has no roles array', () => {
      expect(hasRole(noRolesUser, 'admin')).toBe(false);
    });

    it('works with Session objects (not just SessionUser)', () => {
      const session = makeSession(adminUser);
      expect(hasRole(session, 'admin')).toBe(true);
      expect(hasRole(session, 'superadmin')).toBe(false);
    });
  });

  describe('hasAllRoles', () => {
    it('returns true when user has all specified roles', () => {
      expect(hasAllRoles(adminUser, ['admin', 'editor'])).toBe(true);
    });

    it('returns false when user is missing any role', () => {
      expect(hasAllRoles(adminUser, ['admin', 'superadmin'])).toBe(false);
    });

    it('returns true for empty roles array', () => {
      expect(hasAllRoles(adminUser, [])).toBe(true);
    });

    it('returns false for null/undefined input', () => {
      expect(hasAllRoles(null, ['admin'])).toBe(false);
      expect(hasAllRoles(undefined, ['admin'])).toBe(false);
    });

    it('returns false when user has no roles array', () => {
      expect(hasAllRoles(noRolesUser, ['admin'])).toBe(false);
    });
  });

  describe('hasAnyRole', () => {
    it('returns true when user has at least one role', () => {
      expect(hasAnyRole(viewerUser, ['admin', 'viewer'])).toBe(true);
    });

    it('returns false when user has none of the roles', () => {
      expect(hasAnyRole(viewerUser, ['admin', 'editor'])).toBe(false);
    });

    it('returns false for null/undefined input', () => {
      expect(hasAnyRole(null, ['admin'])).toBe(false);
      expect(hasAnyRole(undefined, ['admin'])).toBe(false);
    });

    it('returns false when user has no roles array', () => {
      expect(hasAnyRole(noRolesUser, ['viewer'])).toBe(false);
    });
  });

  describe('hasPermission', () => {
    it('returns true for granted permission', () => {
      expect(hasPermission(adminUser, 'content:write')).toBe(true);
    });

    it('returns false for missing permission', () => {
      expect(hasPermission(viewerUser, 'content:write')).toBe(false);
    });

    it('returns false for null session', () => {
      expect(hasPermission(null, 'content:read')).toBe(false);
    });
  });

  describe('hasAllPermissions', () => {
    it('returns true when all permissions are present', () => {
      expect(hasAllPermissions(adminUser, ['content:read', 'content:write'])).toBe(true);
    });

    it('returns false when any permission is missing', () => {
      expect(hasAllPermissions(viewerUser, ['content:read', 'content:write'])).toBe(false);
    });

    it('returns false for null/undefined input', () => {
      expect(hasAllPermissions(null, ['content:read'])).toBe(false);
      expect(hasAllPermissions(undefined, ['content:read'])).toBe(false);
    });

    it('returns false when user has no permissions array', () => {
      expect(hasAllPermissions(noRolesUser, ['content:read'])).toBe(false);
    });
  });

  describe('hasAnyPermission', () => {
    it('returns true when any permission matches', () => {
      expect(hasAnyPermission(viewerUser, ['content:read', 'content:write'])).toBe(true);
    });

    it('returns false when no permissions match', () => {
      expect(hasAnyPermission(viewerUser, ['content:write', 'user:manage'])).toBe(false);
    });

    it('returns false for null/undefined input', () => {
      expect(hasAnyPermission(null, ['content:read'])).toBe(false);
      expect(hasAnyPermission(undefined, ['content:read'])).toBe(false);
    });

    it('returns false when user has no permissions array', () => {
      expect(hasAnyPermission(noRolesUser, ['content:read'])).toBe(false);
    });
  });

  describe('can', () => {
    it('maps action+resource to permission string', () => {
      expect(can(adminUser, 'write', 'content')).toBe(true); // checks 'content:write'
      expect(can(adminUser, 'delete', 'content')).toBe(true);
      expect(can(adminUser, 'manage', 'user')).toBe(true);
    });

    it('returns false for unauthorized action', () => {
      expect(can(viewerUser, 'write', 'content')).toBe(false);
      expect(can(viewerUser, 'delete', 'content')).toBe(false);
    });

    it('works with Session objects', () => {
      const session = makeSession(adminUser);
      expect(can(session, 'read', 'content')).toBe(true);
      expect(can(session, 'hack', 'system')).toBe(false);
    });
  });
});
