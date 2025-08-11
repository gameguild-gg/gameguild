'use client';

import React, { Component, ErrorInfo, ReactNode } from 'react';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error?: Error;
}

export class MarkdownErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('Markdown renderer error:', error, errorInfo);
    
    // Only report to error system if it's a proper Error object
    if (error instanceof Error) {
      // Import dynamically to avoid circular dependencies
      import('@/components/common/errors/errorReporting').then(({ errorReporter }) => {
        errorReporter.reportSimpleError(error, 'component');
      }).catch(() => {
        // Silently fail if error reporting is not available
      });
    }
  }

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <div className="p-4 bg-red-50 border border-red-200 rounded-md">
          <h3 className="text-red-800 font-medium mb-2">Error rendering markdown content</h3>
          <p className="text-red-600 text-sm">
            {this.state.error?.message || 'An unexpected error occurred while rendering the content.'}
          </p>
          <button
            onClick={() => this.setState({ hasError: false, error: undefined })}
            className="mt-2 px-3 py-1 bg-red-100 text-red-700 rounded text-sm hover:bg-red-200 transition-colors"
          >
            Try again
          </button>
        </div>
      );
    }

    return this.props.children;
  }
} 