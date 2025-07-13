'use client';

import { useSearchParams } from 'next/navigation';
import { AlertCircle, ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

export default function AuthErrorPage() {
  const searchParams = useSearchParams();
  const error = searchParams.get('error');

  const getErrorMessage = (errorCode: string | null) => {
    switch (errorCode) {
      case 'AccessDenied':
        return {
          title: 'Access Denied',
          description: 'You do not have permission to sign in. This could be due to account restrictions or domain policies.',
          suggestions: [
            'Make sure you are using the correct account',
            'Contact your administrator if this is a work account',
            'Try using a different authentication method',
          ],
        };
      case 'Configuration':
        return {
          title: 'Configuration Error',
          description: 'There is a problem with the authentication setup.',
          suggestions: ['Please contact support for assistance'],
        };
      case 'Verification':
        return {
          title: 'Verification Required',
          description: 'Your account needs to be verified before you can sign in.',
          suggestions: ['Check your email for a verification link', 'Contact support if you need help'],
        };
      default:
        return {
          title: 'Authentication Error',
          description: 'An unexpected error occurred during sign-in.',
          suggestions: ['Try signing in again', 'Contact support if the problem persists'],
        };
    }
  };

  const errorInfo = getErrorMessage(error);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <Card className="max-w-md w-full">
        <CardHeader className="text-center">
          <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-red-100 mb-4">
            <AlertCircle className="h-6 w-6 text-red-600" />
          </div>
          <CardTitle className="text-2xl font-bold text-gray-900">{errorInfo.title}</CardTitle>
          <CardDescription className="text-gray-600">{errorInfo.description}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {error && (
            <div className="bg-gray-100 rounded-md p-3">
              <p className="text-sm text-gray-600">
                <span className="font-medium">Error Code:</span> {error}
              </p>
            </div>
          )}

          <div>
            <h3 className="font-medium text-gray-900 mb-2">What you can try:</h3>
            <ul className="text-sm text-gray-600 space-y-1">
              {errorInfo.suggestions.map((suggestion, index) => (
                <li key={index} className="flex items-start">
                  <span className="mr-2">•</span>
                  <span>{suggestion}</span>
                </li>
              ))}
            </ul>
          </div>

          <div className="flex space-x-3">
            <Button asChild className="flex-1">
              <Link href="/sign-in">
                <ArrowLeft className="h-4 w-4 mr-2" />
                Try Again
              </Link>
            </Button>
            <Button variant="outline" asChild className="flex-1">
              <Link href="/">Go Home</Link>
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
