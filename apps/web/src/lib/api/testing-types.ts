// Types for testing functionality - previously generated, now manually defined
// These should eventually be moved back to the generated API when the backend includes testing endpoints

export type SessionStatus = 0 | 1 | 2 | 3;

export type LocationStatus = 0 | 1 | 2;

export type TestingLocation = {
  readonly isNew?: boolean;
  readonly isGlobal?: boolean;
  readonly domainEvents?: Array<IDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt?: string;
  updatedAt?: string;
  deletedAt?: string | null;
  readonly isDeleted?: boolean;
  tenant?: Tenant;
  name: string;
  description?: string | null;
  address?: string | null;
  maxTestersCapacity: number;
  maxProjectsCapacity: number;
  equipmentAvailable?: string | null;
  status: LocationStatus;
};

export type TestingSession = {
  readonly isNew?: boolean;
  readonly isGlobal?: boolean;
  readonly domainEvents?: Array<IDomainEvent> | null;
  id?: string;
  version?: number;
  createdAt?: string;
  updatedAt?: string;
  deletedAt?: string | null;
  readonly isDeleted?: boolean;
  tenant?: Tenant;
  testingRequestId?: string;
  testingRequest?: TestingRequest;
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
  manager?: User;
  managerUserId?: string;
  createdById?: string;
  createdBy?: User;
};

// Basic types referenced by testing types
export interface IDomainEvent {
  readonly eventId?: string;
  readonly occurredAt?: string;
  readonly version?: number;
  readonly aggregateId?: string;
  readonly aggregateType?: string | null;
}

export interface Tenant {
  id?: string;
  name?: string;
  // ... other tenant properties
}

export interface User {
  id?: string;
  name?: string;
  email?: string;
  // ... other user properties
}

export interface TestingRequest {
  id?: string;
  title?: string;
  // ... other testing request properties
}

// Helper types for form data
export interface CreateTestingSessionDto {
  sessionName: string;
  sessionDate: string;
  startTime: string;
  endTime: string;
  locationId: string;
  maxTesters: number;
  testingRequestId?: string;
}

// Status enum helpers
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

// Display helpers
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
