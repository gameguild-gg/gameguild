import { auth, getToken } from '@/auth';
import { createServerClient } from '@game-guild/client';
import { cache } from 'react';

export interface WorkspaceTeamMember { userId: string; authority: string | number; professionalTitle?: string | null; isActive: boolean; joinedAt: string; }
export interface WorkspaceTeam { id: string; tenantId: string; name: string; slug: string; description?: string | null; visibility: string | number; status: string | number; isPersonal: boolean; members: WorkspaceTeamMember[]; }
export interface WorkspaceTeamInvitation { id: string; invitedUserId?: string | null; invitedEmail?: string | null; authority: string | number; invitedByUserId: string; expiresAt: string; revokedAt?: string | null; usedAt?: string | null; }
export interface WorkspaceMyTeamInvitation { id: string; teamId: string; teamName: string; teamSlug: string; authority: string | number; expiresAt: string; }
export interface WorkspaceProject { id: string; title: string; slug: string; description?: string | null; shortDescription?: string | null; status: string | number; visibility: string | number; createdById?: string | null; }
export interface WorkspaceTeamProject extends WorkspaceProject { teamRole: string | number; participationMode: string | number; updatedAt: string; }
export interface WorkspaceProjectVersion { id: string; projectId: string; versionNumber: string; status: string; releaseNotes?: string | null; }
export interface WorkspaceTask { id: string; columnId: string; title: string; description?: string | null; status: string | number; priority: string | number; assigneeUserId?: string | null; milestoneId?: string | null; dueAt?: string | null; completedAt?: string | null; position: number; }
export interface WorkspaceColumn { id: string; name: string; kind: string | number; position: number; tasks: WorkspaceTask[]; }
export interface WorkspaceBoard { id: string; projectId: string; name: string; columns: WorkspaceColumn[]; }
export interface WorkspaceMilestone { id: string; name: string; description?: string | null; dueAt?: string | null; completedAt?: string | null; }
export interface WorkspaceTaskLabel { id: string; name: string; color: string; }
export interface WorkspaceTaskDetails { task: WorkspaceTask; checklist: Array<{ id: string; text: string; isCompleted: boolean; position: number }>; comments: Array<{ id: string; authorUserId: string; body: string; editedAt?: string | null; createdAt: string }>; dependencies: Array<{ id: string; dependsOnTaskId: string }>; labels: WorkspaceTaskLabel[]; }
export interface WorkspaceWorkHistory { id: string; taskId?: string | null; actorUserId: string; action: string; changesJson?: string | null; createdAt: string; }
export interface WorkspaceLibrary {
  folders: Array<{
    id: string;
    name: string;
    parentFolderId?: string | null;
    restrictionMode: string | number;
    allowedTeamIds: string[];
    allowedAuthorities: string[];
  }>;
  assets: Array<{
    id: string;
    displayName?: string | null;
    originalFilename?: string | null;
    folderId?: string | null;
    currentRevisionNumber: number;
  }>;
}
export interface WorkspaceAssetRevision { id: string; assetReferenceId: string; assetContentId: string; revisionNumber: number; createdByUserId: string; changeNote?: string | null; createdAt: string; }
export interface WorkspaceProjectTeam { id: string; teamId: string; teamName: string; teamSlug: string; role: string | number; participationMode: string | number; permissions: string[]; isActive: boolean; assignedAt: string; endedAt?: string | null; }
export interface WorkspaceProjectAllocation { id: string; projectTeamId: string; userId: string; function: string; capacityPercentage: number; startsAt: string; endsAt?: string | null; isActive: boolean; }
export interface WorkspaceProjectAgreement { id: string; proposingTeamId: string; receivingTeamId: string; proposedByUserId: string; acceptedByUserId?: string | null; status: string | number; scope: string; deliverables: string; startsAt: string; endsAt: string; revision: number; }
export interface WorkspaceProjectOwnership { projectId: string; teams: WorkspaceProjectTeam[]; allocations: WorkspaceProjectAllocation[]; agreements: WorkspaceProjectAgreement[]; }
export interface WorkspaceProjectCollaborator { id: string; userId: string; userName: string; role: string; permissions: string; joinedAt: string; isActive: boolean; }

async function client() {
  const session = await auth().catch(() => null);
  return createServerClient({
    baseUrl: process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080',
    auth: { getAccessToken: () => getToken() },
    tenant: { getTenantId: async () => session?.tenantId ?? null },
  });
}

async function get<T>(path: string): Promise<T | null> {
  const result = await (await client()).request<T>({ method: 'GET', path, requiresAuth: true, cache: 'no-store' });
  return result.ok ? result.data : null;
}

export const getWorkspaceTeams = cache(async () => (await get<WorkspaceTeam[]>('/v1/teams')) ?? []);
export const getWorkspaceTeam = cache(async (slug: string) => (await getWorkspaceTeams()).find((team) => team.slug === slug) ?? null);
export const getWorkspaceTeamProjects = cache(async (teamId: string) => (await get<WorkspaceTeamProject[]>(`/v1/teams/${teamId}/projects`)) ?? []);
export const getWorkspaceTeamInvitations = cache(async (teamId: string) => (await get<WorkspaceTeamInvitation[]>(`/v1/teams/${teamId}/invitations`)) ?? []);
export const getWorkspaceMyTeamInvitations = cache(async () => (await get<WorkspaceMyTeamInvitation[]>('/v1/teams/my-invitations')) ?? []);
export const getWorkspaceProject = cache(async (slug: string) => get<WorkspaceProject>(`/v1/projects/slug/${encodeURIComponent(slug)}`));
export const getWorkspaceProjectVersions = cache(async (projectId: string) => (await get<WorkspaceProjectVersion[]>(`/v1/projects/${projectId}/versions`)) ?? []);
export const getWorkspaceProjectOwnership = cache(async (projectId: string) => get<WorkspaceProjectOwnership>(`/v1/projects/${projectId}/ownership`));
export const getWorkspaceProjectCollaborators = cache(async (projectId: string) => (await get<WorkspaceProjectCollaborator[]>(`/v1/projects/${projectId}/collaborators`)) ?? []);
export const getWorkspaceProjectBoard = cache(async (projectId: string) => get<WorkspaceBoard>(`/v1/projects/${projectId}/work`));
export const getWorkspaceProjectTask = cache(async (projectId: string, taskId: string) => get<WorkspaceTaskDetails>(`/v1/projects/${projectId}/work/tasks/${taskId}`));
export const getWorkspaceProjectMilestones = cache(async (projectId: string) => (await get<WorkspaceMilestone[]>(`/v1/projects/${projectId}/work/milestones`)) ?? []);
export const getWorkspaceProjectLabels = cache(async (projectId: string) => (await get<WorkspaceTaskLabel[]>(`/v1/projects/${projectId}/work/labels`)) ?? []);
export const getWorkspaceProjectWorkHistory = cache(async (projectId: string) => (await get<WorkspaceWorkHistory[]>(`/v1/projects/${projectId}/work/history?take=50`)) ?? []);
export const getWorkspaceLibrary = cache(async (resourceType: 'Team' | 'Project', resourceId: string) => get<WorkspaceLibrary>(`/v1/asset-libraries/${resourceType}/${resourceId}`));
export const getWorkspaceAssetRevisions = cache(async (referenceId: string) => (await get<WorkspaceAssetRevision[]>(`/v1/asset-libraries/assets/${referenceId}/revisions`)) ?? []);
