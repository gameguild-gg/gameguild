export type SessionStatus = 0 | 1 | 2 | 3;

export type LocationStatus = 0 | 1 | 2;

export interface TestingLocation {
  id?: string;
  version?: number;
  createdAt?: string;
  updatedAt?: string;
  deletedAt?: string | null;
  name: string;
  description?: string | null;
  address?: string | null;
  maxTestersCapacity: number;
  maxProjectsCapacity: number;
  equipmentAvailable?: string | null;
  status: LocationStatus;
}

export interface TestingSession {
  id?: string;
  locationId?: string;
  location?: TestingLocation;
  sessionName: string;
  sessionDate: string;
  startTime: string;
  endTime: string;
  maxTesters: number;
  registeredTesterCount?: number;
  registeredProjectMemberCount?: number;
  registeredProjectCount?: number;
  status: SessionStatus;
  managerId?: string;
  createdById?: string;
}

export interface CreateTestingSessionDto {
  sessionName: string;
  sessionDate: string;
  startTime: string;
  endTime: string;
  locationId: string;
  maxTesters: number;
  testingRequestId?: string;
}

export const SessionStatusEnum = {
  Draft: 0 as SessionStatus,
  Scheduled: 1 as SessionStatus,
  InProgress: 2 as SessionStatus,
  Completed: 3 as SessionStatus,
} as const;

export const LocationStatusEnum = {
  Inactive: 0 as LocationStatus,
  Active: 1 as LocationStatus,
  Maintenance: 2 as LocationStatus,
} as const;

export function getSessionStatusLabel(status: SessionStatus): string {
  switch (status) {
    case SessionStatusEnum.Draft:
      return 'Draft';
    case SessionStatusEnum.Scheduled:
      return 'Scheduled';
    case SessionStatusEnum.InProgress:
      return 'In Progress';
    case SessionStatusEnum.Completed:
      return 'Completed';
    default:
      return 'Unknown';
  }
}

export function getLocationStatusLabel(status: LocationStatus): string {
  switch (status) {
    case LocationStatusEnum.Inactive:
      return 'Inactive';
    case LocationStatusEnum.Active:
      return 'Active';
    case LocationStatusEnum.Maintenance:
      return 'Maintenance';
    default:
      return 'Unknown';
  }
}

export interface TestingFeedback {
  id: string;
  content: string;
  rating: number;
  sessionTitle: string;
  submittedBy: {
    id: string;
    name: string;
  };
  submittedAt: string;
}
