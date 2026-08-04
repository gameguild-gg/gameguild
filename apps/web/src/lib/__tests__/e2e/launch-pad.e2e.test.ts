import { createClient, type ApiError, type Result } from '@game-guild/client';
import { beforeAll, describe, expect, it } from 'vitest';

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
  let launchPlan: LaunchPlanOutput;

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
    });
  }, 60_000);

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

  it('creates a launch plan for the project and exposes it on the dashboard', async () => {
    launchPlan = unwrap(
      await authedClient.request<LaunchPlanOutput>({
        method: 'POST',
        path: '/v1/launch-pad',
        body: {
          projectId: project.id,
          name: 'Portfolio release launch',
          positioning: 'Release readiness plan for a student-facing game project.',
          targetLaunchAt: '2026-07-01T12:00:00.000Z',
          channels: ['Website', 'Steam', 'Newsletter', 'Steam'],
          checklistItems: [
            { title: 'Storefront approved', category: 'Storefront', isComplete: true, isRequired: true },
            { title: 'Release package tested', category: 'Quality', isComplete: false, isRequired: true },
          ],
        },
        requiresAuth: true,
      }),
      'Create Launch Pad plan',
    );

    expect(launchPlan.projectId).toBe(project.id);
    expect(launchStatus(launchPlan.status)).toBe('Preparing');
    expect(launchPlan.readinessPercent).toBe(50);
    expect(launchPlan.channels).toEqual(['newsletter', 'steam', 'website']);

    const dashboard = unwrap(
      await authedClient.request<LaunchPlanOutput[]>({
        method: 'GET',
        path: '/v1/launch-pad',
        requiresAuth: true,
      }),
      'Read Launch Pad dashboard',
    );
    expect(dashboard.some((plan) => plan.id === launchPlan.id)).toBe(true);

    const byProject = unwrap(
      await authedClient.request<LaunchPlanOutput>({
        method: 'GET',
        path: `/v1/launch-pad/projects/${project.id}`,
        requiresAuth: true,
      }),
      'Read Launch Pad plan by project',
    );
    expect(byProject.id).toBe(launchPlan.id);
  });

  it('blocks publish until all required checklist items are complete', async () => {
    const blocked = await authedClient.request<LaunchPlanOutput>({
      method: 'POST',
      path: `/v1/launch-pad/${launchPlan.id}:publish`,
      body: {},
      requiresAuth: true,
    });

    expect(blocked.ok).toBe(false);
    if (!blocked.ok) expect(blocked.error?.status).toBe(400);
  });

  it('completes checklist items and publishes the launch', async () => {
    const remainingItem = launchPlan.checklistItems?.find((item) => !item.isComplete);
    expect(remainingItem?.id).toBeTruthy();

    const readyPlan = unwrap(
      await authedClient.request<LaunchPlanOutput>({
        method: 'POST',
        path: `/v1/launch-pad/${launchPlan.id}/checklist/${remainingItem!.id}:complete`,
        body: {},
        requiresAuth: true,
      }),
      'Complete Launch Pad checklist item',
    );
    expect(readyPlan.readinessPercent).toBe(100);
    expect(launchStatus(readyPlan.status)).toBe('Ready');

    const published = unwrap(
      await authedClient.request<LaunchPlanOutput>({
        method: 'POST',
        path: `/v1/launch-pad/${launchPlan.id}:publish`,
        body: {},
        requiresAuth: true,
      }),
      'Publish Launch Pad plan',
    );

    expect(launchStatus(published.status)).toBe('Launched');
    expect(published.launchedAt).toBeTruthy();

    const publishedProject = unwrap(
      await authedClient.request<ProjectOutput>({
        method: 'GET',
        path: `/v1/projects/${project.id}`,
        requiresAuth: false,
      }),
      'Read published Launch Pad project',
    );

    expect(publishedProject.id).toBe(project.id);

    const filteredPublishedProjects = unwrap(
      await authedClient.request<ProjectOutput[]>({
        method: 'GET',
        path: `/v1/projects?status=2&visibility=4&searchTerm=${encodeURIComponent(project.title)}&take=10`,
        requiresAuth: false,
      }),
      'Read published Launch Pad project through public filters',
    );

    expect(filteredPublishedProjects.some((candidate) => candidate.id === project.id)).toBe(true);
  });
});
