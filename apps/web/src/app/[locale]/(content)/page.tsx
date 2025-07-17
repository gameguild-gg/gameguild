'use client';

import React, { useEffect, useState } from 'react';
import { signIn, signOut, useSession } from 'next-auth/react';
import { useTenant } from '@/lib/tenant/tenant-provider';
import { TenantSelector } from '@/components/auth/tenant-selector';
import { TenantService } from '@/lib/services/tenant.service';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Separator } from '@game-guild/ui/components/separator';
import { environment } from '@/configs/environment';

export default function Page(): React.JSX.Element {
  const { data: session, status } = useSession();
  const { currentTenant, availableTenants } = useTenant();

  const [testResults, setTestResults] = useState<Record<string, any>>({});
  const [loading, setLoading] = useState(false);
  const [cmsConnectivity, setCmsConnectivity] = useState<{
    status: 'unknown' | 'testing' | 'success' | 'error';
    message?: string;
    details?: any;
  }>({ status: 'unknown' });

  // Log session data for debugging
  console.log('Session on client:', session);
  console.log('Access Token:', session?.accessToken);
  console.log('Current Tenant:', currentTenant);
  console.log('Available Tenants:', availableTenants);

  const runServerActionTest = async () => {
    setLoading(true);
    try {
      console.log('Testing server action: getUserTenants');
      if (!session?.accessToken) {
        throw new Error('No access token available');
      }
      const result = await TenantService.getUserTenants(session.accessToken);
      setTestResults((prev: Record<string, any>) => ({
        ...prev,
        getUserTenants: result,
      }));
      console.log('Server action result:', result);
    } catch (error) {
      console.error('Server action error:', error);
      setTestResults((prev: Record<string, any>) => ({
        ...prev,
        getUserTenants: { error: error instanceof Error ? error.message : 'Unknown error' },
      }));
    } finally {
      setLoading(false);
    }
  };

  const runApiTest = async () => {
    if (!session?.accessToken) return;

    setLoading(true);
    try {
      console.log('Testing direct API call to CMS backend');
      const { testCmsConnection } = await import('@/lib/api/actions');
      
      const result = await testCmsConnection();

      setTestResults((prev: Record<string, any>) => ({
        ...prev,
        directApi: { status: 'success', data: result },
      }));
      console.log('Direct API result:', result);
    } catch (error) {
      console.error('Direct API error:', error);
      setTestResults((prev: Record<string, any>) => ({
        ...prev,
        directApi: { error: error instanceof Error ? error.message : 'Unknown error' },
      }));
    } finally {
      setLoading(false);
    }
  };
  // Test CMS connectivity
  const testCmsConnectivity = async () => {
    setCmsConnectivity({ status: 'testing' });

    try {
      console.log('Testing CMS connectivity to:', environment.apiBaseUrl);

      // Try multiple endpoints to test connectivity
      let response;
      let endpoint = '';

      try {
        // First try the health endpoint
        endpoint = '/health';
        response = await fetch(`${environment.apiBaseUrl}${endpoint}`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
          },
        });
      } catch (healthError) {
        // If health fails, try a public auth endpoint with OPTIONS to check if server is responding
        try {
          endpoint = '/auth/sign-in';
          response = await fetch(`${environment.apiBaseUrl}${endpoint}`, {
            method: 'OPTIONS',
            headers: {
              'Content-Type': 'application/json',
            },
          });
        } catch (authError) {
          throw new Error('Cannot reach CMS backend on any endpoint');
        }
      }

      // Handle different response scenarios
      if (response.ok) {
        const data = await response.json().catch(() => ({ status: 'ok' }));
        setCmsConnectivity({
          status: 'success',
          message: `CMS backend is reachable (${response.status}) on ${endpoint}`,
          details: data,
        });
      } else if (response.status === 401 && endpoint === '/health') {
        // 401 on health endpoint means server is running but endpoint requires auth
        setCmsConnectivity({
          status: 'success',
          message: 'CMS backend is running (health endpoint requires auth)',
          details: {
            status: response.status,
            statusText: response.statusText,
            note: 'Server is responding correctly',
          },
        });
      } else if (response.status === 405 && endpoint === '/auth/sign-in') {
        // 405 Method Not Allowed on OPTIONS means server is running
        setCmsConnectivity({
          status: 'success',
          message: 'CMS backend is running (detected via auth endpoint)',
          details: {
            status: response.status,
            statusText: response.statusText,
            note: 'Server is responding correctly',
          },
        });
      } else {
        setCmsConnectivity({
          status: 'error',
          message: `CMS backend returned ${response.status} on ${endpoint}`,
          details: { status: response.status, statusText: response.statusText },
        });
      }
    } catch (error) {
      console.error('CMS connectivity test failed:', error);
      setCmsConnectivity({
        status: 'error',
        message: 'Cannot reach CMS backend',
        details: {
          error: error instanceof Error ? error.message : 'Unknown error',
          url: environment.apiBaseUrl,
        },
      });
    }
  };

  // Test on component mount
  useEffect(() => {
    testCmsConnectivity();
  }, []);

  if (status === 'loading') {
    return (
      <div className="container mx-auto p-6">
        <Card>
          <CardContent className="pt-6">
            <p>Carregando sessão...</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (!session) {
    return (
      <div className="container mx-auto p-6 space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>CMS Backend Integration Test</CardTitle>
            <CardDescription>Teste da integração com o backend CMS - Faça login para continuar</CardDescription>
          </CardHeader>{' '}
          <CardContent className="space-y-4">
            {/* CMS Connectivity Status */}
            <div className="space-y-2">
              <h4 className="font-semibold">CMS Backend Status</h4>
              <div className="flex items-center gap-2">
                <Badge
                  variant={
                    cmsConnectivity.status === 'success'
                      ? 'default'
                      : cmsConnectivity.status === 'error'
                        ? 'destructive'
                        : cmsConnectivity.status === 'testing'
                          ? 'secondary'
                          : 'outline'
                  }
                >
                  {cmsConnectivity.status === 'testing'
                    ? 'Testing...'
                    : cmsConnectivity.status === 'success'
                      ? 'Connected'
                      : cmsConnectivity.status === 'error'
                        ? 'Disconnected'
                        : 'Unknown'}
                </Badge>
                <Button size="sm" variant="outline" onClick={testCmsConnectivity} disabled={cmsConnectivity.status === 'testing'}>
                  Test Connection
                </Button>
              </div>
              <p className="text-sm text-muted-foreground">API URL: {environment.apiBaseUrl}</p>
              {cmsConnectivity.message && <p className="text-sm">{cmsConnectivity.message}</p>}
              {cmsConnectivity.details && (
                <pre className="text-xs bg-muted p-2 rounded-md overflow-auto">{JSON.stringify(cmsConnectivity.details, null, 2)}</pre>
              )}
            </div>

            <Alert>
              <AlertDescription>Você não está logado. Faça login para testar a integração.</AlertDescription>
            </Alert>

            <div className="space-y-2">
              <Button onClick={() => signIn('google')} className="w-full">
                Fazer Login com Google
              </Button>
              <p className="text-sm text-muted-foreground text-center">Isso testará a integração NextAuth + CMS Backend</p>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="container mx-auto p-6 space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>🧪 CMS Backend Integration Test</CardTitle>
          <CardDescription>Página de teste para integração NextAuth.js + CMS Backend</CardDescription>
        </CardHeader>{' '}
        <CardContent className="space-y-6">
          {/* CMS Backend Status */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold">CMS Backend Status</h3>
            <div className="flex items-center gap-2">
              <Badge
                variant={
                  cmsConnectivity.status === 'success'
                    ? 'default'
                    : cmsConnectivity.status === 'error'
                      ? 'destructive'
                      : cmsConnectivity.status === 'testing'
                        ? 'secondary'
                        : 'outline'
                }
              >
                {cmsConnectivity.status === 'testing'
                  ? 'Testing...'
                  : cmsConnectivity.status === 'success'
                    ? 'Connected'
                    : cmsConnectivity.status === 'error'
                      ? 'Disconnected'
                      : 'Unknown'}
              </Badge>
              <Button size="sm" variant="outline" onClick={testCmsConnectivity} disabled={cmsConnectivity.status === 'testing'}>
                Test Connection
              </Button>
            </div>
            <p className="text-sm text-muted-foreground">API URL: {environment.apiBaseUrl}</p>
            {cmsConnectivity.message && <p className="text-sm">{cmsConnectivity.message}</p>}
            {cmsConnectivity.details && <pre className="text-xs bg-muted p-2 rounded-md overflow-auto">{JSON.stringify(cmsConnectivity.details, null, 2)}</pre>}
          </div>

          <Separator />

          {/* Session Info */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold">Session Information</h3>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <p>
                  <strong>User:</strong> {session.user?.name || 'N/A'}
                </p>
                <p>
                  <strong>Email:</strong> {session.user?.email || 'N/A'}
                </p>
                <p>
                  <strong>User ID:</strong> {session.user?.id || 'N/A'}
                </p>
                <p>
                  <strong>Username:</strong> {session.user?.username || 'N/A'}
                </p>
              </div>
              <div className="space-y-2">
                <p>
                  <strong>Access Token:</strong>
                  <Badge variant={session.accessToken ? 'default' : 'destructive'}>{session.accessToken ? 'Present' : 'Missing'}</Badge>
                </p>
                <p>
                  <strong>Session Error:</strong>
                  <Badge variant={(session as any).error ? 'destructive' : 'default'}>{(session as any).error || 'None'}</Badge>
                </p>
              </div>
            </div>
          </div>

          <Separator />

          {/* Tenant Context */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold">Tenant Context</h3>
            <div className="space-y-4">
              <TenantSelector />

              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <p>
                    <strong>Current Tenant:</strong>{' '}
                    {currentTenant && typeof currentTenant === 'object' && currentTenant !== null && 'name' in currentTenant
                      ? (currentTenant as any).name
                      : 'None'}
                  </p>
                  {currentTenant && typeof currentTenant === 'object' && currentTenant !== null ? (
                    <>
                      <p>
                        <strong>Tenant ID:</strong> {'id' in currentTenant ? (currentTenant as any).id : 'Unknown'}
                      </p>
                      <p>
                        <strong>Active:</strong>
                        <Badge variant={'isActive' in currentTenant && (currentTenant as any).isActive ? 'default' : 'secondary'}>
                          {'isActive' in currentTenant && (currentTenant as any).isActive ? 'Yes' : 'No'}
                        </Badge>
                      </p>
                    </>
                  ) : null}
                </div>
                <div className="space-y-2">
                  <p>
                    <strong>Available Tenants:</strong> {availableTenants.size}
                  </p>
                  {availableTenants.size > 0 && (
                    <div className="space-y-1">
                      {Array.from(availableTenants).map((tenant: any, index) => (
                        <div key={tenant?.id || index} className="text-sm">
                          • {tenant?.name || 'Unknown'} ({tenant?.isActive ? 'Active' : 'Inactive'})
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>

          <Separator />

          {/* Test Actions */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold">Backend Integration Tests</h3>
            <div className="flex gap-2 flex-wrap">
              <Button onClick={runServerActionTest} disabled={loading} variant="outline">
                {loading ? 'Testing...' : 'Test Server Action'}
              </Button>
              <Button onClick={runApiTest} disabled={loading || !session.accessToken} variant="outline">
                {loading ? 'Testing...' : 'Test Direct API'}
              </Button>
              <Button onClick={() => signOut()} variant="destructive">
                Logout
              </Button>
            </div>

            {/* Test Results */}
            {Object.keys(testResults).length > 0 && (
              <div className="space-y-4">
                <h4 className="font-semibold">Test Results:</h4>
                {Object.entries(testResults).map(([key, result]) => (
                  <Card key={key}>
                    <CardHeader>
                      <CardTitle className="text-sm">{key}</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <pre className="text-xs bg-muted p-3 rounded-md overflow-auto">{JSON.stringify(result, null, 2)}</pre>
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}
          </div>

          <Separator />

          {/* CMS Connectivity Test */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold">CMS Connectivity Test</h3>
            <div>
              <Button onClick={testCmsConnectivity} disabled={cmsConnectivity.status === 'testing'} variant="outline">
                {cmsConnectivity.status === 'testing' ? 'Testing...' : 'Test CMS Connectivity'}
              </Button>
            </div>

            {/* Connectivity Result */}
            <div className="text-sm">
              {cmsConnectivity.status === 'unknown' && <p>O teste de conectividade ainda não foi executado.</p>}
              {cmsConnectivity.status === 'testing' && <p>Testando a conectividade com o CMS backend...</p>}
              {cmsConnectivity.status === 'success' && <p className="text-green-600">✔️ {cmsConnectivity.message}</p>}
              {cmsConnectivity.status === 'error' && <p className="text-red-600">❌ {cmsConnectivity.message}</p>}
            </div>

            {/* Connectivity Details */}
            {cmsConnectivity.details && (
              <Card>
                <CardContent>
                  <pre className="text-xs bg-muted p-3 rounded-md overflow-auto">{JSON.stringify(cmsConnectivity.details, null, 2)}</pre>
                </CardContent>
              </Card>
            )}
          </div>

          <Separator />

          {/* Raw Session Data */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold">Raw Session Data (Debug)</h3>
            <pre className="text-xs bg-muted p-4 rounded-md overflow-auto">{JSON.stringify(session, null, 2)}</pre>
          </div>

          {/* Action Buttons */}
          <div className="flex gap-2">
            <Button onClick={() => (window.location.href = '/dashboard')} disabled={!session.accessToken}>
              Go to Dashboard
            </Button>
            <Button onClick={() => (window.location.href = '/sign-in')} variant="outline">
              Go to Sign In
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
