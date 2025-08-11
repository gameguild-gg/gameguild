'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Link } from '@/i18n/navigation';
import { signInWithGoogle } from '@/lib/auth/auth.actions';
import { signInWithEmailAndPassword } from '@/lib/auth/auth.actions';
import { cn } from '@/lib/utils';
import { useSearchParams } from 'next/navigation';
import React, { ComponentPropsWithoutRef, useState } from 'react';

// import { useAuthError } from '@/lib/hooks/useAuthError';

// import { googleSignInAction } from '@/lib/auth/auth-actions';

export const SignInForm = ({ className, ...props }: ComponentPropsWithoutRef<'div'>) => {
  // const { hasError, error } = useAuthError();
  const searchParams = useSearchParams();
  const [loading, setLoading] = useState(false);
  const [emailPasswordLoading, setEmailPasswordLoading] = useState(false);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);

  // Check for authentication errors from URL params
  const authError = searchParams.get('error');

  const handleGoogleSignIn = async () => {
    setLoading(true);
    try {
      await signInWithGoogle();
    } catch (error) {
      console.error('Sign-in error:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleEmailPasswordSignIn = async (e: React.FormEvent) => {
    e.preventDefault();
    setEmailPasswordLoading(true);
    setError(null);

    try {
      await signInWithEmailAndPassword(email, password);
      // Successful sign-in will redirect automatically
    } catch (error) {
      console.error('Email/password sign-in error:', error);
      setError(error instanceof Error ? error.message : 'Sign-in failed. Please check your credentials.');
    } finally {
      setEmailPasswordLoading(false);
    }
  };

  return (
    <div className={cn('flex flex-col w-full max-w-sm gap-6', className)} {...props}>
      {/* Decorative background elements */}
      <div className="relative">
        {/* Glow effects */}
        <div className="absolute -top-20 -left-20 w-40 h-40 rounded-full bg-gradient-to-br from-blue-500/20 via-purple-500/10 to-transparent blur-3xl"></div>
        <div className="absolute -bottom-20 -right-20 w-32 h-32 rounded-full bg-gradient-to-br from-purple-500/20 via-blue-500/10 to-transparent blur-2xl"></div>

        <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-600/70 shadow-lg hover:shadow-xl transition-all duration-300 relative overflow-hidden">
          <CardHeader className="text-center relative z-10">
            <CardTitle className="text-2xl font-bold text-white">Welcome back</CardTitle>
            <CardDescription className="text-slate-400">Sign-in with your email and password</CardDescription>
          </CardHeader>
          <CardContent className="relative z-10">
            <div className="grid gap-6">
              {/* Show authentication errors */}
              {(error || authError) && (
                <div className="bg-red-500/15 text-red-400 text-sm p-3 rounded-lg border border-red-500/30 backdrop-blur-sm">
                  {error ||
                    (authError === 'OAuthAccountNotLinked' && 'Email already in use with different provider.') ||
                    (authError === 'AccessDenied' && 'Access denied. You may not have permission to sign in.') ||
                    (authError && 'An authentication error occurred.')}
                </div>
              )}

              <form onSubmit={handleEmailPasswordSignIn}>
                <div className="grid gap-6">
                  <div className="grid gap-2">
                    <Label htmlFor="email" className="text-slate-300">
                      Email
                    </Label>
                    <Input
                      id="email"
                      type="email"
                      placeholder="admin@gameguild.local"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      required
                      className="bg-slate-800/50 border-slate-600 text-white placeholder-slate-400 focus:border-blue-400 transition-colors"
                    />
                  </div>
                  <div className="grid gap-2">
                    <Label htmlFor="password" className="text-slate-300">
                      Password
                    </Label>
                    <Input
                      id="password"
                      type="password"
                      placeholder="admin123"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      required
                      className="bg-slate-800/50 border-slate-600 text-white placeholder-slate-400 focus:border-purple-400 transition-colors"
                    />
                    <div className="flex justify-end">
                      <Link href="#" className="text-sm text-slate-400 hover:text-blue-400 transition-colors underline-offset-4 hover:underline">
                        Forgot your password?
                      </Link>
                    </div>
                  </div>
                  <Button type="submit" className="w-full bg-slate-700 hover:bg-slate-600 text-white border-slate-600 shadow-lg hover:shadow-xl transition-all duration-300 hover:scale-[1.02]" disabled={emailPasswordLoading}>
                    {emailPasswordLoading ? (
                      <>
                        <div className="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                        Signing in...
                      </>
                    ) : (
                      'Sign in'
                    )}
                  </Button>
                </div>
              </form>

              <div className="relative text-center text-sm after:absolute after:inset-0 after:top-1/2 after:z-0 after:flex after:items-center after:border-t after:border-slate-600">
                <span className="relative z-10 bg-gradient-to-r from-slate-800 to-slate-900 rounded-full px-4 text-slate-400">Or continue with</span>
              </div>

              <div className="flex flex-col gap-4">
                <Button
                  variant="outline"
                  className="w-full bg-slate-800/30 border-slate-600 text-white hover:bg-slate-700/50 hover:border-slate-500 transition-all duration-300 hover:scale-[1.02]"
                  onClick={handleGoogleSignIn}
                  disabled={loading}
                >
                  {loading ? (
                    <div className="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                  ) : (
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" className="size-5 mr-2" aria-hidden="true">
                      <path
                        d="M12.0003 4.75C13.7703 4.75 15.3553 5.36002 16.6053 6.54998L20.0303 3.125C17.9502 1.19 15.2353 0 12.0003 0C7.31028 0 3.25527 2.69 1.28027 6.60998L5.27028 9.70498C6.21525 6.86002 8.87028 4.75 12.0003 4.75Z"
                        fill="#EA4335"
                      />
                      <path d="M23.49 12.275C23.49 11.49 23.415 10.73 23.3 10H12V14.51H18.47C18.18 15.99 17.34 17.25 16.08 18.1L19.945 21.1C22.2 19.01 23.49 15.92 23.49 12.275Z" fill="#4285F4" />
                      <path
                        d="M5.26498 14.2949C5.02498 13.5699 4.88495 12.7999 4.88495 11.9999C4.88495 11.1999 5.01998 10.4299 5.26498 9.7049L1.27496 6.60986C0.45996 8.22986 0 10.0599 0 11.9999C0 13.9399 0.45996 15.7699 1.27996 17.3899L5.26498 14.2949Z"
                        fill="#FBBC05"
                      />
                      <path
                        d="M12.0001 24C15.2401 24 17.9651 22.935 19.9451 21.095L16.0801 18.095C15.0051 18.82 13.6201 19.245 12.0001 19.245C8.8701 19.245 6.21506 17.135 5.26506 14.29L1.27502 17.385C3.25502 21.31 7.3101 24 12.0001 24Z"
                        fill="#34A853"
                      />
                    </svg>
                  )}
                  {loading ? 'Signing in...' : 'Sign in with Google'}
                </Button>
              </div>

              <div className="text-center text-sm">
                <span className="text-slate-400">Don&apos;t have an account?</span>{' '}
                <Link href="/sign-up" className="text-blue-400 hover:text-blue-300 transition-colors underline underline-offset-4">
                  Sign up
                </Link>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="text-balance text-center text-xs text-slate-400">
        By clicking continue, you agree to our{' '}
        <Link href="/terms-of-service" className="text-blue-400 hover:text-blue-300 transition-colors underline underline-offset-4">
          Terms of Service
        </Link>{' '}
        and{' '}
        <Link href="/polices/privacy" className="text-blue-400 hover:text-blue-300 transition-colors underline underline-offset-4">
          Privacy Policy
        </Link>
        .
      </div>
    </div>
  );
};
