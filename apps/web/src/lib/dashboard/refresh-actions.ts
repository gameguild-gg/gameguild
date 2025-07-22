'use server';

import { revalidateTag } from 'next/cache';
import { refreshUserStatistics } from '@/lib/users/users.actions';
import { refreshRecentActivity } from '@/lib/feed/feed.actions';

/**
 * Server action to refresh all dashboard data
 */
export async function refreshDashboardData(): Promise<{ success: boolean; message: string }> {
  try {
    // Call individual refresh functions
    const [userStatsResult, activityResult] = await Promise.all([
      refreshUserStatistics(),
      refreshRecentActivity(),
    ]);
    
    // Also revalidate general cache tags
    revalidateTag('users');
    revalidateTag('posts');
    
    const allSuccessful = userStatsResult.success && activityResult.success;
    
    return {
      success: allSuccessful,
      message: allSuccessful 
        ? 'Dashboard data refreshed successfully'
        : 'Partial refresh completed',
    };
  } catch (error) {
    console.error('Error refreshing dashboard data:', error);
    return {
      success: false,
      message: 'Failed to refresh dashboard data',
    };
  }
}
