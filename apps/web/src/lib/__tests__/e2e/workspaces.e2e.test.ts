import { createClient, type ApiError, type Result } from '@game-guild/client';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';

interface AuthOutput {
  accessToken: string;
  refreshToken: string;
  userId: string;
  tenantId?: string;
  user?: { id?: string };
}

interface Identified { id: string; }
interface TeamOutput extends Identified { slug: string; name: string; members: Array<{ userId: string; authority: string | number }>; }
interface ProjectOutput extends Identified { slug?: string | null; title: string; }
interface OwnershipOutput {
  teams: Array<{ id: string; teamId: string; role: string | number }>;
  allocations: Array<{ id: string; userId: string; isActive: boolean }>;
  agreements: Array<{ id: string; status: string | number; acceptedByUserId?: string | null }>;
}
interface BoardOutput { columns: Array<{ id: string; name: string; kind: string | number; tasks: TaskOutput[] }>; }
interface TaskOutput extends Identified { title: string; status: string | number; columnId: string; }
interface FolderOutput extends Identified { name: string; restrictionMode: string | number; }
interface AssetOutput extends Identified { displayName?: string | null; folderId?: string | null; }
interface LibraryOutput { folders: FolderOutput[]; assets: AssetOutput[]; }
interface DashboardContextsOutput {
  contexts: Array<{ type: string; id?: string | null }>;
  capabilities: string[];
  navigation: Array<{ title: string; items: Array<{ title: string; route?: string | null; children: Array<{ route?: string | null }> }> }>;
}

const BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:8080';
const PASSWORD = 'Str0ng!Passw0rd123!';
const unique = () => `${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;

function unwrap<T>(result: Result<T, ApiError>, label: string): T {
  if (result.ok) return result.data;
  throw new Error(`${label} failed: ${result.error?.message ?? 'Unknown'} (${result.error?.status}) ${JSON.stringify(result.error)}`);
}

function actorClient(accessToken: string, tenantId?: string) {
  return createClient({
    baseUrl: BASE_URL,
    timeout: 20_000,
    devtools: { enabled: false },
    auth: { getAccessToken: async () => accessToken },
    ...(tenantId ? { tenant: { getTenantId: async () => tenantId } } : {}),
  });
}

describe('Teams, Project ownership, Project Work, files, and dashboard contexts E2E', () => {
  const anonymous = createClient({ baseUrl: BASE_URL, timeout: 20_000, devtools: { enabled: false } });
  const tag = unique();
  const ownerEmail = `workspace_owner_${tag}@example.com`;
  const memberEmail = `workspace_member_${tag}@example.com`;
  let ownerToken = '';
  let ownerId = '';
  let memberToken = '';
  let memberId = '';
  let memberDefaultToken = '';
  let tenantId = '';
  let projectId = '';
  let owner: ReturnType<typeof actorClient>;
  let member: ReturnType<typeof actorClient>;
  let ownerTeam: TeamOutput;
  let participantTeam: TeamOutput;

  beforeAll(async () => {
    const ownerSignup = unwrap(await anonymous.request<AuthOutput>({
      method: 'POST', path: '/v1/auth/sign-up', requiresAuth: false,
      body: { username: `workspace_owner_${tag}`, email: ownerEmail, password: PASSWORD },
    }), 'Workspace owner sign-up');
    ownerId = ownerSignup.userId || ownerSignup.user?.id || '';
    const bootstrap = actorClient(ownerSignup.accessToken, ownerSignup.tenantId);
    const tenant = unwrap(await bootstrap.request<Identified>({
      method: 'POST', path: '/v1/tenants', requiresAuth: true,
      body: { name: `Workspace E2E ${tag}`, slug: `workspace-e2e-${tag.replaceAll('_', '-')}`, adminEmail: ownerEmail },
    }), 'Create Workspace E2E tenant');
    tenantId = tenant.id;
    const ownerAuth = unwrap(await anonymous.request<AuthOutput>({
      method: 'POST', path: '/v1/auth/sign-in', requiresAuth: false,
      body: { email: ownerEmail, password: PASSWORD, tenantId },
    }), 'Workspace owner tenant sign-in');
    ownerToken = ownerAuth.accessToken;
    owner = actorClient(ownerToken, tenantId);

    const memberSignup = unwrap(await anonymous.request<AuthOutput>({
      method: 'POST', path: '/v1/auth/sign-up', requiresAuth: false,
      body: { username: `workspace_member_${tag}`, email: memberEmail, password: PASSWORD },
    }), 'Workspace member sign-up');
    memberDefaultToken = memberSignup.accessToken;
    memberId = memberSignup.userId || memberSignup.user?.id || '';
    unwrap(await owner.request<unknown>({
      method: 'POST', path: `/v1/users/${memberId}/memberships`, requiresAuth: true,
      body: { tenantId, role: 'Member', invitedByEmail: ownerEmail },
    }), 'Add Workspace member to tenant');
    const memberAuth = unwrap(await anonymous.request<AuthOutput>({
      method: 'POST', path: '/v1/auth/sign-in', requiresAuth: false,
      body: { email: memberEmail, password: PASSWORD, tenantId },
    }), 'Workspace member tenant sign-in');
    memberToken = memberAuth.accessToken;
    member = actorClient(memberToken, tenantId);
  }, 60_000);

  afterAll(async () => {
    if (!owner) return;
    if (projectId) {
      const removal = await owner.request<unknown>({
        method: 'DELETE', path: `/v1/projects/${projectId}?softDelete=true&reason=Workspace%20E2E%20fixture%20cleanup`, requiresAuth: true,
      });
      if (!removal.ok && removal.error?.status !== 404)
        throw new Error(`Workspace Project cleanup failed (${removal.error?.status}): ${removal.error?.message}`);
    }
    const cleanup = await owner.request<unknown>({
      method: 'DELETE', path: `/v1/tenants/${tenantId}`, requiresAuth: true,
      body: { reason: 'Workspace E2E fixture cleanup.' },
    });
    if (!cleanup.ok && cleanup.error?.status !== 404)
      throw new Error(`Workspace tenant cleanup failed (${cleanup.error?.status}): ${cleanup.error?.message}`);
  }, 30_000);

  it('creates Teams and accepts a single-use invitation in the correct tenant', async () => {
    ownerTeam = unwrap(await owner.request<TeamOutput>({
      method: 'POST', path: '/v1/teams', requiresAuth: true,
      body: { name: `Owner Team ${tag}`, slug: `owner-team-${tag.replaceAll('_', '-')}`, visibility: 'Private', description: 'Owner Team' },
    }), 'Create owner Team');
    participantTeam = unwrap(await member.request<TeamOutput>({
      method: 'POST', path: '/v1/teams', requiresAuth: true,
      body: { name: `Participant Team ${tag}`, slug: `participant-team-${tag.replaceAll('_', '-')}`, visibility: 'Private', description: 'Participating Team' },
    }), 'Create participant Team');

    const invitation = unwrap(await owner.request<{ id: string }>({
      method: 'POST', path: `/v1/teams/${ownerTeam.id}/invitations`, requiresAuth: true,
      body: { userId: memberId, authority: 'Member', expiresAt: new Date(Date.now() + 60 * 60_000).toISOString() },
    }), 'Invite Team member');
    const accepted = unwrap(await member.request<TeamOutput>({
      method: 'POST', path: `/v1/teams/invitations/${invitation.id}:accept`, body: {}, requiresAuth: true,
    }), 'Accept Team invitation');
    expect(accepted.members).toEqual(expect.arrayContaining([expect.objectContaining({ userId: memberId })]));

    const reused = await member.request<TeamOutput>({
      method: 'POST', path: `/v1/teams/invitations/${invitation.id}:accept`, body: {}, requiresAuth: true,
    });
    expect(reused.ok).toBe(false);
    if (!reused.ok) expect(reused.error?.status).toBe(409);
  });

  it('binds a Project to its Owner Team, participating Team, allocation, and two-actor agreement', async () => {
    const project = unwrap(await owner.request<ProjectOutput>({
      method: 'POST', path: '/v1/projects', requiresAuth: true,
      body: { title: `Workspace Project ${tag}`, description: 'Team-owned Project', type: 0, visibility: 'Private', status: 'Draft', ownerTeamId: ownerTeam.id },
    }), 'Create Team-owned Project');
    projectId = project.id;
    let ownership = unwrap(await owner.request<OwnershipOutput>({
      method: 'GET', path: `/v1/projects/${projectId}/ownership`, requiresAuth: true,
    }), 'Read Project ownership');
    const ownerLink = ownership.teams.find((team) => team.teamId === ownerTeam.id);
    expect(ownerLink).toBeTruthy();

    const participantLink = unwrap(await owner.request<{ id: string }>({
      method: 'POST', path: `/v1/projects/${projectId}/ownership/teams`, requiresAuth: true,
      body: { teamId: participantTeam.id, role: 'Contributor', participationMode: 'SelectedMembers', permissions: ['Read'], contributionPercentage: 25 },
    }), 'Add participating Team');
    unwrap(await owner.request<unknown>({
      method: 'POST', path: `/v1/projects/${projectId}/ownership/allocations`, requiresAuth: true,
      body: { projectTeamId: participantLink.id, userId: memberId, function: 'Gameplay engineer', capacityPercentage: 50, startsAt: new Date(Date.now() - 60_000).toISOString() },
    }), 'Allocate participating Team member');
    const agreement = unwrap(await owner.request<{ id: string }>({
      method: 'POST', path: `/v1/projects/${projectId}/ownership/agreements`, requiresAuth: true,
      body: { proposingTeamId: ownerTeam.id, receivingTeamId: participantTeam.id, scope: 'Testing build delivery', deliverables: 'Playable build and feedback response', startsAt: new Date().toISOString(), endsAt: new Date(Date.now() + 7 * 86_400_000).toISOString() },
    }), 'Propose Project Team agreement');
    const accepted = unwrap(await member.request<{ status: string | number; acceptedByUserId?: string | null }>({
      method: 'POST', path: `/v1/projects/${projectId}/ownership/agreements/${agreement.id}/accept`, body: {}, requiresAuth: true,
    }), 'Accept agreement as distinct actor');
    expect(accepted.acceptedByUserId).toBe(memberId);
    ownership = unwrap(await owner.request<OwnershipOutput>({
      method: 'GET', path: `/v1/projects/${projectId}/ownership`, requiresAuth: true,
    }), 'Read updated ownership');
    expect(ownership.allocations).toEqual(expect.arrayContaining([expect.objectContaining({ userId: memberId, isActive: true })]));
  });

  it('enforces task assignment, cyclic dependencies, and blocked completion', async () => {
    const board = unwrap(await owner.request<BoardOutput>({
      method: 'GET', path: `/v1/projects/${projectId}/work`, requiresAuth: true,
    }), 'Create/read Project board');
    const backlog = board.columns.find((column) => String(column.kind).toLowerCase() === 'backlog') ?? board.columns[0];
    const done = board.columns.find((column) => String(column.kind).toLowerCase() === 'done') ?? board.columns.at(-1);
    if (!backlog || !done) throw new Error('Default Project board columns are unavailable.');
    const first = unwrap(await owner.request<TaskOutput>({
      method: 'POST', path: `/v1/projects/${projectId}/work/tasks`, requiresAuth: true,
      body: { columnId: backlog.id, title: 'Prepare build', priority: 'High', assigneeUserId: memberId },
    }), 'Create allocated task');
    const prerequisite = unwrap(await owner.request<TaskOutput>({
      method: 'POST', path: `/v1/projects/${projectId}/work/tasks`, requiresAuth: true,
      body: { columnId: backlog.id, title: 'Stabilize build', priority: 'Normal', assigneeUserId: memberId },
    }), 'Create prerequisite task');
    unwrap(await owner.request<unknown>({
      method: 'POST', path: `/v1/projects/${projectId}/work/tasks/${first.id}/dependencies`, requiresAuth: true,
      body: { dependsOnTaskId: prerequisite.id },
    }), 'Add task dependency');
    const cycle = await owner.request<unknown>({
      method: 'POST', path: `/v1/projects/${projectId}/work/tasks/${prerequisite.id}/dependencies`, requiresAuth: true,
      body: { dependsOnTaskId: first.id },
    });
    expect(cycle.ok).toBe(false);
    if (!cycle.ok) expect(cycle.error?.status).toBe(409);
    const blocked = await owner.request<TaskOutput>({
      method: 'PUT', path: `/v1/projects/${projectId}/work/tasks/${first.id}/move`, requiresAuth: true,
      body: { columnId: done.id, position: 0 },
    });
    expect(blocked.ok).toBe(false);
    if (!blocked.ok) expect(blocked.error?.status).toBe(409);
    unwrap(await owner.request<TaskOutput>({
      method: 'PUT', path: `/v1/projects/${projectId}/work/tasks/${prerequisite.id}/move`, requiresAuth: true,
      body: { columnId: done.id, position: 0 },
    }), 'Complete prerequisite');
    const completed = unwrap(await owner.request<TaskOutput>({
      method: 'PUT', path: `/v1/projects/${projectId}/work/tasks/${first.id}/move`, requiresAuth: true,
      body: { columnId: done.id, position: 1 },
    }), 'Complete unblocked task');
    expect(String(completed.status).toLowerCase()).toBe('done');
  });

  it('stores deduplicated Project files and applies folder restrictions to copy and revisions', async () => {
    const folder = unwrap(await owner.request<FolderOutput>({
      method: 'POST', path: `/v1/asset-libraries/Project/${projectId}/folders`, requiresAuth: true,
      body: { name: 'Restricted builds', parentFolderId: null },
    }), 'Create Project asset folder');
    const form = new FormData();
    form.set('file', new Blob(['workspace-e2e-build'], { type: 'text/plain' }), 'build.txt');
    const query = new URLSearchParams({ accessPolicy: 'Inherited', parentResourceType: 'Project', parentResourceId: projectId, folderId: folder.id });
    const upload = await fetch(`${BASE_URL}/v1/assets?${query}`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${ownerToken}`, 'X-Tenant-Id': tenantId },
      body: form,
    });
    expect(upload.status).toBe(201);
    const uploadBody = await upload.json() as { assetReferenceId: string };
    const referenceId = uploadBody.assetReferenceId;
    expect(referenceId).toBeTruthy();
    const initialLibrary = unwrap(await owner.request<LibraryOutput>({
      method: 'GET', path: `/v1/asset-libraries/Project/${projectId}`, requiresAuth: true,
    }), 'Read Project library');
    expect(initialLibrary.assets).toEqual(expect.arrayContaining([expect.objectContaining({ id: referenceId })]));

    unwrap(await owner.request<FolderOutput>({
      method: 'PUT', path: `/v1/asset-libraries/folders/${folder.id}/restriction`, requiresAuth: true,
      body: { mode: 'SelectedTeams', teamIds: [participantTeam.id], authorities: [] },
    }), 'Restrict build folder to participant Team');
    const deniedCopy = await owner.request<AssetOutput>({
      method: 'POST', path: `/v1/asset-libraries/assets/${referenceId}/copy`, requiresAuth: true,
      body: { displayName: 'Forbidden owner copy', folderId: null },
    });
    expect(deniedCopy.ok).toBe(false);
    if (!deniedCopy.ok) expect(deniedCopy.error?.status).toBe(404);

    unwrap(await owner.request<FolderOutput>({
      method: 'PUT', path: `/v1/asset-libraries/folders/${folder.id}/restriction`, requiresAuth: true,
      body: { mode: 'None', teamIds: [], authorities: [] },
    }), 'Remove build folder restriction');
    const revisions = unwrap(await owner.request<Array<{ id: string }>>({
      method: 'GET', path: `/v1/asset-libraries/assets/${referenceId}/revisions`, requiresAuth: true,
    }), 'Read Project file revisions');
    expect(revisions.length).toBeGreaterThan(0);
    const copy = unwrap(await owner.request<AssetOutput>({
      method: 'POST', path: `/v1/asset-libraries/assets/${referenceId}/copy`, requiresAuth: true,
      body: { displayName: 'Deduplicated build copy', folderId: null },
    }), 'Copy deduplicated Project file');
    expect(copy.id).not.toBe(referenceId);
    unwrap(await owner.request<unknown>({
      method: 'POST', path: `/v1/asset-libraries/assets/${referenceId}/revisions/${revisions[0]!.id}/restore`, body: {}, requiresAuth: true,
    }), 'Restore Project file revision');
  });

  it('returns contextual Team/Project workspaces without administrative Operations for a common member', async () => {
    const contexts = unwrap(await member.request<DashboardContextsOutput>({
      method: 'GET', path: '/v1/dashboard/contexts', requiresAuth: true,
    }), 'Read member dashboard contexts');
    expect(contexts.contexts).toEqual(expect.arrayContaining([
      expect.objectContaining({ type: 'Team', id: ownerTeam.id }),
      expect.objectContaining({ type: 'Project', id: projectId }),
    ]));
    expect(contexts.contexts.some((context) => context.type === 'Operations')).toBe(false);
    const memberRoutes = contexts.navigation
      .flatMap((group) => group.items)
      .flatMap((item) => [item.route, ...item.children.map((child) => child.route)])
      .filter((route): route is string => Boolean(route));
    expect(memberRoutes.some((route) => route.includes('/testing-lab') || route.includes('/launch-pad'))).toBe(false);

    const wrongTenant = actorClient(memberDefaultToken);
    const hidden = await wrongTenant.request<ProjectOutput>({
      method: 'GET', path: `/v1/projects/${projectId}`, requiresAuth: true,
    });
    expect(hidden.ok).toBe(false);
    if (!hidden.ok) expect(hidden.error?.status).toBe(404);
  });
});
