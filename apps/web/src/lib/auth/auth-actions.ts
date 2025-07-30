'use server';

import { signIn, signOut } from '@/auth';
import { AuthError } from 'next-auth';

/**
 * Server action for Google sign-in
 */
export async function googleSignInAction() {
  try {
    await signIn('google');
  } catch (error) {
    // Handle known NextAuth errors
    if (error instanceof AuthError) {
      switch (error.type) {
        case 'OAuthSignInError':
          throw new Error('OAuth sign-in failed');
        case 'OAuthCallbackError':
          throw new Error('OAuth callback error');
        case 'AccessDenied':
          throw new Error('Access denied');
        case 'OAuthAccountNotLinked':
          throw new Error('Email already in use with different provider');
        default:
          throw new Error('Authentication error occurred');
      }
    }
    // Re-throw other errors
    throw error;
  }
}

/**
 * Server action for sign-out
 */
export async function signOutAction() {
  try {
    await signOut();
  } catch (error) {
    console.error('Sign-out error:', error);
    throw error;
  }
}

/**
 * Server action for sign-in with redirect URL
 */
export async function signInWithRedirectAction(provider: string, redirectTo?: string) {
  try {
    await signIn(provider, { redirectTo });
  } catch (error) {
    if (error instanceof AuthError) {
      switch (error.type) {
        case 'OAuthSignInError':
          throw new Error('OAuth sign-in failed');
        case 'OAuthCallbackError':
          throw new Error('OAuth callback error');
        case 'AccessDenied':
          throw new Error('Access denied');
        case 'OAuthAccountNotLinked':
          throw new Error('Email already in use with different provider');
        default:
          throw new Error('Authentication error occurred');
      }
    }
    throw error;
  }
}
