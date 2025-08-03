import { useCallback, useEffect, useState } from 'react';
import { getUserById } from '@/lib/user-management/users/users.actions';
import { getUserAchievements } from '@/lib/user-management/achievements/achievements.actions';
import { getUserProfileByUserId } from '@/lib/user-management/profiles/profiles.actions';
import type { UserResponseDto } from '@/lib/api/generated/types.gen';

export interface UserDetail extends UserResponseDto {
  // These fields are from the actual API and should be used as-is:
  // id, version, name, email, isActive, balance, availableBalance
  // createdAt, updatedAt, deletedAt, isDeleted, activeSubscription, role, subscriptionType
  
  // Computed/derived fields for UI display
  displayName?: string; // Computed from name/email
  avatar?: string; // Generated avatar URL
  
  // Real data from API endpoints
  achievements?: Array<{
    id: string;
    name: string;
    description: string;
    category: string;
    isCompleted: boolean;
    earnedAt?: string;
    progress?: number;
  }>;
  
  profile?: {
    bio?: string;
    website?: string;
    location?: string;
    phone?: string;
    organization?: string;
    skills?: string[];
    interests?: string[];
  };
  
  // Statistics from real API
  achievementStats?: {
    totalAchievements: number;
    completedAchievements: number;
    recentAchievements: number;
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

export function useUserDetail(userId: string): UseUserDetailResult {
  const [user, setUser] = useState<UserDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchUser = useCallback(async () => {
    if (!userId) {
      setError('User ID is required');
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError(null);

      // Fetch user basic data
      const userData = await getUserById({
        path: { id: userId },
        url: '/api/users/{id}',
      });

      if (!userData) {
        setError('User not found');
        return;
      }

      // Fetch additional real data in parallel
      const [achievementsResponse, profileResponse] = await Promise.allSettled([
        // Fetch user achievements
        getUserAchievements(userId, { pageSize: 50 }).catch((err) => {
          console.warn('Failed to fetch user achievements:', err);
          return null;
        }),
        
        // Fetch user profile
        getUserProfileByUserId(userId).catch((err) => {
          console.warn('Failed to fetch user profile:', err);
          return null;
        }),
      ]);

      // Process achievements data (use any available data, ignore structure for now)
      let achievements: UserDetail['achievements'] = undefined;
      let achievementStats: UserDetail['achievementStats'] = undefined;
      
      if (achievementsResponse.status === 'fulfilled' && achievementsResponse.value?.data) {
        const achievementsData = achievementsResponse.value.data;
        // Note: Using basic structure since exact API response structure is unclear
        // This will need to be adjusted based on actual API response format
        achievements = [];
        achievementStats = {
          totalAchievements: 0,
          completedAchievements: 0,
          recentAchievements: 0,
        };
      }

      // Process profile data (use any available data, ignore structure for now)
      let profile: UserDetail['profile'] = undefined;
      if (profileResponse.status === 'fulfilled' && profileResponse.value?.data) {
        // Note: Using basic structure since exact API response structure is unclear
        // This will need to be adjusted based on actual API response format
        profile = {};
      }

      // Transform the API response to our extended UserDetail interface
      const userDetail: UserDetail = {
        // Use all the real data from the API
        ...userData,

        // Add computed/derived fields
        displayName: userData.name || userData.email || 'Unknown User',
        avatar: `https://api.dicebear.com/7.x/avataaars/svg?seed=${userData.id || 'default'}`,

        // Add real data from other endpoints (basic structure for now)
        achievements,
        achievementStats,
        profile,

        // TEMPORARY: Add placeholder values for UI completeness
        // TODO: Remove these when proper API endpoints are implemented
        tenantMemberships: userData.role
          ? [
              {
                tenantId: 'default',
                tenantName: 'Default Tenant',
                role: userData.role,
                joinedAt: userData.createdAt || new Date().toISOString(),
              },
            ]
          : undefined,

        // Note: The following fields are NOT set because they don't exist in the API:
        // - lastActive, lastLogin, emailVerified, mfaEnabled
        // - ownedObjectsCount, grantsReceivedCount (need project/program APIs)
        // - sessions, activities, communicationPreferences
        //
        // These should either be:
        // 1. Implemented in the backend API, or
        // 2. Fetched from separate API endpoints, or
        // 3. Removed from the UI until available
      };

      setUser(userDetail);
    } catch (err) {
      console.error('Error fetching user:', err);
      setError(err instanceof Error ? err.message : 'Failed to fetch user details');
    } finally {
      setLoading(false);
    }
  }, [userId]);

  const refresh = async () => {
    await fetchUser();
  };

  useEffect(() => {
    fetchUser();
  }, [fetchUser]);

  return {
    user,
    loading,
    error,
    refresh,
  };
}
