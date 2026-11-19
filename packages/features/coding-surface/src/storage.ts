export interface CodingWorkspaceScope {
  readonly tenantId: string;
  readonly assessmentRevisionId: string;
  readonly actorId: string;
  readonly attemptId: string;
  readonly workspaceId?: string;
}

export function codingWorkspaceStorageKey(scope: CodingWorkspaceScope): string {
  const values = [scope.tenantId, scope.assessmentRevisionId, scope.actorId, scope.attemptId, scope.workspaceId ?? 'default'];
  if (values.some((value) => !value.trim())) throw new TypeError('Coding workspace storage scope must be complete.');
  return `gameguild.coding.workspace.${values.map(encodeURIComponent).join('.')}.v1`;
}
