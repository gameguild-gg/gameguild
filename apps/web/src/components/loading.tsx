import React from 'react';
import { Card, CardContent, CardHeader } from '@game-guild/ui/components';
import { Skeleton } from '@game-guild/ui/components';
import { Loader2 } from 'lucide-react';

interface LoadingProps {
  type?: 'page' | 'section' | 'card' | 'minimal';
  message?: string;
  showSpinner?: boolean;
}

export default function Loading({ type = 'page', message = 'Loading...', showSpinner = true }: LoadingProps) {
  if (type === 'minimal') {
    return (
      <div className="flex items-center justify-center p-4">
        {showSpinner && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
        <span className="text-sm text-muted-foreground">{message}</span>
      </div>
    );
  }

  if (type === 'card') {
    return (
      <Card>
        <CardHeader>
          <Skeleton className="h-4 w-3/4" />
          <Skeleton className="h-3 w-1/2" />
        </CardHeader>
        <CardContent className="space-y-2">
          <Skeleton className="h-3 w-full" />
          <Skeleton className="h-3 w-4/5" />
          <Skeleton className="h-3 w-3/5" />
        </CardContent>
      </Card>
    );
  }

  if (type === 'section') {
    return (
      <div className="space-y-4 p-6">
        <div className="flex items-center justify-center mb-6">
          {showSpinner && <Loader2 className="h-6 w-6 animate-spin mr-3" />}
          <span className="text-muted-foreground">{message}</span>
        </div>
        <div className="space-y-3">
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-4/5" />
          <Skeleton className="h-4 w-3/5" />
        </div>
      </div>
    );
  }

  // Default 'page' type
  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <div className="text-center space-y-4">
        {showSpinner && (
          <div className="mx-auto w-16 h-16 bg-muted rounded-full flex items-center justify-center">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
          </div>
        )}
        <h2 className="text-xl font-semibold">Loading</h2>
        <p className="text-muted-foreground max-w-sm">{message}</p>
      </div>
    </div>
  );
}
