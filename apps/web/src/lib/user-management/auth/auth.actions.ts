'use server';

import { revalidateTag } from 'next/cache';
import {
  postApiAuthSignup,
  postApiAuthSignin,
  postApiAuthGoogleIdToken,
  postApiAuthRefresh,
  postApiAuthRevoke,
  getApiAuthProfile,
} from '@/lib/api/generated/sdk.gen';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import type {
  PostApiAuthSignupData,
  PostApiAuthSigninData,
  PostApiAuthGoogleIdTokenData,
  PostApiAuthRefreshData,
  PostApiAuthRevokeData,
  GetApiAuthProfileData,
} from '@/lib/api/generated/types.gen';

// =============================================================================
// AUTHENTICATION ACTIONS
// =============================================================================

/**
 * Sign up a new user
 */
export async function signUpUser(data?: PostApiAuthSignupData) {
  // Note: Signup typically doesn't require authentication
  const result = await postApiAuthSignup({
    body: data?.body,
  });
  
  // Revalidate auth cache
  revalidateTag('auth');
  
  return result;
}

/**
 * Sign in an existing user
 */
export async function signInUser(data?: PostApiAuthSigninData) {
  // Note: Signin typically doesn't require authentication
  const result = await postApiAuthSignin({
    body: data?.body,
  });
  
  // Revalidate auth cache
  revalidateTag('auth');
  
  return result;
}

/**
 * Sign in with Google ID token
 */
export async function signInWithGoogleIdToken(data?: PostApiAuthGoogleIdTokenData) {
  // Note: Google signin typically doesn't require authentication
  const result = await postApiAuthGoogleIdToken({
    body: data?.body,
  });
  
  // Revalidate auth cache
  revalidateTag('auth');
  
  return result;
}

/**
 * Refresh authentication token
 */
export async function refreshAuthToken(data?: PostApiAuthRefreshData) {
  // Note: Refresh typically uses refresh token, not access token
  const result = await postApiAuthRefresh({
    body: data?.body,
  });
  
  // Revalidate auth cache
  revalidateTag('auth');
  
  return result;
}

/**
 * Revoke authentication token
 */
export async function revokeAuthToken(data?: PostApiAuthRevokeData) {
  await configureAuthenticatedClient();
  
  const result = await postApiAuthRevoke({
    body: data?.body,
  });
  
  // Revalidate auth cache
  revalidateTag('auth');
  
  return result;
}

/**
 * Get current user profile
 */
export async function getAuthProfile(data?: GetApiAuthProfileData) {
  await configureAuthenticatedClient();
  
  return getApiAuthProfile({
    query: data?.query,
  });
}
