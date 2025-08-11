'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { deleteApiUserprofilesById, getApiUserprofiles, getApiUserprofilesById, getApiUserprofilesUserByUserId, postApiUserprofiles, postApiUserprofilesByIdRestore, putApiUserprofilesById } from '@/lib/api/generated/sdk.gen';
import { revalidateTag } from 'next/cache';

// =============================================================================
// USER PROFILE CRUD OPERATIONS
// =============================================================================

/**
 * Get all user profiles with optional filtering and pagination
 */
export async function getUserProfilesData(params?: { skip?: number; take?: number; includeDeleted?: boolean; searchTerm?: string; tenantId?: string }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUserprofiles({
      query: {
        skip: params?.skip || 0,
        take: params?.take || 50,
        includeDeleted: params?.includeDeleted || false,
        searchTerm: params?.searchTerm,
        tenantId: params?.tenantId,
      },
    });

    if (response.error) {
      console.error('Error fetching user profiles:', response.error);
      return { error: response.error };
    }

    return { data: response.data };
  } catch (error) {
    console.error('Error in getUserProfilesData:', error);
    return { error: 'Failed to fetch user profiles' };
  }
}

/**
 * Get a specific user profile by ID
 */
export async function getUserProfileById(id: string, includeDeleted = false) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUserprofilesById({
      path: { id },
      query: { includeDeleted },
    });

    if (response.error) {
      console.error('Error fetching user profile:', response.error);
      return { error: response.error };
    }

    return { data: response.data };
  } catch (error) {
    console.error('Error in getUserProfileById:', error);
    return { error: 'Failed to fetch user profile' };
  }
}

/**
 * Get user profile by user ID
 */
export async function getUserProfileByUserId(userId: string, includeDeleted = false) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUserprofilesUserByUserId({
      path: { userId },
      query: { includeDeleted },
    });

    if (response.error) {
      console.error('Error fetching user profile by user ID:', response.error);
      return { error: response.error };
    }

    return { data: response.data };
  } catch (error) {
    console.error('Error in getUserProfileByUserId:', error);
    return { error: 'Failed to fetch user profile by user ID' };
  }
}

/**
 * Create a new user profile
 */
export async function createUserProfile(profileData: { givenName: string; familyName: string; displayName: string; title?: string | null; description?: string | null; userId: string; tenantId?: string | null }) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiUserprofiles({
      body: profileData,
    });

    if (response.error) {
      console.error('Error creating user profile:', response.error);
      return { error: response.error };
    }

    revalidateTag('user-profiles');
    if (profileData.userId) {
      revalidateTag(`user-profile-${profileData.userId}`);
    }
    return { data: response.data };
  } catch (error) {
    console.error('Error in createUserProfile:', error);
    return { error: 'Failed to create user profile' };
  }
}

/**
 * Update a user profile
 */
export async function updateUserProfile(
  id: string,
  profileData: {
    givenName?: string | null;
    familyName?: string | null;
    displayName?: string | null;
    title?: string | null;
    description?: string | null;
  },
  ifMatch?: number,
) {
  await configureAuthenticatedClient();

  try {
    const response = await putApiUserprofilesById({
      path: { id },
      body: profileData,
      headers: ifMatch ? { 'If-Match': ifMatch } : undefined,
    });

    if (response.error) {
      console.error('Error updating user profile:', response.error);
      return { error: response.error };
    }

    revalidateTag('user-profiles');
    revalidateTag(`user-profile-${id}`);
    return { data: response.data };
  } catch (error) {
    console.error('Error in updateUserProfile:', error);
    return { error: 'Failed to update user profile' };
  }
}

/**
 * Delete a user profile (soft delete by default)
 */
export async function deleteUserProfile(id: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteApiUserprofilesById({
      path: { id },
    });

    if (response.error) {
      console.error('Error deleting user profile:', response.error);
      return { error: response.error };
    }

    revalidateTag('user-profiles');
    revalidateTag(`user-profile-${id}`);
    return { data: response.data };
  } catch (error) {
    console.error('Error in deleteUserProfile:', error);
    return { error: 'Failed to delete user profile' };
  }
}

/**
 * Restore a soft-deleted user profile
 */
export async function restoreUserProfile(id: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiUserprofilesByIdRestore({
      path: { id },
    });

    if (response.error) {
      console.error('Error restoring user profile:', response.error);
      return { error: response.error };
    }

    revalidateTag('user-profiles');
    revalidateTag(`user-profile-${id}`);
    return { data: response.data };
  } catch (error) {
    console.error('Error in restoreUserProfile:', error);
    return { error: 'Failed to restore user profile' };
  }
}

// =============================================================================
// PROFILE UTILITY FUNCTIONS
// =============================================================================

/**
 * Get or create user profile for a given user ID
 * This is a convenience function that attempts to get an existing profile
 * and creates one if it doesn't exist
 */
export async function getOrCreateUserProfile(
  userId: string,
  defaultProfileData: {
    givenName: string;
    familyName: string;
    displayName: string;
    title?: string | null;
    description?: string | null;
  },
) {
  // First try to get existing profile
  const existingProfile = await getUserProfileByUserId(userId);

  if (existingProfile.data) {
    return existingProfile;
  }

  // If no profile exists, create one
  return await createUserProfile({
    userId,
    ...defaultProfileData,
  });
}

/**
 * Update user profile with optimistic concurrency control
 */
export async function updateUserProfileWithVersion(
  id: string,
  profileData: {
    givenName?: string | null;
    familyName?: string | null;
    displayName?: string | null;
    title?: string | null;
    description?: string | null;
  },
  expectedVersion: number,
) {
  return await updateUserProfile(id, profileData, expectedVersion);
}
