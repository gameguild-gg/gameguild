import { createClient, type ApiError, type Result } from '@game-guild/client';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';

interface SignInOutput {
  accessToken: string;
  refreshToken: string;
  userId: string;
  user?: { id: string };
}

interface ProjectOutput {
  id: string;
  title: string;
  slug?: string | null;
  status?: number | string | null;
  visibility?: number | string | null;
  publishedAt?: string | null;
}

interface LaunchChecklistItemOutput {
  id: string;
  title: string;
  category: string;
  isComplete: boolean;
}

interface LaunchPlanOutput {
  id: string;
  projectId: string;
  name: string;
  positioning?: string | null;
  targetLaunchAt?: string | null;
  launchedAt?: string | null;
  status: number | string;
  readinessPercent?: number;
  channels?: string[] | null;
  checklistItems?: LaunchChecklistItemOutput[] | null;
}

interface LaunchPadEventOutput { id: string; status: string | number; name: string; }
interface LaunchPadSlotOutput { id: string; eventId: string; reservedCount: number; capacity: number; }
interface LaunchPadApplicationOutput { id: string; projectId: string; status: string | number; }
interface LaunchPadRegistrationOutput { id: string; status: string | number; userId: string; }

const BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:8080';
const TENANT_ID = process.env.API_TENANT_ID ?? process.env.TENANT_ID ?? undefined;

const unwrap = <T>(result: Result<T, ApiError>, label: string): T => {
  if (result.ok) return result.data;
  throw new Error(`${label} failed: ${result.error?.message ?? 'Unknown'} (${result.error?.status})`);
};

const unique = () => `${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
const launchStatus = (status: number | string | null | undefined) =>
  typeof status === 'number' ? ['Draft', 'Preparing', 'Ready', 'Launched', 'Paused'][status] : status;
const contentStatus = (status: number | string | null | undefined) =>
  typeof status === 'number' ? ['Draft', 'Review', 'Published', 'Archived', 'Deleted'][status] : status;
const contentVisibility = (visibility: number | string | null | undefined) =>
  typeof visibility === 'number' ? ['Private', 'Internal', 'Friends', 'Protected', 'Public'][visibility] : visibility;

describe('Launch Pad E2E — project release workflow', () => {
  let accessToken: string;
  let email: string;
  let password: string;
  let tenantId: string | undefined = TENANT_ID;
  let authedClient: ReturnType<typeof createClient>;
  let project: ProjectOutput;
  let createdTenant = false;

  beforeAll(async () => {
    const client = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
    });

    const tag = unique();
    email = `launch_pad_e2e_${tag}@example.com`;
    password = 'Str0ng!Passw0rd123!';

    const signUpResult = await client.request<SignInOutput>({
      method: 'POST',
      path: '/v1/auth/sign-up',
      body: {
        username: `launch_pad_e2e_${tag}`,
        email,
        password,
        ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
      },
      requiresAuth: false,
    });

    accessToken = unwrap(signUpResult, 'Launch Pad E2E sign-up').accessToken;

    if (!TENANT_ID) {
      const tenantClient = createClient({
        baseUrl: BASE_URL,
        timeout: 15_000,
        devtools: { enabled: false },
        auth: { getAccessToken: async () => accessToken },
      });

      const tenantResult = await tenantClient.request<{ id: string }>({
        method: 'POST',
        path: '/v1/tenants',
        body: {
          name: `Launch Pad E2E Tenant ${tag}`,
          slug: `launch-pad-e2e-${tag.replace(/_/g, '-')}`,
          adminEmail: email,
          description: 'Tenant created for Launch Pad E2E coverage',
        },
        requiresAuth: true,
      });

      tenantId = unwrap(tenantResult, 'Create Launch Pad E2E tenant').id;
      createdTenant = true;
      const signInResult = await client.request<SignInOutput>({
        method: 'POST',
        path: '/v1/auth/sign-in',
        body: {
          email,
          password,
          tenantId,
        },
        requiresAuth: false,
      });

      accessToken = unwrap(signInResult, 'Launch Pad tenant-owner sign-in').accessToken;
    }

    authedClient = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
      auth: { getAccessToken: async () => accessToken },
      tenant: { getTenantId: async () => tenantId },
    });
  }, 60_000);

  afterAll(async () => {
    if (!createdTenant || !tenantId || !authedClient) return;
    const result = await authedClient.request<unknown>({
      method: 'DELETE',
      path: `/v1/tenants/${tenantId}`,
      body: { reason: 'Launch Pad E2E fixture cleanup.' },
      requiresAuth: true,
    });
    if (!result.ok && result.error?.status !== 404)
      throw new Error(`Launch Pad E2E cleanup failed: ${result.error?.message ?? 'Unknown'} (${result.error?.status})`);
  });

  it('creates a draft project for launch planning', async () => {
    const tag = unique();
    project = unwrap(
      await authedClient.request<ProjectOutput>({
        method: 'POST',
        path: '/v1/projects',
        body: {
          title: `Launch Pad E2E Project ${tag}`,
          description: 'Project created to validate the launch readiness workflow.',
          shortDescription: 'Launch workflow project',
          imageUrl: 'https://example.com/launch-project.jpg',
          websiteUrl: 'https://example.com/launch-project',
          downloadUrl: 'https://example.com/downloads/launch-project.zip',
          type: 0,
          visibility: 0,
          status: 0,
          tags: ['launch-pad', 'e2e'],
        },
        requiresAuth: true,
      }),
      'Create Launch Pad project',
    );

    expect(project.id).toBeTruthy();
    expect(contentStatus(project.status)).toBe('Draft');
    expect(contentVisibility(project.visibility)).toBe('Private');
  });

  it('requires an approved event application before creating a launch plan', async () => {
    const directCreation = await authedClient.request<LaunchPlanOutput>({
      method: 'POST',
      path: '/v1/launch-pad',
      body: { projectId: project.id, name: 'Direct launch plan is forbidden' },
      requiresAuth: true,
    });

    expect(directCreation.ok).toBe(false);
    if (!directCreation.ok) expect(directCreation.error?.status).toBe(409);
  });

  it('separates Launch Pad event management, Project application, approval, and individual participation', async () => {
    const tag = unique();
    const participantEmail = `launch_pad_applicant_${tag}@example.com`;
    const participantPassword = 'Str0ng!Passw0rd123!';
    const anonymous = createClient({ baseUrl: BASE_URL, timeout: 15_000, devtools: { enabled: false } });
    const signUp = unwrap(await anonymous.request<SignInOutput>({
      method: 'POST',
      path: '/v1/auth/sign-up',
      body: { username: `launch_pad_applicant_${tag}`, email: participantEmail, password: participantPassword },
      requiresAuth: false,
    }), 'Launch Pad applicant sign-up');
    const applicantDefaultTenant = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
      auth: { getAccessToken: async () => signUp.accessToken },
    });
    const participantId = signUp.userId || signUp.user?.id;
    if (!participantId || !tenantId) throw new Error('Launch Pad applicant or tenant identity is unavailable.');

    unwrap(await authedClient.request<unknown>({
      method: 'POST',
      path: `/v1/users/${participantId}/memberships`,
      body: { tenantId, role: 'Member', invitedByEmail: email },
      requiresAuth: true,
    }), 'Add Launch Pad applicant to tenant');
    const participantAuth = unwrap(await anonymous.request<SignInOutput>({
      method: 'POST',
      path: '/v1/auth/sign-in',
      body: { email: participantEmail, password: participantPassword, tenantId },
      requiresAuth: false,
    }), 'Launch Pad applicant tenant sign-in');
    const participant = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
      auth: { getAccessToken: async () => participantAuth.accessToken },
      tenant: { getTenantId: async () => tenantId! },
    });

    const submittedProject = unwrap(await participant.request<ProjectOutput>({
      method: 'POST', path: '/v1/projects', requiresAuth: true,
      body: { title: `Launch event project ${tag}`, description: 'Applicant-owned event project.', type: 0, visibility: 0, status: 0, tags: ['launch-pad-event'] },
    }), 'Create applicant Project');
    const version = unwrap(await participant.request<{ id: string }>({
      method: 'POST', path: `/v1/projects/${submittedProject.id}/versions`, requiresAuth: true,
      body: { versionNumber: '1.0.0-event-e2e', status: 'ready', releaseNotes: 'Applicant event build.' },
    }), 'Create applicant ProjectVersion');

    const now = Date.now();
    const launchEvent = unwrap(await authedClient.request<LaunchPadEventOutput>({
      method: 'POST', path: '/v1/launch-pad/events', requiresAuth: true,
      body: {
        name: `Launch showcase ${tag}`,
        description: 'Management and participation are separate.',
        startsAt: new Date(now + 2 * 60 * 60_000).toISOString(),
        endsAt: new Date(now + 5 * 60 * 60_000).toISOString(),
        applicationsOpenAt: new Date(now - 60_000).toISOString(),
        applicationsCloseAt: new Date(now + 60 * 60_000).toISOString(),
      },
    }), 'Create Launch Pad event');
    const slot = unwrap(await authedClient.request<LaunchPadSlotOutput>({
      method: 'POST', path: `/v1/launch-pad/events/${launchEvent.id}/slots`, requiresAuth: true,
      body: { name: 'Presenter stage', role: 'Presenter', capacity: 5, startsAt: new Date(now + 2 * 60 * 60_000).toISOString(), endsAt: new Date(now + 4 * 60 * 60_000).toISOString() },
    }), 'Create Launch Pad participant slot');
    unwrap(await authedClient.request<LaunchPadEventOutput>({
      method: 'POST', path: `/v1/launch-pad/events/${launchEvent.id}:transition`, body: { status: 'ApplicationsOpen' }, requiresAuth: true,
    }), 'Open Launch Pad applications');

    const unauthorizedManagement = await participant.request<LaunchPadEventOutput[]>({
      method: 'GET', path: '/v1/launch-pad/events/management', requiresAuth: true,
    });
    expect(unauthorizedManagement.ok).toBe(false);
    if (!unauthorizedManagement.ok) expect(unauthorizedManagement.error?.status).toBe(403);

    const application = unwrap(await participant.request<LaunchPadApplicationOutput>({
      method: 'POST', path: `/v1/launch-pad/events/${launchEvent.id}/applications`, requiresAuth: true,
      body: { projectId: submittedProject.id, projectVersionId: version.id, pitch: 'A Team-owned release candidate.' },
    }), 'Submit Project to Launch Pad event');
    expect(application.projectId).toBe(submittedProject.id);

    unwrap(await authedClient.request<LaunchPadApplicationOutput>({
      method: 'POST', path: `/v1/launch-pad/events/applications/${application.id}:review`, body: { status: 'UnderReview' }, requiresAuth: true,
    }), 'Start Launch Pad application review');
    const approvedApplication = unwrap(await authedClient.request<LaunchPadApplicationOutput>({
      method: 'POST', path: `/v1/launch-pad/events/applications/${application.id}:review`, body: { status: 'Approved', launchPlanName: 'Approved event launch' }, requiresAuth: true,
    }), 'Approve Launch Pad application');
    expect(approvedApplication.status).toBe('Approved');
    const generatedPlan = unwrap(await authedClient.request<LaunchPlanOutput>({
      method: 'GET', path: `/v1/launch-pad/projects/${submittedProject.id}`, requiresAuth: true,
    }), 'Read LaunchPlan created by approval');
    expect(generatedPlan.projectId).toBe(submittedProject.id);
    expect(generatedPlan.checklistItems?.length).toBeGreaterThan(0);

    const prematurePublish = await authedClient.request<LaunchPlanOutput>({
      method: 'POST', path: `/v1/launch-pad/${generatedPlan.id}:publish`, body: {}, requiresAuth: true,
    });
    expect(prematurePublish.ok).toBe(false);
    if (!prematurePublish.ok) expect(prematurePublish.error?.status).toBe(400);

    let currentPlan = generatedPlan;
    for (const item of generatedPlan.checklistItems?.filter((candidate) => !candidate.isComplete) ?? []) {
      currentPlan = unwrap(await authedClient.request<LaunchPlanOutput>({
        method: 'POST', path: `/v1/launch-pad/${generatedPlan.id}/checklist/${item.id}:complete`, body: {}, requiresAuth: true,
      }), `Complete Launch Pad checklist item ${item.title}`);
    }
    expect(currentPlan.readinessPercent).toBe(100);
    expect(launchStatus(currentPlan.status)).toBe('Ready');

    const published = unwrap(await authedClient.request<LaunchPlanOutput>({
      method: 'POST', path: `/v1/launch-pad/${generatedPlan.id}:publish`, body: {}, requiresAuth: true,
    }), 'Publish approved Launch Pad plan');
    expect(launchStatus(published.status)).toBe('Launched');
    expect(published.launchedAt).toBeTruthy();

    unwrap(await authedClient.request<LaunchPadEventOutput>({
      method: 'POST', path: `/v1/launch-pad/events/${launchEvent.id}:transition`, body: { status: 'ApplicationsClosed' }, requiresAuth: true,
    }), 'Close Launch Pad applications');
    unwrap(await authedClient.request<LaunchPadEventOutput>({
      method: 'POST', path: `/v1/launch-pad/events/${launchEvent.id}:transition`, body: { status: 'Scheduled' }, requiresAuth: true,
    }), 'Schedule Launch Pad event');
    const registration = unwrap(await participant.request<LaunchPadRegistrationOutput>({
      method: 'POST', path: `/v1/launch-pad/events/slots/${slot.id}/registrations`, requiresAuth: true,
    }), 'Register individual Launch Pad participant');
    expect(registration.userId).toBe(participantId);
    if (createdTenant) {
      const crossTenantCancellation = await applicantDefaultTenant.request<LaunchPadRegistrationOutput>({
        method: 'POST',
        path: `/v1/launch-pad/events/registrations/${registration.id}:cancel`,
        requiresAuth: true,
      });
      expect(crossTenantCancellation.ok).toBe(false);
      if (!crossTenantCancellation.ok) expect(crossTenantCancellation.error?.status).toBe(404);
    }
    for (const status of ['CheckedIn', 'Attended', 'Completed']) {
      unwrap(await authedClient.request<LaunchPadRegistrationOutput>({
        method: 'POST', path: `/v1/launch-pad/events/registrations/${registration.id}:transition`, body: { status }, requiresAuth: true,
      }), `Transition Launch Pad participant to ${status}`);
    }
    const ownRegistrations = unwrap(await participant.request<LaunchPadRegistrationOutput[]>({
      method: 'GET', path: '/v1/launch-pad/events/registrations/me', requiresAuth: true,
    }), 'Read individual Launch Pad participation');
    expect(ownRegistrations).toEqual(expect.arrayContaining([expect.objectContaining({ id: registration.id, status: 'Completed' })]));
  }, 90_000);
});
