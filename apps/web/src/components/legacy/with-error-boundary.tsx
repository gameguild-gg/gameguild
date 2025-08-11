'use client';

import React, { Component, ReactNode } from 'react';
import ErrorBoundary from './error-boundary';

interface ErrorBoundaryState {
  hasError: boolean;
  error?: Error;
}

interface WithErrorBoundaryProps {
  fallback?: (error: Error, reset: () => void) => ReactNode;
  onError?: (error: Error, errorInfo: React.ErrorInfo) => void;
  title?: string;
  description?: string;
  showDetails?: boolean;
}

export function withErrorBoundary<P extends object>(WrappedComponent: React.ComponentType<P>, errorBoundaryProps?: WithErrorBoundaryProps) {
  return class WithErrorBoundaryComponent extends Component<
    P & {
      errorBoundaryProps?: WithErrorBoundaryProps;
    },
    ErrorBoundaryState
  > {
    constructor(props: P & { errorBoundaryProps?: WithErrorBoundaryProps }) {
      super(props);
      this.state = { hasError: false };
    }

    static getDerivedStateFromError(error: Error): ErrorBoundaryState {
      return { hasError: true, error };
    }

    componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
      const { errorBoundaryProps: propsBoundaryProps } = this.props;
      const { onError } = { ...errorBoundaryProps, ...propsBoundaryProps };

      if (onError) {
        onError(error, errorInfo);
      }

      // Log to error reporting service
      console.error('Component Error Boundary:', error, errorInfo);
    }

    reset = () => {
      this.setState({ hasError: false, error: undefined });
    };

    render() {
      if (this.state.hasError && this.state.error) {
        const { errorBoundaryProps: propsBoundaryProps } = this.props;
        const { fallback, title, description, showDetails } = {
          ...errorBoundaryProps,
          ...propsBoundaryProps,
        };

        if (fallback) {
          return fallback(this.state.error, this.reset);
        }

        return <ErrorBoundary error={this.state.error} reset={this.reset} title={title} description={description} showDetails={showDetails} />;
      }

      return <WrappedComponent {...(this.props as P)} />;
    }
  };
}

// React Hook version for functional components
export function useErrorBoundary() {
  const [error, setError] = React.useState<Error | null>(null);

  const captureError = React.useCallback((error: Error) => {
    setError(error);
  }, []);

  const resetError = React.useCallback(() => {
    setError(null);
  }, []);

  React.useEffect(() => {
    if (error) {
      throw error;
    }
  }, [error]);

  return { captureError, resetError };
}
