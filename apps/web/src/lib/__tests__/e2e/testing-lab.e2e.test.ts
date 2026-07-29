import { createClient, type ApiError, type Result } from '@game-guild/client';
import { beforeAll, describe, expect, it } from 'vitest';

interface SignInOutput {
  accessToken: string;
  refreshToken: string;
  userId: string;
  tenantId?: string;
  user?: { id: string };
}

interface TestingRequestOutput {
  id: string;
  title: string;
  status: number | string;
  maxTesters?: number | null;
  currentTesterCount?: number | null;
  projectVersion?: {
    id: string;
    projectId: string;
    versionNumber?: string | null;
    project?: { id: string; title?: string | null; slug?: string | null } | null;
  } | null;
}

interface ProjectOutput {
  id: string;
  title: string;
  slug?: string | null;
}

interface TestingLocationOutput {
  id: string;
  name: string;
  status: number | string;
}

interface TestingSessionOutput {
  id: string;
  testingRequestId: string;
  locationId: string;
  sessionName: string;
  status: number | string;
}

const BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:5295';
const TENANT_ID = process.env.API_TENANT_ID ?? process.env.TENANT_ID ?? undefined;

const unwrap = <T>(result: Result<T, ApiError>, label: string): T => {
  if (result.ok) return result.data;
  throw new Error(`${label} failed: ${result.error?.message ?? 'Unknown'} (${result.error?.status}) ${JSON.stringify(result.error)}`);
};

const unique = () => `${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
const statusName = (status: number | string, names: string[]) => (typeof status === 'number' ? names[status] : status);

describe('Testing Lab E2E — build submission and session scheduling', () => {
  let accessToken: string;
  let userId: string;
  let email: string;
  let password: string;
  let tenantId: string | undefined = TENANT_ID;
  let authedClient: ReturnType<typeof createClient>;
  let project: ProjectOutput;
  let testingRequest: TestingRequestOutput;
  let location: TestingLocationOutput;
  let session: TestingSessionOutput;

  beforeAll(async () => {
    const client = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
    });

    const tag = unique();
    email = `testing_lab_e2e_${tag}@example.com`;
    password = 'Str0ng!Passw0rd123!';

    const signUpResult = await client.request<SignInOutput>({
      method: 'POST',
      path: '/v1/auth/sign-up',
      body: {
        username: `testing_lab_e2e_${tag}`,
        email,
        password,
        ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
      },
      requiresAuth: false,
    });

    const signUp = unwrap(signUpResult, 'Testing Lab E2E sign-up');
    accessToken = signUp.accessToken;
    userId = signUp.userId || signUp.user?.id || '';

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
          name: `Testing Lab E2E Tenant ${tag}`,
          slug: `testing-lab-e2e-${tag.replace(/_/g, '-')}`,
          adminEmail: email,
          description: 'Tenant created for Testing Lab E2E coverage',
        },
        requiresAuth: true,
      });

      tenantId = unwrap(tenantResult, 'Create Testing Lab E2E tenant').id;
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

      const signIn = unwrap(signInResult, 'Testing Lab tenant-owner sign-in');
      accessToken = signIn.accessToken;
      tenantId = signIn.tenantId ?? tenantId;
      userId = signIn.userId || signIn.user?.id || userId;
    }

    authedClient = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
      auth: { getAccessToken: async () => accessToken },
      tenant: { getTenantId: async () => tenantId ?? null },
    });
  }, 60_000);

  it('creates a real project before requesting Testing Lab coverage', async () => {
    const tag = unique();
    project = unwrap(
      await authedClient.request<ProjectOutput>({
        method: 'POST',
        path: '/v1/projects',
        body: {
          title: `Testing Lab E2E Project ${tag}`,
          description: 'Project created before submitting a Testing Lab build.',
          shortDescription: 'Testing Lab project-backed submission',
          imageUrl: 'https://example.com/testing-lab-project.jpg',
          websiteUrl: 'https://example.com/testing-lab-project',
          downloadUrl: 'https://example.com/downloads/testing-lab-project.zip',
          type: 0,
          visibility: 4,
          status: 2,
          tags: ['testing-lab', 'e2e'],
        },
        requiresAuth: true,
      }),
      'Create Testing Lab project',
    );

    expect(project.id).toBeTruthy();
    expect(project.title).toContain('Testing Lab E2E Project');
  });

  it('submits a simple testing build', async () => {
    const tag = unique();
    const start = new Date(Date.now() + 24 * 60 * 60 * 1000);
    const end = new Date(Date.now() + 14 * 24 * 60 * 60 * 1000);

    testingRequest = unwrap(
      await authedClient.request<TestingRequestOutput>({
        method: 'POST',
        path: '/v1/testing/submit-simple',
        body: {
          title: `Testing Lab E2E Build ${tag}`,
          description: 'Build submitted for moderated playtesting coverage.',
          versionNumber: '0.1.0',
          downloadUrl: 'https://example.com/testing-build.zip',
          instructionsType: 0,
          instructionsContent: 'Install, play the tutorial, and report the first blocker.',
          feedbackFormContent: 'What confused you? What felt polished?',
          maxTesters: 8,
          startDate: start.toISOString(),
          endDate: end.toISOString(),
          projectId: project.id,
          teamIdentifier: project.title,
        },
        requiresAuth: true,
      }),
      'Submit simple Testing Lab request',
    );

    expect(testingRequest.id).toBeTruthy();
    expect(testingRequest.title).toContain('Testing Lab E2E Build');
    expect(testingRequest.projectVersion?.projectId).toBe(project.id);
    expect(statusName(testingRequest.status, ['Draft', 'Open', 'Active', 'InProgress', 'Paused', 'Completed', 'Cancelled'])).toBe('Draft');
  });

  it('lists the submitted request in the authenticated dashboard API', async () => {
    const requests = unwrap(
      await authedClient.request<TestingRequestOutput[]>({
        method: 'GET',
        path: '/v1/testing/my-requests',
        requiresAuth: true,
      }),
      'Read my Testing Lab requests',
    );

    expect(requests.some((request) => request.id === testingRequest.id)).toBe(true);
  });

  it('creates a testing location and scheduled session', async () => {
    const tag = unique();
    location = unwrap(
      await authedClient.request<TestingLocationOutput>({
        method: 'POST',
        path: '/v1/testing/locations',
        body: {
          name: `Remote UX Lab ${tag}`,
          description: 'Remote moderated testing room for E2E coverage.',
          address: 'Remote',
          maxTestersCapacity: 20,
          maxProjectsCapacity: 4,
          equipmentAvailable: 'Discord, screen share, capture notes',
          status: 0,
        },
        requiresAuth: true,
      }),
      'Create Testing Lab location',
    );

    const sessionDate = new Date(Date.now() + 3 * 24 * 60 * 60 * 1000);
    const startTime = new Date(sessionDate);
    startTime.setUTCHours(15, 0, 0, 0);
    const endTime = new Date(sessionDate);
    endTime.setUTCHours(17, 0, 0, 0);

    session = unwrap(
      await authedClient.request<TestingSessionOutput>({
        method: 'POST',
        path: '/v1/testing/sessions',
        body: {
          testingRequestId: testingRequest.id,
          locationId: location.id,
          sessionName: `Moderated E2E Lab ${tag}`,
          sessionDate: sessionDate.toISOString(),
          startTime: startTime.toISOString(),
          endTime: endTime.toISOString(),
          maxTesters: 8,
          maxProjects: 1,
          status: 0,
          managerId: userId,
          managerUserId: userId,
        },
        requiresAuth: true,
      }),
      'Create Testing Lab session',
    );

    expect(session.testingRequestId).toBe(testingRequest.id);
    expect(session.locationId).toBe(location.id);
    expect(statusName(session.status, ['Scheduled', 'Active', 'Completed', 'Cancelled'])).toBe('Scheduled');
  });

  it('exposes scheduled sessions through the public Testing Lab endpoint', async () => {
    const publicSessions = unwrap(
      await authedClient.request<TestingSessionOutput[]>({
        method: 'GET',
        path: '/v1/testing/public/sessions?take=50',
        requiresAuth: false,
      }),
      'Read public Testing Lab sessions',
    );

    expect(publicSessions.some((candidate) => candidate.id === session.id)).toBe(true);
  });
});
