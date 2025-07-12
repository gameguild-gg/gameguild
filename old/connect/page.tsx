import { Suspense } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { signIn } from '@/auth';
import { redirect } from 'next/navigation';
import { ArrowLeft, LogIn, Shield, Star, Users } from 'lucide-react';
import Link from 'next/link';

interface ConnectPageProps {
  searchParams: {
    returnUrl?: string;
    error?: string;
  };
}

export default function ConnectPage({ searchParams }: ConnectPageProps) {
  const { returnUrl, error } = searchParams;

  const handleGoogleSignIn = async () => {
    'use server';
    await signIn('google', {
      redirectTo: returnUrl || '/dashboard',
      redirect: true,
    });
  };

  const handleAdminSignIn = async (formData: FormData) => {
    'use server';

    const email = formData.get('email') as string;
    const password = formData.get('password') as string;

    try {
      await signIn('admin-bypass', {
        email,
        password,
        redirectTo: returnUrl || '/dashboard',
        redirect: true,
      });
    } catch (error) {
      console.error('Admin sign-in error:', error);
      redirect(`/connect?error=Invalid credentials&returnUrl=${encodeURIComponent(returnUrl || '')}`);
    }
  };

  return (
    <div className="min-h-screen bg-gray-950 text-white flex items-center justify-center p-4">
      <div className="w-full max-w-md space-y-6">
        {/* Header */}
        <div className="text-center space-y-2">
          <div className="flex justify-center mb-4">
            <div className="w-16 h-16 bg-gradient-to-r from-blue-600 to-purple-600 rounded-xl flex items-center justify-center">
              <Shield className="w-8 h-8 text-white" />
            </div>
          </div>
          <h1 className="text-3xl font-bold">Welcome to Game Guild</h1>
          <p className="text-gray-400">Sign in to access your courses and continue learning</p>

          {returnUrl && (
            <div className="bg-blue-500/10 border border-blue-500/20 rounded-lg p-3">
              <p className="text-sm text-blue-300">You need to sign in to access course content</p>
            </div>
          )}

          {error && (
            <div className="bg-red-500/10 border border-red-500/20 rounded-lg p-3">
              <p className="text-sm text-red-300">{error}</p>
            </div>
          )}
        </div>

        {/* Sign In Options */}
        <div className="space-y-4">
          {/* Google Sign In */}
          <Card className="bg-gray-800 border-gray-700">
            <CardHeader>
              <CardTitle className="text-center text-lg">Quick Sign In</CardTitle>
            </CardHeader>
            <CardContent>
              <form action={handleGoogleSignIn}>
                <Button type="submit" className="w-full bg-white hover:bg-gray-100 text-black font-medium">
                  <svg className="w-5 h-5 mr-2" viewBox="0 0 24 24">
                    <path
                      fill="#4285F4"
                      d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                    />
                    <path
                      fill="#34A853"
                      d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                    />
                    <path
                      fill="#FBBC05"
                      d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                    />
                    <path
                      fill="#EA4335"
                      d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                    />
                  </svg>
                  Continue with Google
                </Button>
              </form>
            </CardContent>
          </Card>

          {/* Admin/Dev Sign In */}
          <Card className="bg-gray-800 border-gray-700">
            <CardHeader>
              <CardTitle className="text-center text-lg flex items-center justify-center">
                <Badge variant="outline" className="mr-2 border-orange-500 text-orange-400">
                  DEV
                </Badge>
                Admin Access
              </CardTitle>
            </CardHeader>
            <CardContent>
              <form action={handleAdminSignIn} className="space-y-4">
                <div>
                  <label htmlFor="email" className="block text-sm font-medium text-gray-300 mb-1">
                    Email
                  </label>
                  <input
                    type="email"
                    id="email"
                    name="email"
                    required
                    defaultValue="admin@example.com"
                    className="w-full px-3 py-2 bg-gray-700 border border-gray-600 rounded-md text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    placeholder="Enter your email"
                  />
                </div>
                <div>
                  <label htmlFor="password" className="block text-sm font-medium text-gray-300 mb-1">
                    Password
                  </label>
                  <input
                    type="password"
                    id="password"
                    name="password"
                    required
                    defaultValue="password123"
                    className="w-full px-3 py-2 bg-gray-700 border border-gray-600 rounded-md text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    placeholder="Enter your password"
                  />
                </div>
                <Button type="submit" className="w-full bg-orange-600 hover:bg-orange-700">
                  <LogIn className="w-4 h-4 mr-2" />
                  Admin Sign In
                </Button>
              </form>
              <p className="text-xs text-gray-400 mt-2 text-center">For development and testing purposes</p>
            </CardContent>
          </Card>
        </div>

        {/* Features */}
        <Card className="bg-gray-800/50 border-gray-700">
          <CardContent className="pt-6">
            <h3 className="font-semibold mb-3 text-center">What you get with an account:</h3>
            <div className="space-y-2 text-sm">
              <div className="flex items-center">
                <Star className="w-4 h-4 text-yellow-400 mr-2" />
                <span>Access to premium courses</span>
              </div>
              <div className="flex items-center">
                <Users className="w-4 h-4 text-blue-400 mr-2" />
                <span>Join our learning community</span>
              </div>
              <div className="flex items-center">
                <Shield className="w-4 h-4 text-green-400 mr-2" />
                <span>Track your progress</span>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Back Link */}
        <div className="text-center">
          <Button asChild variant="ghost" className="text-gray-400 hover:text-white">
            <Link href="/">
              <ArrowLeft className="w-4 h-4 mr-2" />
              Back to Home
            </Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
