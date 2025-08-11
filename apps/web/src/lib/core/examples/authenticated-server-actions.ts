'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { getTestingRequests as getTestingRequestsApi, getTestingRequestsById } from '@/lib/api/generated';
import { revalidateTag } from 'next/cache';

// Example 1: Simple GET request with default client
export async function getTestingRequests() {
  // Configure the client with authentication
  await configureAuthenticatedClient();

  try {
    // Now you can use the SDK functions directly without passing options
    const response = await getTestingRequestsApi();
    return response.data || [];
  } catch (error) {
    console.error('Error fetching testing requests:', error);
    throw new Error('Failed to fetch testing requests');
  }
}

// Example 2: Simple authenticated request with parameters
export async function getUserTestingRequests() {
  await configureAuthenticatedClient();

  try {
    // Use the SDK function directly - the client is already configured
    const response = await getTestingRequestsApi({
      query: { skip: 0, take: 10 }, // Use valid query parameters
    });

    return response.data || [];
  } catch (error) {
    console.error('Error fetching user testing requests:', error);
    throw new Error('Failed to fetch user testing requests');
  }
}

// Example 3: Get single item with caching
export async function getTestingRequestById(id: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequestsById({
      path: { id },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching testing request:', error);
    throw new Error('Failed to fetch testing request');
  }
}

// Example 4: Server action that returns state for useActionState
export async function deleteTestingRequest(prevState: { success?: boolean; error?: string }, formData: FormData) {
  await configureAuthenticatedClient();

  const id = formData.get('id') as string;

  if (!id) {
    return { success: false, error: 'ID is required' };
  }

  try {
    // Your delete API call would go here
    // await deleteTestingRequestsById({ path: { id } });

    revalidateTag('testing-requests');

    return { success: true, message: 'Testing request deleted successfully' };
  } catch (error) {
    console.error('Error deleting testing request:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}
