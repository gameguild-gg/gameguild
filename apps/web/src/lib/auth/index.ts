export { signInWithGoogle } from './auth.actions';
export type { RefreshTokenResponse, SignInResponse } from './auth.types';
// Re-export NextAuth helpers for consistency so importing from '@/lib/auth' works.
export { auth, signIn, signOut } from '@/auth';

