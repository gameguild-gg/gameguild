'use server';

import { auth } from '@/auth';
import { 
  createTestingLocation, 
  updateTestingLocation, 
  deleteTestingLocation, 
  getTestingLocations, 
  type CreateTestingLocationRequest, 
  type UpdateTestingLocationRequest 
} from '@/lib/api/testing-lab';
import type { TestingLocation } from '@/lib/api/testing-types';

/**
 * Server action to get all testing locations
 */
export async function getTestingLocationsAction(skip: number = 0, take: number = 50): Promise<TestingLocation[]> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  
  try {
    return await getTestingLocations(skip, take);
  } catch (error) {
    console.error('Error getting testing locations:', error);
    if (error instanceof Error) {
      if (error.message.includes('Unauthorized')) {
        throw new Error('You do not have permission to access TestingLab locations. Please contact your administrator.');
      }
      throw error;
    }
    throw new Error('An unexpected error occurred while getting testing locations');
  }
}

/**
 * Server action to create a new testing location
 */
export async function createTestingLocationAction(locationData: CreateTestingLocationRequest): Promise<TestingLocation> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  
  try {
    return await createTestingLocation(locationData);
  } catch (error) {
    console.error('Error creating testing location:', error);
    if (error instanceof Error) {
      if (error.message.includes('Unauthorized')) {
        throw new Error('You do not have permission to create testing locations. Please contact your administrator.');
      }
      if (error.message.includes('400') || error.message.includes('Bad Request')) {
        throw new Error('Invalid location data. Please check your input and try again.');
      }
      if (error.message.includes('409') || error.message.includes('Conflict')) {
        throw new Error('A location with this name already exists.');
      }
      throw error;
    }
    throw new Error('An unexpected error occurred while creating the testing location');
  }
}

/**
 * Server action to update an existing testing location
 */
export async function updateTestingLocationAction(id: string, locationData: UpdateTestingLocationRequest): Promise<TestingLocation> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  
  try {
    return await updateTestingLocation(id, locationData);
  } catch (error) {
    console.error('Error updating testing location:', error);
    if (error instanceof Error) {
      if (error.message.includes('Unauthorized')) {
        throw new Error('You do not have permission to update testing locations. Please contact your administrator.');
      }
      if (error.message.includes('404') || error.message.includes('Not Found')) {
        throw new Error('Testing location not found. It may have been deleted.');
      }
      if (error.message.includes('400') || error.message.includes('Bad Request')) {
        throw new Error('Invalid location data. Please check your input and try again.');
      }
      if (error.message.includes('409') || error.message.includes('Conflict')) {
        throw new Error('A location with this name already exists.');
      }
      throw error;
    }
    throw new Error('An unexpected error occurred while updating the testing location');
  }
}

/**
 * Server action to delete a testing location
 */
export async function deleteTestingLocationAction(id: string): Promise<void> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  
  try {
    return await deleteTestingLocation(id);
  } catch (error) {
    console.error('Error deleting testing location:', error);
    if (error instanceof Error) {
      if (error.message.includes('Unauthorized')) {
        throw new Error('You do not have permission to delete testing locations. Please contact your administrator.');
      }
      if (error.message.includes('404') || error.message.includes('Not Found')) {
        throw new Error('Testing location not found. It may have already been deleted.');
      }
      if (error.message.includes('409') || error.message.includes('Conflict')) {
        throw new Error('Cannot delete location. There may be active sessions or dependencies.');
      }
      throw error;
    }
    throw new Error('An unexpected error occurred while deleting the testing location');
  }
}
