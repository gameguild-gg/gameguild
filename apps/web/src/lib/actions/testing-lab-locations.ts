'use server';

import { auth } from '@/auth';
import { 
  createTestingLocation, 
  updateTestingLocation, 
  deleteTestingLocation, 
  getTestingLocations
} from '@/lib/api/testing-lab';
import type { TestingLocation, CreateTestingLocationDto, UpdateTestingLocationDto } from '@/lib/api/generated/types.gen';
import { revalidatePath } from 'next/cache';

/**
 * Server action to get all testing locations
 */
export async function getTestingLocationsAction(skip: number = 0, take: number = 50): Promise<TestingLocation[]> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  
  // Ensure fresh data by revalidating
  revalidatePath('/testing-lab-settings');
  
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
export async function createTestingLocationAction(locationData: CreateTestingLocationDto): Promise<TestingLocation> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  
  try {
    console.log('Creating location with data:', JSON.stringify(locationData, null, 2));
    const result = await createTestingLocation(locationData);
    
    // Revalidate the path to ensure fresh data on next load
    revalidatePath('/testing-lab-settings');
    
    return result;
  } catch (error) {
    console.error('Error creating testing location:', error);
    console.error('Location data that caused error:', JSON.stringify(locationData, null, 2));
    
    if (error instanceof Error) {
      console.error('Full error message:', error.message);
      
      if (error.message.includes('Unauthorized')) {
        throw new Error('You do not have permission to create testing locations. Please contact your administrator.');
      }
      if (error.message.includes('400') || error.message.includes('Bad Request')) {
        throw new Error(`Invalid location data: ${error.message}`);
      }
      if (error.message.includes('409') || error.message.includes('Conflict')) {
        throw new Error('A location with this name already exists.');
      }
      // Don't mask the original error - pass it through for debugging
      throw new Error(`Location creation failed: ${error.message}`);
    }
    throw new Error(`An unexpected error occurred while creating the testing location: ${String(error)}`);
  }
}

/**
 * Server action to update an existing testing location
 */
export async function updateTestingLocationAction(id: string, locationData: UpdateTestingLocationDto): Promise<TestingLocation> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  
  try {
    console.log('Updating location with data:', JSON.stringify(locationData, null, 2));
    return await updateTestingLocation(id, locationData);
  } catch (error) {
    console.error('Error updating testing location:', error);
    console.error('Location data that caused error:', JSON.stringify(locationData, null, 2));
    
    if (error instanceof Error) {
      console.error('Full error message:', error.message);
      
      if (error.message.includes('Unauthorized')) {
        throw new Error('You do not have permission to update testing locations. Please contact your administrator.');
      }
      if (error.message.includes('404') || error.message.includes('Not Found')) {
        throw new Error('Testing location not found. It may have been deleted.');
      }
      if (error.message.includes('400') || error.message.includes('Bad Request')) {
        throw new Error(`Invalid location data: ${error.message}`);
      }
      if (error.message.includes('409') || error.message.includes('Conflict')) {
        throw new Error('A location with this name already exists.');
      }
      // Don't mask the original error - pass it through for debugging
      throw new Error(`Location update failed: ${error.message}`);
    }
    throw new Error(`An unexpected error occurred while updating the testing location: ${String(error)}`);
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
