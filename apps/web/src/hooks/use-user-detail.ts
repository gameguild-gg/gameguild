import type { UserResponseDto } from '@/lib/api/generated/types.gen';
import type { User } from '@/lib/api/users';
import { getUserByUsername } from '@/lib/api/users';
import { getUserProfileByUserId } from '@/lib/user-management/profiles/profiles.actions';
import { getUserById } from '@/lib/user-management/users/users.actions';
import { useCallback, useEffect, useState } from 'react';

export enum UserIdentifierType {
  ID = 'id',
  USERNAME = 'username'
}

export interface UserDetail extends UserResponseDto {
  // These fields are from the actual API and should be used as-is:
  // id, version, name, email, isActive, balance, availableBalance
  // createdAt, updatedAt, deletedAt, isDeleted, activeSubscription, role, subscriptionType

  // Computed/derived fields for UI display
  displayName?: string; // Computed from name/email
  avatar?: string; // Generated avatar URL

  // Real data from API endpoints
  profile?: {
    bio?: string;
    website?: string;
    location?: string;
    phone?: string;
    organization?: string;
    skills?: string[];
    interests?: string[];
  };

  // Extended fields that don't exist in API yet - marked as placeholders
  // TODO: These should be implemented in backend API or removed from UI
  lastActive?: string; // PLACEHOLDER - not available in API
  lastLogin?: string; // PLACEHOLDER - not available in API
  emailVerified?: boolean; // PLACEHOLDER - not available in API
  mfaEnabled?: boolean; // PLACEHOLDER - not available in API
  ownedObjectsCount?: number; // PLACEHOLDER - needs separate API call for projects/programs
  grantsReceivedCount?: number; // PLACEHOLDER - needs separate API call

  // Complex data that requires separate API endpoints - TODO: implement these
  tenantMemberships?: Array<{
    tenantId: string;
    tenantName: string;
    role: string;
    joinedAt: string;
  }>; // PLACEHOLDER - needs separate API endpoint

  sessions?: Array<{
    id: string;
    deviceInfo: string;
    location: string;
    lastActive: string;
    isActive: boolean;
  }>; // PLACEHOLDER - needs separate sessions API endpoint

  activities?: Array<{
    id: string;
    type: string;
    description: string;
    timestamp: string;
    metadata?: Record<string, unknown>;
  }>; // PLACEHOLDER - needs separate activity log API endpoint

  communicationPreferences?: {
    email: boolean;
    push: boolean;
    sms: boolean;
    marketing: boolean;
  }; // PLACEHOLDER - needs separate preferences API endpoint
}

interface UseUserDetailResult {
  user: UserDetail | null;
  loading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
}

export function useUserDetail(identifierType: UserIdentifierType, identifier: string): UseUserDetailResult {
  const [user, setUser] = useState<UserDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Shared utility to fetch additional user data
  const fetchAdditionalUserData = async (userId: string) => {
    const [profileResult] = await Promise.allSettled([
      getUserProfileByUserId(userId)
    ]);

    // Process profile
    let profile: UserDetail['profile'];
    if (profileResult.status === 'fulfilled' && profileResult.value.data) {
      const profileData = profileResult.value.data;
      profile = {
        bio: profileData.description || undefined,
        website: undefined, // Not available in UserProfileResponseDto
        location: undefined, // Not available in UserProfileResponseDto
        phone: undefined, // Not available in UserProfileResponseDto
        organization: undefined, // Not available in UserProfileResponseDto
        skills: undefined, // Not available in UserProfileResponseDto
        interests: undefined // Not available in UserProfileResponseDto
      };
    }

    return { profile };
  };

  // Shared utility to create UserDetail object
  const createUserDetail = (userResponse: UserResponseDto | User, additionalData: {
    profile: UserDetail['profile'];
  }): UserDetail => {
    return {
      ...userResponse,
      // Handle different user types - UserResponseDto vs User
      subscriptionType: 'subscriptionType' in userResponse ? userResponse.subscriptionType : undefined,
      role: 'role' in userResponse ? userResponse.role : undefined,
      activeSubscription: 'activeSubscription' in userResponse ? userResponse.activeSubscription : undefined,
      username: 'username' in userResponse ? userResponse.username : undefined,
      displayName: userResponse.name || userResponse.email?.split('@')[0] || 'Unknown User',
      avatar: `https://api.dicebear.com/7.x/initials/svg?seed=${encodeURIComponent(userResponse.name || userResponse.email || 'User')}`,
      profile: additionalData.profile,

      // PLACEHOLDER fields - these are not available in the current API
      lastActive: undefined,
      lastLogin: undefined,
      emailVerified: undefined,
      mfaEnabled: undefined,
      ownedObjectsCount: undefined,
      grantsReceivedCount: undefined,
      tenantMemberships: undefined,
      sessions: undefined,
      activities: undefined,
      communicationPreferences: undefined,
    };
  };

  // Fetch user by ID
  const fetchUserById = async (userId: string): Promise<UserDetail> => {
    const userData = await getUserById({
      path: { id: userId },
    });

    if (!userData) {
      throw new Error('User not found');
    }

    const additionalData = await fetchAdditionalUserData(userData.id!);
    return createUserDetail(userData, additionalData);
  };

  // Fetch user by username
  const fetchUserByUsername = async (username: string): Promise<UserDetail> => {
    const userData = await getUserByUsername(username);

    if (!userData) {
      throw new Error('User not found');
    }

    const additionalData = await fetchAdditionalUserData(userData.id!);
    return createUserDetail(userData, additionalData);
  };

  const refresh = useCallback(async () => {
    if (!identifier) {
      setError('User identifier is required');
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError(null);

      let userData: UserDetail;
      if (identifierType === UserIdentifierType.ID) {
        userData = await fetchUserById(identifier);
      } else if (identifierType === UserIdentifierType.USERNAME) {
        userData = await fetchUserByUsername(identifier);
      } else {
        throw new Error('Invalid identifier type');
      }

      setUser(userData);
    } catch (err) {
      console.error('Error fetching user:', err);
      setError(err instanceof Error ? err.message : 'Failed to fetch user');
    } finally {
      setLoading(false);
    }
  }, [identifierType, identifier]);

  useEffect(() => {
    refresh();
  }, [identifierType, identifier]);

  return {
    user,
    loading,
    error,
    refresh,
  };
}
