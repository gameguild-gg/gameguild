'use server';

import { auth } from '@/auth';
import { TestingLabSettingsAPI, type CreateTestingLabSettingsDto, type UpdateTestingLabSettingsDto, type TestingLabSettingsDto } from '@/lib/api/testing-lab-settings';

/**
 * Server action to get testing lab settings
 */
export async function getTestingLabSettings(): Promise<TestingLabSettingsDto> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  
  try {
    return await TestingLabSettingsAPI.getTestingLabSettings(session.api.accessToken);
  } catch (error) {
    console.error('Error getting testing lab settings:', error);
    // Re-throw with more descriptive message
    if (error instanceof Error) {
      if (error.message.includes('Unauthorized')) {
        throw new Error('You do not have permission to access TestingLab settings. Please contact your administrator.');
      }
      throw error;
    }
    throw new Error('An unexpected error occurred while getting testing lab settings');
  }
}

/**
 * Server action to create or update testing lab settings
 */
export async function createOrUpdateTestingLabSettings(dto: CreateTestingLabSettingsDto): Promise<TestingLabSettingsDto> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  
  return await TestingLabSettingsAPI.createOrUpdateTestingLabSettings(dto, session.api.accessToken);
}

/**
 * Server action to update testing lab settings (partial)
 */
export async function updateTestingLabSettings(dto: UpdateTestingLabSettingsDto): Promise<TestingLabSettingsDto> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  
  return await TestingLabSettingsAPI.updateTestingLabSettings(dto, session.api.accessToken);
}

/**
 * Server action to reset testing lab settings
 */
export async function resetTestingLabSettings(): Promise<TestingLabSettingsDto> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  
  return await TestingLabSettingsAPI.resetTestingLabSettings(session.api.accessToken);
}

/**
 * Server action to check if testing lab settings exist
 */
export async function testingLabSettingsExist(): Promise<boolean> {
  const session = await auth();
  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }
  
  return await TestingLabSettingsAPI.testingLabSettingsExist(session.api.accessToken);
}
