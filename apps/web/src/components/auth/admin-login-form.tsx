'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { signIn } from 'next-auth/react';
import { Button } from '@game-guild/ui/components';
import { Input } from '@game-guild/ui/components';
import { Label } from '@game-guild/ui/components';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components';
import { Alert, AlertDescription } from '@game-guild/ui/components';
import { Shield, AlertCircle } from 'lucide-react';
import { toast } from 'sonner';

export function AdminLoginForm() {
  const [email, setEmail] = useState('admin@gameguild.local');
  const [password, setPassword] = useState('admin123'); // Default for dev
  const [loading, setLoading] = useState(false);
  const router = useRouter();

  const handleAdminLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      console.log('Admin login attempt with NextAuth:', { email });

      // Use NextAuth.js signIn with the admin-bypass provider
      const result = await signIn('admin-bypass', {
        email,
        password,
        redirect: false, // Don't redirect automatically
      });

      if (result?.error) {
        console.error('NextAuth signIn error:', result.error);
        toast.error('Invalid admin credentials');
      } else if (result?.ok) {
        console.log('NextAuth signIn successful');
        toast.success('Admin access granted!');

        // Redirect to tenant management
        router.push('/dashboard/tenant');
      } else {
        console.error('Unexpected signIn result:', result);
        toast.error('Login failed');
      }
    } catch (error) {
      console.error('Admin login error:', error);
      toast.error('Login failed: ' + (error as Error).message);
    } finally {
      setLoading(false);
    }
  };

  const handleQuickAdminLogin = async () => {
    setLoading(true);
    try {
      console.log('Quick admin login with NextAuth...');

      // Use NextAuth.js signIn with default admin credentials
      const result = await signIn('admin-bypass', {
        email: 'admin@gameguild.local',
        password: 'admin123',
        redirect: false,
      });

      if (result?.error) {
        console.error('Quick admin login error:', result.error);
        toast.error('Quick login failed');
      } else if (result?.ok) {
        toast.success('Quick admin access granted!');
        router.push('/dashboard/tenant');
      } else {
        toast.error('Quick login failed');
      }
    } catch (error) {
      console.error('Quick admin login error:', error);
      toast.error('Quick login failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card className="w-full max-w-md mx-auto">
      <CardHeader className="text-center">
        <CardTitle className="flex items-center justify-center gap-2">
          <Shield className="h-5 w-5" />
          Admin Login
        </CardTitle>
        <CardDescription>Development login for super admin access</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <Alert>
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>This is a development feature for testing admin functionality.</AlertDescription>
        </Alert>

        <form onSubmit={handleAdminLogin} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="admin@gameguild.local" required />
          </div>

          <div className="space-y-2">
            <Label htmlFor="password">Password (Dev)</Label>
            <Input id="password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="admin123" required />
          </div>

          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? 'Signing in...' : 'Sign in as Admin'}
          </Button>
        </form>

        <div className="relative">
          <div className="absolute inset-0 flex items-center">
            <span className="w-full border-t" />
          </div>
          <div className="relative flex justify-center text-xs uppercase">
            <span className="bg-background px-2 text-muted-foreground">Or</span>
          </div>
        </div>

        <Button variant="outline" onClick={handleQuickAdminLogin} disabled={loading} className="w-full">
          <Shield className="h-4 w-4 mr-2" />
          Quick Super Admin Login
        </Button>
      </CardContent>
    </Card>
  );
}
