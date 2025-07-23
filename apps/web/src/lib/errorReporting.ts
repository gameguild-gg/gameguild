/**
 * Error reporting utilities for Next.js 15+ with SSR support
 * Handles error tracking and analytics integration
 */

export interface ErrorReport {
  error: {
    name: string;
    message: string;
    stack?: string;
  };
  errorInfo?: {
    componentStack?: string;
  };
  context: {
    userAgent: string;
    url: string;
    timestamp: string;
    level: 'page' | 'component' | 'critical';
    eventId: string;
  };
  user?: {
    id?: string;
    email?: string;
  };
}

export class ErrorReporter {
  private isClient = typeof window !== 'undefined';
  private isDevelopment = process.env.NODE_ENV === 'development';

  /**
   * Generate a unique error ID
   */
  private generateErrorId(): string {
    return `error-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Create a standardized error report
   */
  createErrorReport(error: Error, errorInfo?: { componentStack?: string }, level: 'page' | 'component' | 'critical' = 'component'): ErrorReport {
    return {
      error: {
        name: error.name,
        message: error.message,
        stack: error.stack,
      },
      errorInfo,
      context: {
        userAgent: this.isClient ? navigator.userAgent : 'server',
        url: this.isClient ? window.location.href : 'server',
        timestamp: new Date().toISOString(),
        level,
        eventId: this.generateErrorId(),
      },
    };
  }

  /**
   * Report error to multiple services
   */
  async reportError(errorReport: ErrorReport): Promise<void> {
    // Console logging in development
    if (this.isDevelopment) {
      console.error('Error Report:', errorReport);
    }

    // Only proceed with external reporting on client-side
    if (!this.isClient) {
      return;
    }

    // Report to Sentry if available
    try {
      if (window.Sentry) {
        window.Sentry.captureException(new Error(errorReport.error.message), {
          contexts: {
            react: { componentStack: errorReport.errorInfo?.componentStack },
            error: errorReport.context,
          },
          tags: {
            errorBoundary: true,
            level: errorReport.context.level,
          },
          extra: errorReport,
        });
      }
    } catch (sentryError) {
      console.warn('Failed to report to Sentry:', sentryError);
    }

    // Report to Google Analytics if available
    try {
      if (window.gtag) {
        window.gtag('event', 'exception', {
          description: errorReport.error.message,
          fatal: errorReport.context.level === 'critical',
          event_category: 'Error Boundary',
          event_label: errorReport.context.level,
        });
      }
    } catch (analyticsError) {
      console.warn('Failed to report to Google Analytics:', analyticsError);
    }

    // Report to custom endpoint if needed
    try {
      if (process.env.NEXT_PUBLIC_ERROR_REPORTING_ENDPOINT) {
        await fetch(process.env.NEXT_PUBLIC_ERROR_REPORTING_ENDPOINT, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(errorReport),
        });
      }
    } catch (fetchError) {
      console.warn('Failed to report to custom endpoint:', fetchError);
    }
  }

  /**
   * Report error with simplified interface
   */
  async reportSimpleError(error: Error, level: 'page' | 'component' | 'critical' = 'component'): Promise<void> {
    const errorReport = this.createErrorReport(error, undefined, level);
    await this.reportError(errorReport);
  }
}

// Export a singleton instance
export const errorReporter = new ErrorReporter();

// Global error handler for unhandled promise rejections
if (typeof window !== 'undefined') {
  window.addEventListener('unhandledrejection', (event) => {
    errorReporter.reportSimpleError(new Error(`Unhandled Promise Rejection: ${event.reason}`), 'critical');
  });

  // Global error handler for uncaught exceptions
  window.addEventListener('error', (event) => {
    errorReporter.reportSimpleError(new Error(`Uncaught Error: ${event.message} at ${event.filename}:${event.lineno}`), 'critical');
  });
}
