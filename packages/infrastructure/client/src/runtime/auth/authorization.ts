/**
 * Authorization Utilities
 *
 * Role and permission checking helpers for use in pages,
 * components, server actions, and proxy/middleware.
 *
 * These operate on the Session/SessionUser types which now carry
 * `roles` and `permissions` arrays populated from the backend.
 *
 * @example
 * ```ts
 * import { hasRole, hasPermission, can } from '@game-guild/client';
 *
 * const session = await auth();
 * if (hasRole(session, 'admin')) { ... }
 * if (hasPermission(session, 'content:write')) { ... }
 * if (can(session, 'write', 'content')) { ... }
 * ```
 */

import type { Session, SessionUser } from './types.js';

// ─── Role Checks ─────────────────────────────────────────────────

/**
 * Check if a session's user has a specific role.
 */
export function hasRole(session: Session | SessionUser | null | undefined, role: string): boolean {
  const user = resolveUser(session);
  if (!user?.roles) return false;
  return user.roles.includes(role);
}

/**
 * Check if a session's user has ALL of the specified roles.
 */
export function hasAllRoles(session: Session | SessionUser | null | undefined, roles: string[]): boolean {
  const user = resolveUser(session);
  if (!user?.roles) return false;
  return roles.every((r) => user.roles!.includes(r));
}

/**
 * Check if a session's user has at least ONE of the specified roles.
 */
export function hasAnyRole(session: Session | SessionUser | null | undefined, roles: string[]): boolean {
  const user = resolveUser(session);
  if (!user?.roles) return false;
  return roles.some((r) => user.roles!.includes(r));
}

// ─── Permission Checks ──────────────────────────────────────────

/**
 * Check if a session's user has a specific permission.
 */
export function hasPermission(session: Session | SessionUser | null | undefined, permission: string): boolean {
  const user = resolveUser(session);
  if (!user?.permissions) return false;
  return user.permissions.includes(permission);
}

/**
 * Check if a session's user has ALL of the specified permissions.
 */
export function hasAllPermissions(session: Session | SessionUser | null | undefined, permissions: string[]): boolean {
  const user = resolveUser(session);
  if (!user?.permissions) return false;
  return permissions.every((p) => user.permissions!.includes(p));
}

/**
 * Check if a session's user has at least ONE of the specified permissions.
 */
export function hasAnyPermission(session: Session | SessionUser | null | undefined, permissions: string[]): boolean {
  const user = resolveUser(session);
  if (!user?.permissions) return false;
  return permissions.some((p) => user.permissions!.includes(p));
}

// ─── Shorthand ───────────────────────────────────────────────────

/**
 * Shorthand: check if user can perform an action on a resource.
 *
 * Maps to permission string `"<resource>:<action>"`.
 *
 * @example
 * ```ts
 * can(session, 'write', 'content')   // checks 'content:write'
 * can(session, 'delete', 'project')  // checks 'project:delete'
 * ```
 */
export function can(session: Session | SessionUser | null | undefined, action: string, resource: string): boolean {
  return hasPermission(session, `${resource}:${action}`);
}

// ─── Helpers ─────────────────────────────────────────────────────

function resolveUser(input: Session | SessionUser | null | undefined): SessionUser | null {
  if (!input) return null;
  // If it has a `user` property, it's a Session
  if ('user' in input && 'expires' in input) {
    return (input as Session).user;
  }
  // Otherwise it's a SessionUser directly
  return input as SessionUser;
}
