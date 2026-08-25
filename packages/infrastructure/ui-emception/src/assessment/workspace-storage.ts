/** Default localStorage key retained for existing non-assessment workspaces. */
export const WORKSPACE_STORAGE_KEY = 'gameguild.emception.workspace.v1';

/**
 * Per-assessment localStorage key. The optional workspace id isolates draft
 * layouts when an author changes an assessment's configured language.
 */
export function workspaceStorageKey(assessmentToken?: string, workspaceId?: string): string {
  if (assessmentToken && workspaceId) {
    return `gameguild.emception.workspace.${assessmentToken}.${workspaceId}.v2`;
  }
  return assessmentToken
    ? `gameguild.emception.workspace.${assessmentToken}.v2`
    : WORKSPACE_STORAGE_KEY;
}

/** Return the pre-user-namespacing token for one-time draft restoration. */
export function legacyAssessmentToken(assessmentToken?: string): string | undefined {
  if (!assessmentToken) return undefined;
  const separator = assessmentToken.lastIndexOf(':');
  return separator === -1 ? assessmentToken : assessmentToken.slice(separator + 1);
}
