'use client';

import React from 'react';
import { Button } from '@game-guild/ui/components';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components';
import { AlertTriangle, RefreshCw, Home, ArrowLeft } from 'lucide-react';

interface GlobalErrorProps {
  error: Error & { digest?: string };
  reset: () => void;
}

export default function GlobalError({ error, reset }: GlobalErrorProps) {
  const isDevelopment = process.env.NODE_ENV === 'development';

  return (
    <html>
      <body>
        <div className="min-h-screen bg-background flex items-center justify-center p-4">
          <Card className="w-full max-w-2xl">
            <CardHeader className="text-center">
              <div className="mx-auto w-16 h-16 bg-destructive/10 rounded-full flex items-center justify-center mb-4">
                <AlertTriangle className="h-8 w-8 text-destructive" />
              </div>
              <CardTitle className="text-2xl">Something went wrong!</CardTitle>
              <p className="text-muted-foreground">We apologize for the inconvenience. An unexpected error has occurred.</p>
            </CardHeader>
            <CardContent className="space-y-6">
              {isDevelopment && (
                <div className="bg-muted p-4 rounded-lg">
                  <h3 className="font-semibold text-sm mb-2">Error Details (Development Only):</h3>
                  <code className="text-xs text-muted-foreground break-all">{error.message}</code>
                  {error.digest && <p className="text-xs text-muted-foreground mt-2">Error ID: {error.digest}</p>}
                </div>
              )}

              <div className="flex flex-col sm:flex-row gap-3 justify-center">
                <Button onClick={reset} className="flex items-center gap-2">
                  <RefreshCw className="h-4 w-4" />
                  Try Again
                </Button>
                <Button variant="outline" onClick={() => (window.location.href = '/')}>
                  <Home className="h-4 w-4 mr-2" />
                  Go Home
                </Button>
                <Button variant="outline" onClick={() => window.history.back()}>
                  <ArrowLeft className="h-4 w-4 mr-2" />
                  Go Back
                </Button>
              </div>

              <div className="text-center text-sm text-muted-foreground">
                <p>If this problem persists, please contact our support team.</p>
                <p className="mt-1">
                  <a href="/contact" className="text-primary hover:underline">
                    Contact Support
                  </a>
                </p>
              </div>
            </CardContent>
          </Card>
        </div>
      </body>
    </html>
  );
}
