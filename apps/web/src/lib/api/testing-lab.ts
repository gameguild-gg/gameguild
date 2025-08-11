'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { client } from '@/lib/api/generated/client.gen';
import type { TestingLocation, LocationStatus, TestingSession, CreateTestingLocationDto, UpdateTestingLocationDto } from '@/lib/api/generated/types.gen';

// Re-export the generated types for convenience
export type { CreateTestingLocationDto as CreateTestingLocationRequest, UpdateTestingLocationDto as UpdateTestingLocationRequest } from '@/lib/api/generated/types.gen';

export interface TestingLabManager {
  id: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'Admin' | 'Manager' | 'Coordinator';
  permissions: {
    canManageLocations: boolean;
    canScheduleSessions: boolean;
    canManageUsers: boolean;
    canViewReports: boolean;
    canModifySettings: boolean;
  };
  assignedLocations: string[];
  status: 'Active' | 'Inactive';
  createdAt: string;
  updatedAt: string;
}

export interface CreateTestingLabManagerRequest {
  email: string;
  role: 'Admin' | 'Manager' | 'Coordinator';
  permissions: {
    canManageLocations: boolean;
    canScheduleSessions: boolean;
    canManageUsers: boolean;
    canViewReports: boolean;
    canModifySettings: boolean;
  };
  assignedLocations: string[];
}

export interface UpdateTestingLabManagerRequest {
  role?: 'Admin' | 'Manager' | 'Coordinator';
  permissions?: {
    canManageLocations: boolean;
    canScheduleSessions: boolean;
    canManageUsers: boolean;
    canViewReports: boolean;
    canModifySettings: boolean;
  };
  assignedLocations?: string[];
}

/**
 * Get all testing locations
 */
export async function getTestingLocations(skip = 0, take = 50): Promise<TestingLocation[]> {
  try {
    await configureAuthenticatedClient();

    const response = await client.get({
      url: '/testing/locations',
      query: {
        skip: skip.toString(),
        take: take.toString(),
      },
    });

    if (response.error) {
      // Handle different error types
      const errorMessage = typeof response.error === 'object' 
        ? JSON.stringify(response.error) 
        : String(response.error);
      throw new Error(`Failed to fetch testing locations: ${errorMessage}`);
    }

    return response.data as TestingLocation[];
  } catch (error) {
    console.error('Error fetching testing locations from API:', error);
    // Re-throw with better error handling
    if (error instanceof Error) {
      throw error;
    }
    throw new Error(`Failed to fetch testing locations: ${String(error)}`);
  }
}

/**
 * Get a specific testing location by ID
 */
export async function getTestingLocation(id: string): Promise<TestingLocation | null> {
  await configureAuthenticatedClient();

  const response = await client.get({
    url: `/testing/locations/${id}`,
  });

  if (response.error) {
    if (response.response?.status === 404) {
      return null;
    }
    throw new Error(`Failed to fetch testing location: ${response.error}`);
  }

  return response.data as TestingLocation;
}

/**
 * Create a new testing location
 */
export async function createTestingLocation(locationData: CreateTestingLocationDto): Promise<TestingLocation> {
  try {
    await configureAuthenticatedClient();

    const response = await client.post({
      url: '/testing/locations',
      body: locationData,
    });

    if (response.error) {
      // Handle different error types
      const errorMessage = typeof response.error === 'object' 
        ? JSON.stringify(response.error) 
        : String(response.error);
      throw new Error(`Failed to create testing location: ${errorMessage}`);
    }

    return response.data as TestingLocation;
  } catch (error) {
    // Re-throw with better error handling
    if (error instanceof Error) {
      throw error;
    }
    throw new Error(`Failed to create testing location: ${String(error)}`);
  }
}

/**
 * Update an existing testing location
 */
export async function updateTestingLocation(id: string, locationData: UpdateTestingLocationDto): Promise<TestingLocation> {
  try {
    await configureAuthenticatedClient();

    const response = await client.put({
      url: `/testing/locations/${id}`,
      body: locationData,
    });

    if (response.error) {
      // Handle different error types
      const errorMessage = typeof response.error === 'object' 
        ? JSON.stringify(response.error) 
        : String(response.error);
      throw new Error(`Failed to update testing location: ${errorMessage}`);
    }

    return response.data as TestingLocation;
  } catch (error) {
    // Re-throw with better error handling
    if (error instanceof Error) {
      throw error;
    }
    throw new Error(`Failed to update testing location: ${String(error)}`);
  }
}

/**
 * Delete a testing location
 */
export async function deleteTestingLocation(id: string): Promise<void> {
  try {
    await configureAuthenticatedClient();

    const response = await client.delete({
      url: `/testing/locations/${id}`,
    });

    if (response.error) {
      // Handle different error types
      const errorMessage = typeof response.error === 'object' 
        ? JSON.stringify(response.error) 
        : String(response.error);
      throw new Error(`Failed to delete testing location: ${errorMessage}`);
    }
  } catch (error) {
    // Re-throw with better error handling
    if (error instanceof Error) {
      throw error;
    }
    throw new Error(`Failed to delete testing location: ${String(error)}`);
  }
}

/**
 * Restore a soft-deleted testing location
 */
export async function restoreTestingLocation(id: string): Promise<void> {
  await configureAuthenticatedClient();

  const response = await client.post({
    url: `/testing/locations/${id}/restore`,
  });

  if (response.error) {
    throw new Error(`Failed to restore testing location: ${response.error}`);
  }
}

// Mock functions for testing lab managers (to be implemented when backend is ready)
export async function getTestingLabManagers(): Promise<TestingLabManager[]> {
  // This is mock data - replace with actual API call when backend is implemented
  return [
    {
      id: '1',
      userId: 'user-1',
      email: 'alice.johnson@gameguild.gg',
      firstName: 'Alice',
      lastName: 'Johnson',
      role: 'Admin',
      permissions: {
        canManageLocations: true,
        canScheduleSessions: true,
        canManageUsers: true,
        canViewReports: true,
        canModifySettings: true,
      },
      assignedLocations: ['1', '2', '3'],
      status: 'Active',
      createdAt: '2024-01-15T10:00:00Z',
      updatedAt: '2024-01-15T10:00:00Z',
    },
    {
      id: '2',
      userId: 'user-2',
      email: 'bob.smith@gameguild.gg',
      firstName: 'Bob',
      lastName: 'Smith',
      role: 'Manager',
      permissions: {
        canManageLocations: false,
        canScheduleSessions: true,
        canManageUsers: false,
        canViewReports: true,
        canModifySettings: false,
      },
      assignedLocations: ['1', '2'],
      status: 'Active',
      createdAt: '2024-01-16T10:00:00Z',
      updatedAt: '2024-01-16T10:00:00Z',
    },
  ];
}

export async function createTestingLabManager(managerData: CreateTestingLabManagerRequest): Promise<TestingLabManager> {
  // Mock implementation - replace with actual API call
  const newManager: TestingLabManager = {
    id: Date.now().toString(),
    userId: '',
    email: managerData.email,
    firstName: '',
    lastName: '',
    role: managerData.role,
    permissions: managerData.permissions,
    assignedLocations: managerData.assignedLocations,
    status: 'Active',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };

  return newManager;
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function updateTestingLabManager(_id: string, _managerData: UpdateTestingLabManagerRequest): Promise<TestingLabManager> {
  // Mock implementation - replace with actual API call
  throw new Error('Not implemented yet');
}

export async function deleteTestingLabManager(id: string): Promise<void> {
  // Mock implementation - replace with actual API call
  console.log(`Would delete manager with ID: ${id}`);
}

// ============================================================================
// TESTING SESSIONS
// ============================================================================

export interface CreateTestingSessionRequest {
  sessionName: string;
  sessionDate: string;
  startTime: string;
  endTime: string;
  locationId: string;
  maxTesters: number;
  testingRequestId?: string;
  managerId?: string;
}

export async function getTestingSessions(): Promise<TestingSession[]> {
  // Mock implementation - replace with actual API call
  await configureAuthenticatedClient();

  // Return mock data for now
  return [
    {
      id: '1',
      sessionName: 'Morning Testing Session',
      sessionDate: '2024-01-20',
      startTime: '09:00',
      endTime: '12:00',
      maxTesters: 10,
      registeredTesterCount: 5,
      registeredProjectMemberCount: 2,
      registeredProjectCount: 3,
      status: 1, // Scheduled
      locationId: '1',
      createdAt: '2024-01-15T10:00:00Z',
      updatedAt: '2024-01-15T10:00:00Z',
    },
    {
      id: '2',
      sessionName: 'Afternoon Testing Session',
      sessionDate: '2024-01-20',
      startTime: '14:00',
      endTime: '17:00',
      maxTesters: 8,
      registeredTesterCount: 3,
      registeredProjectMemberCount: 1,
      registeredProjectCount: 2,
      status: 0, // Draft
      locationId: '2',
      createdAt: '2024-01-15T11:00:00Z',
      updatedAt: '2024-01-15T11:00:00Z',
    },
  ];
}

export async function createTestingSession(sessionData: CreateTestingSessionRequest): Promise<TestingSession> {
  // Mock implementation - replace with actual API call
  await configureAuthenticatedClient();

  const newSession: TestingSession = {
    id: Date.now().toString(),
    sessionName: sessionData.sessionName,
    sessionDate: sessionData.sessionDate,
    startTime: sessionData.startTime,
    endTime: sessionData.endTime,
    locationId: sessionData.locationId,
    maxTesters: sessionData.maxTesters,
    registeredTesterCount: 0,
    registeredProjectMemberCount: 0,
    registeredProjectCount: 0,
    status: 0, // Draft
    testingRequestId: sessionData.testingRequestId,
    managerId: sessionData.managerId,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };

  return newSession;
}
