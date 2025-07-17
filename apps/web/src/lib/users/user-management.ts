/**
 * Client-safe wrapper functions for user management actions
 * This module provides client-compatible versions of user management functions
 */

'use server';

import { revalidateTag } from 'next/cache';
import { UpdateUserRequest, User } from '@/types/user';

export interface ActionState {
  success: boolean;
  error?: string;
}

export interface UserActionState extends ActionState {
  user?: User;
}

/**
 * Client-safe user creation action
 */
export async function createUserAction(prevState: ActionState, formData: FormData): Promise<UserActionState> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
    
    const userData = {
      username: formData.get('username') as string,
      email: formData.get('email') as string,
      firstName: formData.get('firstName') as string,
      lastName: formData.get('lastName') as string,
      isActive: formData.get('isActive') === 'true',
    };

    const response = await fetch(`${apiUrl}/api/users`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(userData),
    });

    if (!response.ok) {
      throw new Error(`Failed to create user: ${response.status} ${response.statusText}`);
    }

    const user = await response.json();
    
    // Revalidate cache
    revalidateTag('users');
    
    return { success: true, user };
  } catch (error) {
    console.error('Error creating user:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to create user',
    };
  }
}

/**
 * Client-safe user update action
 */
export async function updateUserAction(id: string, prevState: ActionState, formData: FormData): Promise<UserActionState> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
    
    const userData: Partial<UpdateUserRequest> = {
      username: formData.get('username') as string,
      email: formData.get('email') as string,
      firstName: formData.get('firstName') as string,
      lastName: formData.get('lastName') as string,
      isActive: formData.get('isActive') === 'true',
    };

    const response = await fetch(`${apiUrl}/api/users/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(userData),
    });

    if (!response.ok) {
      throw new Error(`Failed to update user: ${response.status} ${response.statusText}`);
    }

    const user = await response.json();
    
    // Revalidate cache
    revalidateTag('users');
    revalidateTag('user-detail');
    
    return { success: true, user };
  } catch (error) {
    console.error('Error updating user:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to update user',
    };
  }
}

/**
 * Client-safe user deletion
 */
export async function deleteUserAction(id: string): Promise<{ success: boolean; error?: string }> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    const response = await fetch(`${apiUrl}/api/users/${id}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to delete user: ${response.status} ${response.statusText}`);
    }

    // Revalidate cache
    revalidateTag('users');
    revalidateTag('user-detail');

    return { success: true };
  } catch (error) {
    console.error('Error deleting user:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to delete user',
    };
  }
}

/**
 * Client-safe user status toggle
 */
export async function toggleUserStatusAction(id: string, currentStatus: boolean): Promise<{ success: boolean; error?: string; user?: User }> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

    const response = await fetch(`${apiUrl}/api/users/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ isActive: !currentStatus }),
    });

    if (!response.ok) {
      throw new Error(`Failed to toggle user status: ${response.status} ${response.statusText}`);
    }

    const user = await response.json();
    
    // Revalidate cache
    revalidateTag('users');
    revalidateTag('user-detail');

    return { success: true, user };
  } catch (error) {
    console.error('Error toggling user status:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to toggle user status',
    };
  }
}

/**
 * Client-safe data revalidation
 */
export async function revalidateUsersDataAction(): Promise<void> {
  revalidateTag('users');
  revalidateTag('user-detail');
}
