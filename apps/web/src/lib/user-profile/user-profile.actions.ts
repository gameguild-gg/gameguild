'use server';

import { auth } from '@/auth';
import { getUserById } from '@/lib/users/users.actions';
import type { UserResponseDto } from '@/lib/api/generated/types.gen';

/**
 * Get current user's profile data
 */
export async function getCurrentUserProfile(): Promise<{ success: boolean; data?: UserResponseDto; error?: string }> {
  try {
    const session = await auth();
    
    if (!session?.user?.id) {
      return { success: false, error: 'User not authenticated' };
    }

    const result = await getUserById({
      path: { id: session.user.id },
    });

    if (!result.data) {
      return { success: false, error: 'User not found' };
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('getCurrentUserProfile error:', error);
    return { 
      success: false, 
      error: error instanceof Error ? error.message : 'Failed to fetch user profile' 
    };
  }
}