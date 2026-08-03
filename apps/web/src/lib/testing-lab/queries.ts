import { auth, getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type ApiError,
  type ProjectsProject,
  type Result,
  type TestingLabTestingLabAnalyticsReportProjection,
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
  state?: string | null;
  postalCode?: string | null;
  country?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
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
    analytics: new GeneratedApi.TestinglabTestinganalyticsModule(client),
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
    state: location.state,
    postalCode: location.postalCode,
    country: location.country,
    contactEmail: location.contactEmail,
    contactPhone: location.contactPhone,
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

export interface TestingLabLocationDirectory {
  locations: TestingLocationSummary[];
  accessIssues: string[];
}

export const getTestingLabLocations = cache(async (): Promise<TestingLabLocationDirectory> => {
  const api = createTestingLabModules();
  const locations = await readResult(
    api.locations.getTestingLocations({ skip: 0, take: 200, includeArchived: true }),
    'Testing locations',
  );

  return {
    locations: compact((locations.data ?? []).map(mapLocation)),
    accessIssues: locations.issue ? [locations.issue] : [],
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

export interface TestingLabAnalyticsSummary {
  events: number;
  completedEvents: number;
  applications: number;
  approvedProjects: number;
  registeredTesters: number;
  attendedTesters: number;
  feedback: number;
  averageRating: number | null;
  recommendationRate: number | null;
  capacity: number;
  fillRate: number;
}

export interface TestingLabAnalyticsTrend {
  date: string;
  events: number;
  applications: number;
  registrations: number;
  attendance: number;
  feedback: number;
}

export interface TestingLabEventAnalytics {
  eventId: string;
  name: string;
  status: string;
  mode: string;
  startsAt: string;
  applications: number;
  approvedProjects: number;
  registeredTesters: number;
  attendedTesters: number;
  feedback: number;
  averageRating: number | null;
  capacity: number;
  fillRate: number;
}

export interface TestingLabAnalyticsData {
  fromDate: string;
  toDate: string;
  generatedAt: string | null;
  current: TestingLabAnalyticsSummary;
  previous: TestingLabAnalyticsSummary | null;
  locations: { total: number; active: number };
  trend: TestingLabAnalyticsTrend[];
  events: TestingLabEventAnalytics[];
  accessIssues: string[];
}

export interface TestingLabAnalyticsOptions {
  fromDate?: string;
  toDate?: string;
  includeComparison?: boolean;
}

export interface TestingLabAnalyticsCsvResult {
  data: string | null;
  issue?: string;
}

const emptyAnalyticsSummary: TestingLabAnalyticsSummary = {
  events: 0,
  completedEvents: 0,
  applications: 0,
  approvedProjects: 0,
  registeredTesters: 0,
  attendedTesters: 0,
  feedback: 0,
  averageRating: null,
  recommendationRate: null,
  capacity: 0,
  fillRate: 0,
};

function mapAnalyticsSummary(summary: TestingLabTestingLabAnalyticsReportProjection['current'] | null | undefined): TestingLabAnalyticsSummary {
  return {
    events: summary?.events ?? 0,
    completedEvents: summary?.completedEvents ?? 0,
    applications: summary?.applications ?? 0,
    approvedProjects: summary?.approvedProjects ?? 0,
    registeredTesters: summary?.registeredTesters ?? 0,
    attendedTesters: summary?.attendedTesters ?? 0,
    feedback: summary?.feedback ?? 0,
    averageRating: summary?.averageRating ?? null,
    recommendationRate: summary?.recommendationRate ?? null,
    capacity: summary?.capacity ?? 0,
    fillRate: summary?.fillRate ?? 0,
  };
}

export const getTestingLabAnalytics = cache(async (options: TestingLabAnalyticsOptions = {}): Promise<TestingLabAnalyticsData> => {
  const api = createTestingLabModules();
  const report = await readResult(api.analytics.getTestingAnalytics(options), 'Testing Lab analytics');
  const data = report.data;

  return {
    fromDate: data?.fromDate ?? options.fromDate ?? '',
    toDate: data?.toDate ?? options.toDate ?? '',
    generatedAt: data?.generatedAt ?? null,
    current: data?.current ? mapAnalyticsSummary(data.current) : { ...emptyAnalyticsSummary },
    previous: data?.previous ? mapAnalyticsSummary(data.previous) : null,
    locations: {
      total: data?.locations?.total ?? 0,
      active: data?.locations?.active ?? 0,
    },
    trend: (data?.trend ?? []).map((item) => ({
      date: item.date ?? '',
      events: item.events ?? 0,
      applications: item.applications ?? 0,
      registrations: item.registrations ?? 0,
      attendance: item.attendance ?? 0,
      feedback: item.feedback ?? 0,
    })),
    events: (data?.events ?? [])
      .filter((item) => item.eventId)
      .map((item) => ({
        eventId: item.eventId!,
        name: item.name ?? 'Untitled event',
        status: String(item.status ?? 'Draft'),
        mode: String(item.mode ?? 'Online'),
        startsAt: item.startsAt ?? '',
        applications: item.applications ?? 0,
        approvedProjects: item.approvedProjects ?? 0,
        registeredTesters: item.registeredTesters ?? 0,
        attendedTesters: item.attendedTesters ?? 0,
        feedback: item.feedback ?? 0,
        averageRating: item.averageRating ?? null,
        capacity: item.capacity ?? 0,
        fillRate: item.fillRate ?? 0,
      })),
    accessIssues: report.issue ? [report.issue] : [],
  };
});

export async function getTestingLabAnalyticsCsv(
  options: Omit<TestingLabAnalyticsOptions, 'includeComparison'>,
): Promise<TestingLabAnalyticsCsvResult> {
  const api = createTestingLabModules();
  const result = await readResult(api.analytics.getTestingAnalyticsExport(options), 'Testing Lab analytics export');
  if (!result.data) return { data: null, issue: result.issue };

  try {
    return { data: await result.data.text() };
  } catch (error) {
    return {
      data: null,
      issue: `Testing Lab analytics export could not be read: ${error instanceof Error ? error.message : 'Unknown error'}`,
    };
  }
}
export interface TestingLabLocationFilterOptions {
  q?: string;
  status?: 'all' | 'active' | 'maintenance' | 'inactive' | 'archived';
  mode?: 'all' | 'physical' | 'remote';
}

export function filterTestingLabLocations(
  locations: TestingLocationSummary[],
  options: TestingLabLocationFilterOptions,
): TestingLocationSummary[] {
  const query = options.q?.trim().toLowerCase() ?? '';
  const status = options.status ?? 'all';
  const mode = options.mode ?? 'all';

  return locations.filter((location) => {
    const searchable = [
      location.name,
      location.description,
      location.address,
      location.city,
      location.state,
      location.postalCode,
      location.country,
      location.contactEmail,
    ]
      .filter(Boolean)
      .join(' ')
      .toLowerCase();
    const normalizedStatus = normalizeTestingLocationStatus(location.status).toLowerCase();
    const matchesStatus =
      status === 'all' ||
      (status === 'archived' ? Boolean(location.isDeleted) : !location.isDeleted && normalizedStatus === status);
    const matchesMode = mode === 'all' || (mode === 'remote' ? Boolean(location.isVirtual) : !location.isVirtual);

    return (!query || searchable.includes(query)) && matchesStatus && matchesMode;
  });
}
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
