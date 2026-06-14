import { getToken } from '@/auth';
import { cache } from 'react';

export type TestingRequestStatus = 'Draft' | 'Open' | 'Active' | 'InProgress' | 'Paused' | 'Completed' | 'Cancelled' | number;
export type TestingSessionStatus = 'Scheduled' | 'Active' | 'Completed' | 'Cancelled' | number;
export type TestingLocationStatus = 'Active' | 'Maintenance' | 'Inactive' | number;

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
}

export interface TestingLocationSummary {
  id: string;
  name: string;
  description?: string | null;
  city?: string | null;
  country?: string | null;
  isVirtual?: boolean;
  virtualUrl?: string | null;
  maxTestersCapacity?: number | null;
  maxProjectsCapacity?: number | null;
  capacity?: number | null;
  status: TestingLocationStatus;
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
  status: TestingSessionStatus;
}

export interface TestingLabDashboardData {
  requests: TestingRequestSummary[];
  sessions: TestingSessionSummary[];
  locations: TestingLocationSummary[];
  publicSessions: TestingSessionSummary[];
  accessIssues: string[];
}

interface ApiReadResult<T> {
  data: T | null;
  issue?: string;
}

async function testingLabApiGet<T>(path: string, authenticated = true): Promise<ApiReadResult<T>> {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
  const token = authenticated ? await getToken() : null;
  const response = await fetch(`${apiUrl}${path}`, {
    headers: {
      Accept: 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    next: { revalidate: 30 },
  });

  if (response.status === 401 || response.status === 403) {
    return { data: null, issue: `${path} returned ${response.status}` };
  }

  if (!response.ok) {
    return { data: null, issue: `${path} returned ${response.status}` };
  }

  return { data: (await response.json()) as T };
}

export const getTestingLabDashboard = cache(async (): Promise<TestingLabDashboardData> => {
  const [requests, sessions, locations, publicSessions] = await Promise.all([
    testingLabApiGet<TestingRequestSummary[]>('/v1/testing/requests?take=20'),
    testingLabApiGet<TestingSessionSummary[]>('/v1/testing/sessions?take=20'),
    testingLabApiGet<TestingLocationSummary[]>('/v1/testing/locations?take=20'),
    testingLabApiGet<TestingSessionSummary[]>('/v1/testing/public/sessions?take=20', false),
  ]);

  return {
    requests: requests.data ?? [],
    sessions: sessions.data ?? [],
    locations: locations.data ?? [],
    publicSessions: publicSessions.data ?? [],
    accessIssues: [requests.issue, sessions.issue, locations.issue, publicSessions.issue].filter(Boolean) as string[],
  };
});

export function normalizeTestingRequestStatus(status: TestingRequestStatus): string {
  if (typeof status === 'string') return status;
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
