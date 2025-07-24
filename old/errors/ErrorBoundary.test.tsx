/**
 * Test file for Error Boundary components
 * Demonstrates usage patterns and testing scenarios
 */

import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { SmartErrorBoundary, GracefullyDegradingErrorBoundary, RetryableErrorBoundary } from './index';

// Mock components for testing
const ThrowError = ({ shouldThrow = false, message = 'Test error' }) => {
  if (shouldThrow) {
    throw new Error(message);
  }
  return <div>No error</div>;
};

const AsyncErrorComponent = ({ shouldFail = false }) => {
  React.useEffect(() => {
    if (shouldFail) {
      throw new Error('Async error');
    }
  }, [shouldFail]);

  return <div>Async component</div>;
};

describe('Error Boundary Components', () => {
  // Mock console.error to avoid noise in tests
  const originalError = console.error;
  beforeAll(() => {
    console.error = jest.fn();
  });

  afterAll(() => {
    console.error = originalError;
  });

  describe('GracefullyDegradingErrorBoundary', () => {
    it('renders children when there is no error', () => {
      render(
        <GracefullyDegradingErrorBoundary>
          <div>Test content</div>
        </GracefullyDegradingErrorBoundary>,
      );

      expect(screen.getByText('Test content')).toBeInTheDocument();
    });

    it('renders error fallback when child throws error', () => {
      render(
        <GracefullyDegradingErrorBoundary>
          <ThrowError shouldThrow />
        </GracefullyDegradingErrorBoundary>,
      );

      expect(screen.getByText('Something went wrong')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument();
    });

    it('resets error when try again button is clicked', () => {
      const { rerender } = render(
        <GracefullyDegradingErrorBoundary>
          <ThrowError shouldThrow />
        </GracefullyDegradingErrorBoundary>,
      );

      expect(screen.getByText('Something went wrong')).toBeInTheDocument();

      fireEvent.click(screen.getByRole('button', { name: /try again/i }));

      // Re-render with non-throwing component
      rerender(
        <GracefullyDegradingErrorBoundary>
          <ThrowError shouldThrow={false} />
        </GracefullyDegradingErrorBoundary>,
      );

      expect(screen.getByText('No error')).toBeInTheDocument();
    });

    it('calls onError callback when error occurs', () => {
      const onError = jest.fn();

      render(
        <GracefullyDegradingErrorBoundary onError={onError}>
          <ThrowError shouldThrow message="Custom error" />
        </GracefullyDegradingErrorBoundary>,
      );

      expect(onError).toHaveBeenCalledWith(expect.objectContaining({ message: 'Custom error' }), expect.any(Object));
    });
  });

  describe('RetryableErrorBoundary', () => {
    it('shows retry functionality in error state', () => {
      render(
        <RetryableErrorBoundary maxRetries={3}>
          <ThrowError shouldThrow />
        </RetryableErrorBoundary>,
      );

      expect(screen.getByText('Component Error')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
    });

    it('disables retry when max retries reached', () => {
      const { rerender } = render(
        <RetryableErrorBoundary maxRetries={1}>
          <ThrowError shouldThrow />
        </RetryableErrorBoundary>,
      );

      // First retry
      fireEvent.click(screen.getByRole('button', { name: /retry/i }));

      // Should still show retry button with 0 retries left
      rerender(
        <RetryableErrorBoundary maxRetries={1}>
          <ThrowError shouldThrow />
        </RetryableErrorBoundary>,
      );

      // After max retries, retry button should be disabled or hidden
      expect(screen.queryByRole('button', { name: /retry/i })).toBeNull();
    });
  });

  describe('SmartErrorBoundary', () => {
    it('renders as graceful error boundary by default', () => {
      render(
        <SmartErrorBoundary>
          <ThrowError shouldThrow />
        </SmartErrorBoundary>,
      );

      expect(screen.getByText('Something went wrong')).toBeInTheDocument();
    });

    it('renders as retryable error boundary when enableRetry is true', () => {
      render(
        <SmartErrorBoundary enableRetry>
          <ThrowError shouldThrow />
        </SmartErrorBoundary>,
      );

      expect(screen.getByText('Component Error')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
    });
  });

  describe('Error Boundary Integration', () => {
    it('handles different error levels appropriately', () => {
      const onError = jest.fn();

      render(
        <GracefullyDegradingErrorBoundary level="critical" onError={onError}>
          <ThrowError shouldThrow message="Critical error" />
        </GracefullyDegradingErrorBoundary>,
      );

      expect(onError).toHaveBeenCalled();
    });

    it('resets on prop changes when resetKeys are provided', () => {
      const { rerender } = render(
        <GracefullyDegradingErrorBoundary resetKeys={['key1']}>
          <ThrowError shouldThrow />
        </GracefullyDegradingErrorBoundary>,
      );

      expect(screen.getByText('Something went wrong')).toBeInTheDocument();

      // Change reset keys to trigger reset
      rerender(
        <GracefullyDegradingErrorBoundary resetKeys={['key2']}>
          <ThrowError shouldThrow={false} />
        </GracefullyDegradingErrorBoundary>,
      );

      expect(screen.getByText('No error')).toBeInTheDocument();
    });
  });

  describe('SSR Compatibility', () => {
    it('does not break during server-side rendering', () => {
      // Mock window to simulate SSR environment
      const originalWindow = global.window;
      delete (global as any).window;

      expect(() => {
        render(
          <GracefullyDegradingErrorBoundary>
            <div>SSR content</div>
          </GracefullyDegradingErrorBoundary>,
        );
      }).not.toThrow();

      global.window = originalWindow;
    });
  });
});

// Example usage patterns for documentation
export const UsageExamples = {
  // Basic usage
  basic: (
    <SmartErrorBoundary>
      <div>Your component here</div>
    </SmartErrorBoundary>
  ),

  // With retry functionality
  withRetry: (
    <SmartErrorBoundary enableRetry maxRetries={3} retryDelay={1000}>
      <div>Component that might fail</div>
    </SmartErrorBoundary>
  ),

  // With custom error handler
  withCustomHandler: (
    <SmartErrorBoundary
      onError={(error, errorInfo) => {
        console.error('Custom error handling:', { error, errorInfo });
      }}
    >
      <div>Component with custom error handling</div>
    </SmartErrorBoundary>
  ),

  // Component-level isolation
  isolated: (
    <SmartErrorBoundary isolate level="component">
      <div>Isolated component</div>
    </SmartErrorBoundary>
  ),

  // Page-level error boundary
  pageLevel: (
    <SmartErrorBoundary level="page" enableRetry maxRetries={2}>
      <div>Page content</div>
    </SmartErrorBoundary>
  ),
};
