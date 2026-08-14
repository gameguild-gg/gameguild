import { auth, getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type ApiError,
  type Result,
  type TestingLabPublicTestingEventProjection,
  type TestingLabTestingFeedbackObligationProjection,
  type TestingLabTestingProjectApplicationProjection,
  type TestingLabTestingSlotRegistrationProjection,
} from '@game-guild/client';

export interface PublicTestingEventsDirectory {
  events: TestingLabPublicTestingEventProjection[];
  accessIssues: string[];
}

export interface PublicTestingEventExperience {
  event: TestingLabPublicTestingEventProjection | null;
  applications: TestingLabTestingProjectApplicationProjection[];
  registrations: TestingLabTestingSlotRegistrationProjection[];
  feedbackObligations: TestingLabTestingFeedbackObligationProjection[];
  isAuthenticated: boolean;
  accessIssues: string[];
}

export interface PublicTestingEventsDirectoryOptions {
  skip?: number;
  take?: number;
}

export interface TestingParticipationOverview {
  applications: TestingLabTestingProjectApplicationProjection[];
  registrations: TestingLabTestingSlotRegistrationProjection[];
  feedbackObligations: TestingLabTestingFeedbackObligationProjection[];
  isAuthenticated: boolean;
  accessIssues: string[];
}

function createPublicModules() {
  const client = createServerClient({
    baseUrl: process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080',
    cache: 'no-store',
  });

  return {
    events: new GeneratedApi.TestinglabTestingeventsModule(client),
  };
}

function createAuthenticatedModules() {
  const client = createServerClient({
    baseUrl: process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080',
    auth: { getAccessToken: () => getToken() },
    tenant: { getTenantId: async () => (await auth().catch(() => null))?.tenantId ?? null },
  });

  return {
    events: new GeneratedApi.TestinglabTestingeventsModule(client),
    participation: new GeneratedApi.TestinglabTestingeventparticipationModule(client),
  };
}

async function read<T>(operation: Promise<Result<T, ApiError>>, label: string) {
  try {
    const result = await operation;
    if (result.ok) return { data: result.data, issue: null };
    return {
      data: null,
      issue: `${label} returned ${result.error.status ?? 'an error'}: ${result.error.message}`,
    };
  } catch (error) {
    return {
      data: null,
      issue: `${label} failed: ${error instanceof Error ? error.message : 'Unknown error'}`,
    };
  }
}

export async function getPublicTestingEventsDirectory(
  options: PublicTestingEventsDirectoryOptions = {},
): Promise<PublicTestingEventsDirectory> {
  const api = createPublicModules();
  const result = await read(
    api.events.getTestingEventsPublic({
      skip: Math.max(0, options.skip ?? 0),
      take: Math.min(100, Math.max(1, options.take ?? 50)),
    }),
    'Public events',
  );
  return {
    events: result.data ?? [],
    accessIssues: result.issue ? [result.issue] : [],
  };
}

export async function getPublicTestingEventExperience(
  eventId: string,
): Promise<PublicTestingEventExperience> {
  const publicApi = createPublicModules();
  const eventPromise = read(publicApi.events.getTestingEventsPublicByEventId(eventId), 'Public event');
  const session = await auth().catch(() => null);
  const eventResult = await eventPromise;
  const isAuthenticated = Boolean(session?.user);

  if (!isAuthenticated) {
    return {
      event: eventResult.data ?? null,
      applications: [],
      registrations: [],
      feedbackObligations: [],
      isAuthenticated: false,
      accessIssues: eventResult.issue ? [eventResult.issue] : [],
    };
  }

  const api = createAuthenticatedModules();
  const [applicationsResult, registrationsResult, obligationsResult] = await Promise.all([
    read(api.events.getTestingEventsApplicationsMe({ eventId }), 'Your project applications'),
    read(api.participation.getTestingEventsRegistrationsMe({ eventId }), 'Your tester registrations'),
    read(api.participation.getTestingEventsFeedbackObligationsMe({ eventId }), 'Your feedback obligations'),
  ]);

  return {
    event: eventResult.data ?? null,
    applications: applicationsResult.data ?? [],
    registrations: registrationsResult.data ?? [],
    feedbackObligations: obligationsResult.data ?? [],
    isAuthenticated: true,
    accessIssues: [
      eventResult.issue,
      applicationsResult.issue,
      registrationsResult.issue,
      obligationsResult.issue,
    ].filter((issue): issue is string => Boolean(issue)),
  };
}

export async function getTestingParticipationOverview(): Promise<TestingParticipationOverview> {
  const session = await auth().catch(() => null);
  if (!session?.user) {
    return { applications: [], registrations: [], feedbackObligations: [], isAuthenticated: false, accessIssues: [] };
  }

  const api = createAuthenticatedModules();
  const [applicationsResult, registrationsResult, obligationsResult] = await Promise.all([
    read(api.events.getTestingEventsApplicationsMe(), 'Your project applications'),
    read(api.participation.getTestingEventsRegistrationsMe(), 'Your tester registrations'),
    read(api.participation.getTestingEventsFeedbackObligationsMe(), 'Your feedback obligations'),
  ]);

  return {
    applications: applicationsResult.data ?? [],
    registrations: registrationsResult.data ?? [],
    feedbackObligations: obligationsResult.data ?? [],
    isAuthenticated: true,
    accessIssues: [applicationsResult.issue, registrationsResult.issue, obligationsResult.issue]
      .filter((issue): issue is string => Boolean(issue)),
  };
}
