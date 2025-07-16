'use client';

import { Button } from '@game-guild/ui/components';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components';
import { AlertTriangle, BarChart3, Home, RefreshCw } from 'lucide-react';

interface DashboardErrorProps {
  error: Error & { digest?: string };
  reset: () => void;
}

export default function DashboardError({ error, reset }: DashboardErrorProps) {
  const isDevelopment = process.env.NODE_ENV === 'development';

  return (
    <div className="container mx-auto px-4 py-8">
      <Card className="max-w-2xl mx-auto">
        <CardHeader className="text-center">
          <div className="mx-auto mb-4 h-12 w-12 rounded-full bg-red-100 p-3">
            <BarChart3 className="h-6 w-6 text-red-600" />
          </div>
          <CardTitle className="text-2xl font-bold text-red-700">Dashboard Error</CardTitle>
          <p className="text-gray-600">Something went wrong while loading your dashboard</p>
        </CardHeader>
        <CardContent className="space-y-6">
          {isDevelopment && (
            <div className="rounded-lg bg-red-50 border border-red-200 p-4">
              <div className="flex items-start space-x-3">
                <AlertTriangle className="h-5 w-5 text-red-500 mt-0.5 flex-shrink-0" />
                <div className="space-y-2">
                  <h4 className="font-medium text-red-800">Debug Information</h4>
                  <p className="text-sm text-red-700 font-mono">{error.message}</p>
                  {error.digest && <p className="text-xs text-red-600">Error ID: {error.digest}</p>}
                </div>
              </div>
            </div>
          )}

          <div className="flex flex-col sm:flex-row gap-3">
            <Button onClick={reset} className="flex-1">
              <RefreshCw className="mr-2 h-4 w-4" />
              Retry Dashboard
            </Button>
            <Button variant="outline" onClick={() => (window.location.href = '/')} className="flex-1">
              <Home className="mr-2 h-4 w-4" />
              Go Home
            </Button>
          </div>

          <p className="text-sm text-gray-500 text-center">If this error persists, please contact support with error ID: {error.digest || 'N/A'}</p>
        </CardContent>
      </Card>
    </div>
  );
}
