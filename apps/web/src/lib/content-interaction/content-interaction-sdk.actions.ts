'use server';

import { revalidateTag } from 'next/cache';
import {
  postApiContentinteractionStart,
  putApiContentinteractionByInteractionIdProgress,
  postApiContentinteractionByInteractionIdSubmit,
  postApiContentinteractionByInteractionIdComplete,
  getApiContentinteractionUserByProgramUserIdContentByContentId,
  getApiContentinteractionUserByProgramUserId,
  putApiContentinteractionByInteractionIdTimeSpent,
} from '@/lib/api/generated/sdk.gen';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import type {
  PostApiContentinteractionStartData,
  PutApiContentinteractionByInteractionIdProgressData,
  PostApiContentinteractionByInteractionIdSubmitData,
  PostApiContentinteractionByInteractionIdCompleteData,
  GetApiContentinteractionUserByProgramUserIdContentByContentIdData,
  GetApiContentinteractionUserByProgramUserIdData,
  PutApiContentinteractionByInteractionIdTimeSpentData,
} from '@/lib/api/generated/types.gen';

// =============================================================================
// CONTENT INTERACTION MANAGEMENT
// =============================================================================

/**
 * Start a content interaction
 */
export async function startContentInteraction(data?: PostApiContentinteractionStartData) {
  await configureAuthenticatedClient();
  
  const result = await postApiContentinteractionStart({
    body: data?.body,
  });
  
  // Revalidate content interactions cache
  revalidateTag('content-interactions');
  
  return result;
}

/**
 * Update content interaction progress
 */
export async function updateContentInteractionProgress(data: PutApiContentinteractionByInteractionIdProgressData) {
  await configureAuthenticatedClient();
  
  const result = await putApiContentinteractionByInteractionIdProgress({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate content interactions cache
  revalidateTag('content-interactions');
  
  return result;
}

/**
 * Submit content interaction
 */
export async function submitContentInteraction(data: PostApiContentinteractionByInteractionIdSubmitData) {
  await configureAuthenticatedClient();
  
  const result = await postApiContentinteractionByInteractionIdSubmit({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate content interactions cache
  revalidateTag('content-interactions');
  
  return result;
}

/**
 * Complete content interaction
 */
export async function completeContentInteraction(data: PostApiContentinteractionByInteractionIdCompleteData) {
  await configureAuthenticatedClient();
  
  const result = await postApiContentinteractionByInteractionIdComplete({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate content interactions cache
  revalidateTag('content-interactions');
  
  return result;
}

/**
 * Update time spent on content interaction
 */
export async function updateContentInteractionTimeSpent(data: PutApiContentinteractionByInteractionIdTimeSpentData) {
  await configureAuthenticatedClient();
  
  const result = await putApiContentinteractionByInteractionIdTimeSpent({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate content interactions cache
  revalidateTag('content-interactions');
  
  return result;
}

// =============================================================================
// CONTENT INTERACTION RETRIEVAL
// =============================================================================

/**
 * Get content interactions for a specific user and content
 */
export async function getContentInteractionByUserAndContent(data: GetApiContentinteractionUserByProgramUserIdContentByContentIdData) {
  await configureAuthenticatedClient();
  
  return getApiContentinteractionUserByProgramUserIdContentByContentId({
    path: data.path,
  });
}

/**
 * Get all content interactions for a program user
 */
export async function getContentInteractionsByUser(data: GetApiContentinteractionUserByProgramUserIdData) {
  await configureAuthenticatedClient();
  
  return getApiContentinteractionUserByProgramUserId({
    path: data.path,
  });
}
