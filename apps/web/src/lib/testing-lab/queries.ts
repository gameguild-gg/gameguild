import { auth, getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type ApiError,
  type ProjectsProject,
  type Result,
  type TestingLabSessionProjectProjection,
  type TestingLabSessionRegistration,
  type TestingLabSessionWaitlist,
  type TestingLabTestingFeedback,
  type TestingLabTestingInput,
  type TestingLabTestingLabRoleTemplate,
  type TestingLabTestingLabSettings,
  type TestingLabTestingLocation,
  type TestingLabTestingParticipant,
  type TestingLabTestingSession,
} from '@game-guild/client';
import { cache } from 'react';

export type TestingRequestStatus = 'Draft' | 'Open' | 'Active' | 'InProgress' | 'Paused' | 'Completed' | 'Cancelled' | number;
export type TestingSessionStatus = 'Scheduled' | 'Active' | 'Completed' | 'Cancelled' | number;
export type TestingLocationStatus = 'Active' | 'Maintenance' | 'Inactive' | number;

export interface TestingProjectSummary {
  id: string;
  title?: string | null;
  name?: string | null;
  slug?: string | null;
  status?: string | number | null;
}

export interface TestingProjectVersionSummary {
  id: string;
  projectId: string;
  versionNumber?: string | null;
  status?: string | null;
  project?: TestingProjectSummary | null;
}

export interface TestingRequestSummary {
  id: string;
  title: string;
  description?: string | null;
  downloadUrl?: string | null;
  instructionsContent?: string | null;
  feedbackFormContent?: string | null;
  maxTesters?: number | null;
  currentTesterCount?: number | null;
  startDate?: string | null;
  endDate?: string | null;
  status: TestingRequestStatus;
  projectVersionId?: string | null;
  projectVersion?: TestingProjectVersionSummary | null;
  isDeleted?: boolean;
}

export interface TestingLocationSummary {
  id: string;
  name: string;
  description?: string | null;
  address?: string | null;
  equipmentAvailable?: string | null;
  city?: string | null;
  country?: string | null;
  isVirtual?: boolean;
  virtualUrl?: string | null;
  maxTestersCapacity?: number | null;
  maxProjectsCapacity?: number | null;
  capacity?: number | null;
  status: TestingLocationStatus;
  isDeleted?: boolean;
}

export interface TestingSessionSummary {
  id: string;
  testingRequestId?: string | null;
  locationId?: string | null;
  location?: TestingLocationSummary | null;
  sessionName: string;
  sessionDate?: string | null;
  startTime?: string | null;
  endTime?: string | null;
  maxTesters?: number | null;
  registeredTesterCount?: number | null;
  registeredProjectCount?: number | null;
  maxProjects?: number | null;
  availableSpots?: number | null;
  allowsRegistration?: boolean;
  testingRequest?: TestingRequestSummary | null;
  status: TestingSessionStatus;
  isDeleted?: boolean;
}

export interface TestingLabDashboardData {
  requests: TestingRequestSummary[];
  sessions: TestingSessionSummary[];
  locations: TestingLocationSummary[];
  publicSessions: TestingSessionSummary[];
  accessIssues: string[];
}

export interface TestingProjectOption {
  id: string;
  title: string;
  slug?: string | null;
  status?: string | number | null;
}

interface ApiReadResult<T> {
  data: T | null;
  issue?: string;
}

function getApiUrl() {
  return process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
}

function createTestingLabModules() {
  const client = createServerClient({
    baseUrl: getApiUrl(),
    auth: { getAccessToken: () => getToken() },
    tenant: { getTenantId: async () => (await auth().catch(() => null))?.tenantId ?? null },
  });

  return {
    requests: new GeneratedApi.TestinglabTestingrequestsModule(client),
    sessions: new GeneratedApi.TestinglabTestingsessionsModule(client),
    locations: new GeneratedApi.TestinglabTestinglocationsModule(client),
    participants: new GeneratedApi.TestinglabTestingparticipantsModule(client),
    feedback: new GeneratedApi.TestinglabTestingfeedbackModule(client),
    settings: new GeneratedApi.TestinglabSettingsModule(client),
    permissions: new GeneratedApi.TestinglabPermissionModule(client),
    projects: new GeneratedApi.ProjectsModule(client),
  };
}

async function readResult<T>(operation: Promise<Result<T, ApiError>>, label: string): Promise<ApiReadResult<T>> {
  try {
    const result = await operation;
    if (result.ok) return { data: result.data };
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

function mapProject(project: ProjectsProject): TestingProjectOption | null {
  if (!project.id) return null;
  return {
    id: project.id,
    title: project.title || project.slug || project.id,
    slug: project.slug,
    status: project.status,
  };
}

function mapRequest(request: TestingLabTestingInput): TestingRequestSummary | null {
  if (!request.id) return null;
  const version = request.projectVersion;
  return {
    id: request.id,
    title: request.title,
    description: request.description,
    downloadUrl: request.downloadUrl,
    instructionsContent: request.instructionsContent,
    feedbackFormContent: request.feedbackFormContent,
    maxTesters: request.maxTesters,
    currentTesterCount: request.currentTesterCount,
    startDate: request.startDate,
    endDate: request.endDate,
    status: request.status,
    projectVersionId: request.projectVersionId,
    projectVersion:
      version?.id && version.projectId
        ? {
            id: version.id,
            projectId: version.projectId,
            versionNumber: version.versionNumber,
            status: version.status,
            project: version.project?.id
              ? {
                  id: version.project.id,
                  title: version.project.title,
                  slug: version.project.slug,
                  status: version.project.status,
                }
              : null,
          }
        : null,
    isDeleted: request.isDeleted,
  };
}

function mapLocation(location: TestingLabTestingLocation): TestingLocationSummary | null {
  if (!location.id) return null;
  return {
    id: location.id,
    name: location.name,
    description: location.description,
    address: location.address,
    equipmentAvailable: location.equipmentAvailable,
    city: location.city,
    country: location.country,
    isVirtual: location.isVirtual,
    virtualUrl: location.virtualUrl,
    maxTestersCapacity: location.maxTestersCapacity,
    maxProjectsCapacity: location.maxProjectsCapacity,
    capacity: location.capacity,
    status: location.status ?? 'Inactive',
    isDeleted: location.isDeleted,
  };
}

function mapSession(session: TestingLabTestingSession): TestingSessionSummary | null {
  if (!session.id) return null;
  return {
    id: session.id,
    testingRequestId: session.testingRequestId,
    locationId: session.locationId,
    location: session.location ? mapLocation(session.location) : null,
    sessionName: session.sessionName,
    sessionDate: session.sessionDate,
    startTime: session.startTime,
    endTime: session.endTime,
    maxTesters: session.maxTesters,
    registeredTesterCount: session.registeredTesterCount,
    registeredProjectCount: session.registeredProjectCount,
    maxProjects: session.maxProjects,
    availableSpots: session.availableSpots,
    allowsRegistration: session.allowsRegistration,
    testingRequest: session.testingRequest ? mapRequest(session.testingRequest) : null,
    status: session.status,
    isDeleted: session.isDeleted,
  };
}

function compact<T>(values: Array<T | null>): T[] {
  return values.filter((value): value is T => value !== null);
}

export const getTestingLabDashboard = cache(async (): Promise<TestingLabDashboardData> => {
  const api = createTestingLabModules();
  const [requests, sessions, locations, publicSessions] = await Promise.all([
    readResult(api.requests.getTestingRequests({ skip: 0, take: 200 }), 'Testing requests'),
    readResult(api.sessions.getTestingSessions({ skip: 0, take: 200 }), 'Testing sessions'),
    readResult(api.locations.getTestingLocations({ skip: 0, take: 200 }), 'Testing locations'),
    readResult(api.sessions.getTestingPublicSessions({ take: 200 }), 'Public testing sessions'),
  ]);

  return {
    requests: compact((requests.data ?? []).map(mapRequest)),
    sessions: compact((sessions.data ?? []).map(mapSession)),
    locations: compact((locations.data ?? []).map(mapLocation)),
    publicSessions: compact((publicSessions.data ?? []).map(mapSession)),
    accessIssues: [requests.issue, sessions.issue, locations.issue, publicSessions.issue].filter(Boolean) as string[],
  };
});

export interface PublicTestingLabDirectory {
  sessions: TestingSessionSummary[];
  projects: TestingProjectOption[];
  accessIssues: string[];
}

export const getPublicTestingLabDirectory = cache(async (): Promise<PublicTestingLabDirectory> => {
  const api = createTestingLabModules();
  const [sessions, projects] = await Promise.all([
    readResult(api.sessions.getTestingPublicSessions({ take: 100 }), 'Public testing sessions'),
    readResult(api.projects.getProjects({ skip: 0, take: 100, sortBy: 'UpdatedAt', sortDirection: 'DESC' }), 'Testable projects'),
  ]);

  return {
    sessions: compact((sessions.data ?? []).map(mapSession)),
    projects: compact((projects.data ?? []).map(mapProject)).filter((project) => project.status === 'Published' || project.status === 1),
    accessIssues: [sessions.issue, projects.issue].filter(Boolean) as string[],
  };
});
export const getTestingProjectOptions = cache(async (): Promise<TestingProjectOption[]> => {
  const api = createTestingLabModules();
  const projects = await readResult(api.projects.getProjects({ skip: 0, take: 50, sortBy: 'UpdatedAt', sortDirection: 'DESC' }), 'Projects');

  return compact((projects.data ?? []).map(mapProject));
});

export interface TestingRequestDetailData {
  request: TestingRequestSummary | null;
  sessions: TestingSessionSummary[];
  participants: TestingLabTestingParticipant[];
  feedback: TestingLabTestingFeedback[];
  accessIssues: string[];
}

export const getTestingRequestDetail = cache(async (requestId: string): Promise<TestingRequestDetailData> => {
  const api = createTestingLabModules();
  const [request, sessions, participants, feedback] = await Promise.all([
    readResult(api.requests.getTestingRequests1(requestId), 'Testing request'),
    readResult(api.sessions.getTestingSessionsByRequest(requestId), 'Request sessions'),
    readResult(api.participants.getTestingRequestsParticipants(requestId), 'Request participants'),
    readResult(api.feedback.getTestingRequestsFeedback(requestId), 'Request feedback'),
  ]);

  return {
    request: request.data ? mapRequest(request.data) : null,
    sessions: compact((sessions.data ?? []).map(mapSession)),
    participants: participants.data ?? [],
    feedback: feedback.data ?? [],
    accessIssues: [request.issue, sessions.issue, participants.issue, feedback.issue].filter(Boolean) as string[],
  };
});

export interface TestingSessionDetailData {
  session: TestingSessionSummary | null;
  registrations: TestingLabSessionRegistration[];
  waitlist: TestingLabSessionWaitlist[];
  projects: TestingLabSessionProjectProjection[];
  accessIssues: string[];
}

export const getTestingSessionDetail = cache(async (sessionId: string): Promise<TestingSessionDetailData> => {
  const api = createTestingLabModules();
  const [session, registrations, waitlist, projects] = await Promise.all([
    readResult(api.sessions.getTestingSessions1(sessionId), 'Testing session'),
    readResult(api.participants.getTestingSessionsRegistrations(sessionId), 'Session registrations'),
    readResult(api.participants.getTestingSessionsWaitlist(sessionId), 'Session waitlist'),
    readResult(api.sessions.getTestingSessionsProjects(sessionId, { includeInactive: true }), 'Session projects'),
  ]);

  return {
    session: session.data ? mapSession(session.data) : null,
    registrations: registrations.data ?? [],
    waitlist: waitlist.data ?? [],
    projects: projects.data ?? [],
    accessIssues: [session.issue, registrations.issue, waitlist.issue, projects.issue].filter(Boolean) as string[],
  };
});

export interface TestingLabAdministrationData {
  settings: TestingLabTestingLabSettings | null;
  roles: TestingLabTestingLabRoleTemplate[];
  accessIssues: string[];
}

export const getTestingLabAdministration = cache(async (): Promise<TestingLabAdministrationData> => {
  const api = createTestingLabModules();
  const [settings, roles] = await Promise.all([
    readResult(api.settings.getApiTestingLabSettings(), 'Testing Lab settings'),
    readResult(api.permissions.getApiTestingLabPermissionsRoleTemplates(), 'Testing Lab roles'),
  ]);

  return {
    settings: settings.data,
    roles: roles.data ?? [],
    accessIssues: [settings.issue, roles.issue].filter(Boolean) as string[],
  };
});

export interface TestingLabAnalyticsData {
  requests: { total: number; open: number; active: number; completed: number };
  sessions: { total: number; scheduled: number; active: number; completed: number };
  capacity: { registered: number; available: number; total: number; fillRate: number };
  feedback: { total: number; averageRating: number | null; recommendationRate: number | null };
  locations: { total: number; active: number };
  accessIssues: string[];
}

function roundedPercent(numerator: number, denominator: number): number {
  return denominator > 0 ? Math.round((numerator / denominator) * 10000) / 100 : 0;
}

export const getTestingLabAnalytics = cache(async (): Promise<TestingLabAnalyticsData> => {
  const api = createTestingLabModules();
  const [requests, sessions, locations] = await Promise.all([
    readResult(api.requests.getTestingRequests({ skip: 0, take: 500 }), 'Testing requests'),
    readResult(api.sessions.getTestingSessions({ skip: 0, take: 500 }), 'Testing sessions'),
    readResult(api.locations.getTestingLocations({ skip: 0, take: 500 }), 'Testing locations'),
  ]);

  const requestRecords = requests.data ?? [];
  const sessionRecords = sessions.data ?? [];
  const locationRecords = locations.data ?? [];
  const feedbackResults = await Promise.all(
    requestRecords
      .filter((request) => request.id)
      .map((request) => readResult(api.feedback.getTestingRequestsFeedback(request.id!), `Feedback for ${request.title}`)),
  );
  const feedbackRecords = feedbackResults.flatMap((result) => result.data ?? []);
  const ratings = feedbackRecords.map((feedback) => feedback.overallRating).filter((rating): rating is number => typeof rating === 'number');
  const recommendations = feedbackRecords
    .map((feedback) => feedback.wouldRecommend)
    .filter((recommendation): recommendation is boolean => typeof recommendation === 'boolean');
  const registered = sessionRecords.reduce((total, session) => total + (session.registeredTesterCount ?? 0), 0);
  const totalCapacity = sessionRecords.reduce((total, session) => total + Math.max(0, session.maxTesters ?? 0), 0);
  const requestStatuses = requestRecords.map((request) => normalizeTestingRequestStatus(request.status));
  const sessionStatuses = sessionRecords.map((session) => normalizeTestingSessionStatus(session.status));

  return {
    requests: {
      total: requestRecords.length,
      open: requestStatuses.filter((status) => status === 'Open').length,
      active: requestStatuses.filter((status) => status === 'Active' || status === 'In Progress').length,
      completed: requestStatuses.filter((status) => status === 'Completed').length,
    },
    sessions: {
      total: sessionRecords.length,
      scheduled: sessionStatuses.filter((status) => status === 'Scheduled').length,
      active: sessionStatuses.filter((status) => status === 'Active').length,
      completed: sessionStatuses.filter((status) => status === 'Completed').length,
    },
    capacity: {
      registered,
      available: Math.max(0, totalCapacity - registered),
      total: totalCapacity,
      fillRate: roundedPercent(registered, totalCapacity),
    },
    feedback: {
      total: feedbackRecords.length,
      averageRating: ratings.length > 0 ? Math.round((ratings.reduce((total, rating) => total + rating, 0) / ratings.length) * 100) / 100 : null,
      recommendationRate: recommendations.length > 0 ? roundedPercent(recommendations.filter(Boolean).length, recommendations.length) : null,
    },
    locations: {
      total: locationRecords.length,
      active: locationRecords.filter((location) => normalizeTestingLocationStatus(location.status ?? 'Inactive') === 'Active').length,
    },
    accessIssues: [requests.issue, sessions.issue, locations.issue, ...feedbackResults.map((result) => result.issue)].filter(Boolean) as string[],
  };
});
export function normalizeTestingRequestStatus(status: TestingRequestStatus): string {
  if (typeof status === 'string') return status === 'InProgress' ? 'In Progress' : status;
  return ['Draft', 'Open', 'Active', 'In Progress', 'Paused', 'Completed', 'Cancelled'][status] ?? 'Unknown';
}

export function normalizeTestingSessionStatus(status: TestingSessionStatus): string {
  if (typeof status === 'string') return status;
  return ['Scheduled', 'Active', 'Completed', 'Cancelled'][status] ?? 'Unknown';
}

export function normalizeTestingLocationStatus(status: TestingLocationStatus): string {
  if (typeof status === 'string') return status;
  return ['Active', 'Maintenance', 'Inactive'][status] ?? 'Unknown';
}

export function countAvailableTesterSlots(requests: TestingRequestSummary[]): number {
  return requests.reduce((total, request) => {
    if (typeof request.maxTesters !== 'number') return total;
    return total + Math.max(0, request.maxTesters - (request.currentTesterCount ?? 0));
  }, 0);
}
