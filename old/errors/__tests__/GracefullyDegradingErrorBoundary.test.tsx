import { render, screen, fireEvent } from '@testing-library/react';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { GracefullyDegradingErrorBoundary } from '../GracefullyDegradingErrorBoundary';

// Mock logger
vi.mock('@/lib/logger', () => ({
  logger: {
    error: vi.fn(),
  },
}));

// Component that throws an error
const ThrowError = ({ shouldThrow = true }: { shouldThrow?: boolean }) => {
  if (shouldThrow) {
    throw new Error('Test error message');
  }
  return <div>No error</div>;
};

// Custom fallback for testing
const TestFallback = ({ error, resetError }: { error: Error; resetError: () => void }) => (
  <div>
    <h2>Custom Error</h2>
    <p>{error.message}</p>
    <button onClick={resetError}>Reset</button>
  </div>
);

describe('GracefullyDegradingErrorBoundary', () => {
  const mockOnError = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    // Suppress console.error for cleaner test output
    vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  it('renders children when there is no error', () => {
    render(
      <GracefullyDegradingErrorBoundary>
        <div>Test content</div>
      </GracefullyDegradingErrorBoundary>,
    );

    expect(screen.getByText('Test content')).toBeInTheDocument();
  });

  it('catches errors and displays default fallback', () => {
    render(
      <GracefullyDegradingErrorBoundary onError={mockOnError}>
        <ThrowError />
      </GracefullyDegradingErrorBoundary>,
    );

    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
    expect(screen.getByText('Try Again')).toBeInTheDocument();
    expect(screen.getByText('Reload Page')).toBeInTheDocument();
    expect(mockOnError).toHaveBeenCalledWith(
      expect.any(Error),
      expect.objectContaining({
        componentStack: expect.any(String),
      }),
    );
  });

  it('renders custom fallback when provided', () => {
    render(
      <GracefullyDegradingErrorBoundary fallback={TestFallback}>
        <ThrowError />
      </GracefullyDegradingErrorBoundary>,
    );

    expect(screen.getByText('Custom Error')).toBeInTheDocument();
    expect(screen.getByText('Test error message')).toBeInTheDocument();
  });

  it('resets error when reset button is clicked', () => {
    const { rerender } = render(
      <GracefullyDegradingErrorBoundary fallback={TestFallback}>
        <ThrowError />
      </GracefullyDegradingErrorBoundary>,
    );

    expect(screen.getByText('Custom Error')).toBeInTheDocument();

    // Click reset button
    fireEvent.click(screen.getByText('Reset'));

    // Re-render with non-throwing component
    rerender(
      <GracefullyDegradingErrorBoundary fallback={TestFallback}>
        <ThrowError shouldThrow={false} />
      </GracefullyDegradingErrorBoundary>,
    );

    expect(screen.getByText('No error')).toBeInTheDocument();
  });

  it('resets error when resetKeys change', () => {
    const { rerender } = render(
      <GracefullyDegradingErrorBoundary resetKeys={['key1']} fallback={TestFallback}>
        <ThrowError />
      </GracefullyDegradingErrorBoundary>,
    );

    expect(screen.getByText('Custom Error')).toBeInTheDocument();

    // Change resetKeys
    rerender(
      <GracefullyDegradingErrorBoundary resetKeys={['key2']} fallback={TestFallback}>
        <ThrowError shouldThrow={false} />
      </GracefullyDegradingErrorBoundary>,
    );

    expect(screen.getByText('No error')).toBeInTheDocument();
  });

  it('resets error when props change and resetOnPropsChange is true', () => {
    const { rerender } = render(
      <GracefullyDegradingErrorBoundary resetOnPropsChange fallback={TestFallback}>
        <ThrowError />
      </GracefullyDegradingErrorBoundary>,
    );

    expect(screen.getByText('Custom Error')).toBeInTheDocument();

    // Change children
    rerender(
      <GracefullyDegradingErrorBoundary resetOnPropsChange fallback={TestFallback}>
        <ThrowError shouldThrow={false} />
      </GracefullyDegradingErrorBoundary>,
    );

    expect(screen.getByText('No error')).toBeInTheDocument();
  });

  it('shows error details in development mode', () => {
    const originalEnv = process.env.NODE_ENV;

    // Mock NODE_ENV for this test
    Object.defineProperty(process.env, 'NODE_ENV', {
      value: 'development',
      configurable: true,
    });

    render(
      <GracefullyDegradingErrorBoundary>
        <ThrowError />
      </GracefullyDegradingErrorBoundary>,
    );

    expect(screen.getByText('Error Details (Development)')).toBeInTheDocument();

    // Restore original NODE_ENV
    Object.defineProperty(process.env, 'NODE_ENV', {
      value: originalEnv,
      configurable: true,
    });
  });

  it('applies isolation class when isolate prop is true', () => {
    const { container } = render(
      <GracefullyDegradingErrorBoundary isolate>
        <ThrowError />
      </GracefullyDegradingErrorBoundary>,
    );

    expect(container.querySelector('.error-boundary-isolation')).toBeInTheDocument();
  });

  it('has proper accessibility attributes', () => {
    render(
      <GracefullyDegradingErrorBoundary>
        <ThrowError />
      </GracefullyDegradingErrorBoundary>,
    );

    const errorContainer = screen.getByRole('alert');
    expect(errorContainer).toHaveAttribute('aria-labelledby', 'error-title');
    expect(errorContainer).toHaveAttribute('aria-describedby', 'error-description');

    expect(screen.getByLabelText('Try again')).toBeInTheDocument();
    expect(screen.getByLabelText('Reload page')).toBeInTheDocument();
  });
});
