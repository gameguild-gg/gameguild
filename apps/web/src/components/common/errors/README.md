# Error Boundary Usage Guide

## GracefullyDegradingErrorBoundary

A modern, accessible, and feature-rich error boundary component for Next.js 15+ and React 19.

### Features

- ✅ **React 19 Compatible**: Uses `startTransition` for better performance
- ✅ **Next.js 15+ Optimized**: Client-side rendering with proper hydration handling
- ✅ **Tailwind CSS 4+ Ready**: Modern utility classes with dark mode support
- ✅ **Accessibility First**: ARIA labels, focus management, and semantic HTML
- ✅ **Error Recovery**: Automatic and manual reset capabilities
- ✅ **Development Tools**: Detailed error information in development mode
- ✅ **Production Ready**: Error reporting and monitoring integration
- ✅ **TypeScript**: Fully typed with comprehensive interfaces

### Basic Usage

```tsx
import { GracefullyDegradingErrorBoundary } from '@/components/errors/GracefullyDegradingErrorBoundary';

function App() {
  return (
    <GracefullyDegradingErrorBoundary>
      <YourComponent />
    </GracefullyDegradingErrorBoundary>
  );
}
```

### Advanced Usage

```tsx
import { GracefullyDegradingErrorBoundary } from '@/components/errors/GracefullyDegradingErrorBoundary';

// Custom fallback component
const CustomErrorFallback = ({ error, resetError }) => (
  <div className="p-8 text-center">
    <h2>Oops! Something went wrong</h2>
    <p>{error.message}</p>
    <button onClick={resetError}>Try Again</button>
  </div>
);

function App() {
  return (
    <GracefullyDegradingErrorBoundary
      fallback={CustomErrorFallback}
      onError={(error, errorInfo) => {
        // Custom error handling
        console.log('Error caught:', error);
        // Send to analytics, etc.
      }}
      resetKeys={[userId, currentRoute]}
      resetOnPropsChange={true}
      isolate={true}
    >
      <YourComponent />
    </GracefullyDegradingErrorBoundary>
  );
}
```

### Functional Component Error Handling

```tsx
import { useAsyncError, useErrorBoundary } from '@/hooks/useErrorBoundary';

function MyComponent() {
  const { captureError } = useErrorBoundary();
  const { executeAsync } = useAsyncError();

  const handleClick = () => {
    // Trigger error boundary from event handler
    captureError(new Error('Something went wrong!'));
  };

  const handleAsyncOperation = async () => {
    // Handle async errors safely
    await executeAsync(async () => {
      const response = await fetch('/api/data');
      if (!response.ok) {
        throw new Error('Failed to fetch data');
      }
      return response.json();
    });
  };

  return (
    <div>
      <button onClick={ handleClick }>Trigger Error</button>
      <button onClick={ handleAsyncOperation }>Async Operation</button>
    </div>
  );
}
```

### Layout Integration

```tsx
// app/layout.tsx
import { GracefullyDegradingErrorBoundary } from '@/components/errors/GracefullyDegradingErrorBoundary';

export default function RootLayout({ children }) {
  return (
    <html lang="en">
      <body>
        <GracefullyDegradingErrorBoundary
          onError={(error, errorInfo) => {
            // Global error reporting
            if (window.Sentry) {
              window.Sentry.captureException(error);
            }
          }}
        >
          {children}
        </GracefullyDegradingErrorBoundary>
      </body>
    </html>
  );
}
```

### Props Reference

#### GracefullyDegradingErrorBoundary

| Prop                 | Type                                           | Default                | Description                    |
| -------------------- | ---------------------------------------------- | ---------------------- | ------------------------------ |
| `children`           | `ReactNode`                                    | -                      | Child components to wrap       |
| `fallback`           | `ComponentType<ErrorFallbackProps>`            | `DefaultErrorFallback` | Custom error UI component      |
| `onError`            | `(error: Error, errorInfo: ErrorInfo) => void` | -                      | Error event handler            |
| `resetOnPropsChange` | `boolean`                                      | `false`                | Reset when props change        |
| `resetKeys`          | `Array<string \| number>`                      | -                      | Reset when these values change |
| `isolate`            | `boolean`                                      | `false`                | Prevent error propagation      |

#### ErrorFallbackProps

| Prop         | Type         | Description                          |
| ------------ | ------------ | ------------------------------------ |
| `error`      | `Error`      | The caught error object              |
| `resetError` | `() => void` | Function to reset the error boundary |
| `errorInfo`  | `ErrorInfo`  | React error info (optional)          |

### Best Practices

1. **Granular Boundaries**: Place error boundaries at multiple levels for better isolation
2. **Custom Fallbacks**: Create context-specific error messages for better UX
3. **Error Reporting**: Always integrate with error monitoring services
4. **Recovery Strategies**: Implement automatic retry mechanisms where appropriate
5. **User Feedback**: Provide clear next steps for users when errors occur

### Integration with Monitoring

```tsx
// Sentry integration example
import * as Sentry from '@sentry/nextjs';

<GracefullyDegradingErrorBoundary
  onError={(error, errorInfo) => {
    Sentry.captureException(error, {
      contexts: {
        react: {
          componentStack: errorInfo.componentStack,
        },
      },
    });
  }}
>
  <App />
</GracefullyDegradingErrorBoundary>;
```

### Testing

```tsx
// Test error boundary behavior
import { render, screen } from '@testing-library/react';
import { GracefullyDegradingErrorBoundary } from './GracefullyDegradingErrorBoundary';

const ThrowError = () => {
  throw new Error('Test error');
};

test('catches and displays error', () => {
  render(
    <GracefullyDegradingErrorBoundary>
      <ThrowError />
    </GracefullyDegradingErrorBoundary>,
  );

  expect(screen.getByText('Something went wrong')).toBeInTheDocument();
  expect(screen.getByText('Try Again')).toBeInTheDocument();
});
```
