'use client';

import { Component, type ErrorInfo, type ReactNode } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { AlertTriangle, RefreshCw } from 'lucide-react';

interface ErrorBoundaryState {
  readonly hasError: boolean;
  readonly error?: Error;
}

interface ErrorBoundaryProps {
  readonly children: ReactNode;
  readonly fallback?: ReactNode;
  readonly onError?: (error: Error, errorInfo: ErrorInfo) => void;
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    console.error('ErrorBoundary caught an error:', error, errorInfo);
    this.props.onError?.(error, errorInfo);
  }

  render(): ReactNode {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <Card className="bg-red-950/20 border-red-800">
          <CardHeader>
            <CardTitle className="flex items-center text-red-400">
              <AlertTriangle className="mr-2 h-5 w-5" />
              Something went wrong
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-red-300 mb-4">{this.state.error?.message || 'An unexpected error occurred while loading this content.'}</p>
            {process.env.NODE_ENV === 'development' && this.state.error?.stack && (
              <details className="mb-4 p-3 bg-gray-900 rounded border border-gray-700">
                <summary className="cursor-pointer text-sm text-gray-400 mb-2">Error Details</summary>
                <pre className="text-xs text-red-300 whitespace-pre-wrap overflow-auto max-h-40">{this.state.error.stack}</pre>
              </details>
            )}
            <Button onClick={this.handleReset} variant="outline" className="border-red-600 text-red-400 hover:bg-red-950">
              <RefreshCw className="mr-2 h-4 w-4" />
              Try again
            </Button>
          </CardContent>
        </Card>
      );
    }

    return this.props.children;
  }

  private handleReset = (): void => {
    this.setState({ hasError: false, error: undefined });
  };
}
