/** The single writable user mount exposed by the Emception Browser toolchain. */
export const ASSESSMENT_WORKSPACE_MOUNT = '/home/user';

/**
 * Translate the historical GameGuild `/user/*` workspace convention to the
 * Toolchain's physical VFS mount. Already canonical and relative paths stay
 * unchanged, so persisted submissions retain their original path contract.
 */
export function normalizeAssessmentWorkspacePath(path: string): string {
  if (path === '/user') return ASSESSMENT_WORKSPACE_MOUNT;
  if (path.startsWith('/user/')) {
    return `${ASSESSMENT_WORKSPACE_MOUNT}/${path.slice('/user/'.length)}`;
  }
  return path;
}
