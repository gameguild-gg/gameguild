'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { AlertCircle, Home, LogIn } from 'lucide-react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense } from 'react';

function AuthErrorContent() {
  const searchParams = useSearchParams();
  const error = searchParams.get('error');

  const getErrorMessage = (errorType: string | null) => {
    switch (errorType) {
      case 'AccessDenied':
        return {
          title: 'Access Denied',
          description: 'You do not have permission to sign in. This could be because your account is not authorized or there was an issue with your authentication provider.',
          suggestions: [
            'Make sure you are using the correct Google account',
            'Contact support if you believe this is an error',
            'Try signing in with a different method'
          ]
        };
      case 'OAuthSignInError':
        return {
          title: 'OAuth Sign-in Error',
          description: 'There was an error during the OAuth sign-in process.',
          suggestions: [
            'Try signing in again',
            'Check your internet connection',
            'Contact support if the issue persists'
          ]
        };
      case 'OAuthCallbackError':
        return {
          title: 'OAuth Callback Error',
          description: 'There was an error processing the OAuth callback.',
          suggestions: [
            'Try signing in again',
            'Clear your browser cache and cookies',
            'Contact support if the issue persists'
          ]
        };
      case 'OAuthAccountNotLinked':
        return {
          title: 'Account Not Linked',
          description: 'This email is already associated with another account using a different sign-in method.',
          suggestions: [
            'Try signing in with your original method',
            'Contact support to link your accounts',
            'Use a different email address'
          ]
        };
      default:
        return {
          title: 'Authentication Error',
          description: 'An unexpected error occurred during authentication.',
          suggestions: [
            'Try signing in again',
            'Check your internet connection',
            'Contact support if the issue persists'
          ]
        };
    }
  };

  const errorInfo = getErrorMessage(error);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-900 via-purple-900 to-slate-900 p-4">
      <Card className="w-full max-w-md bg-slate-800/50 border-slate-700 backdrop-blur-sm">
        <CardHeader className="text-center">
          <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-500/10">
            <AlertCircle className="h-6 w-6 text-red-500" />
          </div>
          <CardTitle className="text-xl font-semibold text-white">
            {errorInfo.title}
          </CardTitle>
          <CardDescription className="text-slate-300">
            {errorInfo.description}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <h4 className="text-sm font-medium text-slate-200">What you can try:</h4>
            <ul className="space-y-1 text-sm text-slate-400">
              {errorInfo.suggestions.map((suggestion, index) => (
                <li key={index} className="flex items-start">
                  <span className="mr-2 mt-1.5 h-1 w-1 rounded-full bg-slate-500 flex-shrink-0" />
                  {suggestion}
                </li>
              ))}
            </ul>
          </div>
          
          <div className="flex flex-col gap-2 pt-4">
            <Button asChild className="w-full">
              <Link href="/sign-in">
                <LogIn className="mr-2 h-4 w-4" />
                Try Again
              </Link>
            </Button>
            <Button variant="outline" asChild className="w-full border-slate-600 text-slate-300 hover:bg-slate-700">
              <Link href="/">
                <Home className="mr-2 h-4 w-4" />
                Go Home
              </Link>
            </Button>
          </div>
          
          {error && (
            <div className="mt-4 p-3 bg-slate-900/50 rounded-md border border-slate-700">
              <p className="text-xs text-slate-500">
                Error code: <code className="text-slate-400">{error}</code>
              </p>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

export default function AuthErrorPage() {
  return (
    <Suspense fallback={
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-900 via-purple-900 to-slate-900">
        <div className="text-white">Loading...</div>
      </div>
    }>
      <AuthErrorContent />
    </Suspense>
  );
}