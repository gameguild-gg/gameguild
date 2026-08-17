/**
 * @vitest-environment happy-dom
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import React from 'react';
import { render, waitFor } from '@testing-library/react';
import { SessionContext } from '../../src/integrations/react/session-provider.js';
import { useSession } from '../../src/integrations/react/use-session.js';

// Helper component that uses useSession with options
function SessionConsumer({ required, onUnauthenticated }: { required?: boolean; onUnauthenticated?: () => void }) {
  const session = useSession({ required, onUnauthenticated });
  return <span data-testid="status">{session.status}</span>;
}

describe('useSession — gap coverage (lines 81-82)', () => {
  let originalHref: string;

  beforeEach(() => {
    originalHref = window.location.href;
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('throws when used outside SessionProvider', () => {
    // suppress React error boundary console output
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});

    expect(() => {
      render(<SessionConsumer />);
    }).toThrow('useSession must be used within a <SessionProvider>');

    spy.mockRestore();
  });

  it('redirects to /sign-in when required=true, status=unauthenticated, no onUnauthenticated (lines 81-82)', async () => {
    // Mock window.location.href
    const hrefSetter = vi.fn();
    Object.defineProperty(window, 'location', {
      value: {
        ...window.location,
        href: 'http://localhost:3000/dashboard',
      },
      writable: true,
    });

    // We need to intercept the redirect. Use a spy on location.href setter.
    const mockLocation = {
      href: 'http://localhost:3000/dashboard',
    };
    Object.defineProperty(window, 'location', {
      value: mockLocation,
      writable: true,
    });

    const unauthContext = {
      data: null,
      status: 'unauthenticated' as const,
      update: vi.fn(async () => null),
    };

    render(
      <SessionContext.Provider value={unauthContext}>
        <SessionConsumer required={true} />
      </SessionContext.Provider>,
    );

    await waitFor(() => {
      // The code should have set window.location.href to /sign-in?callbackUrl=...
      expect(mockLocation.href).toContain('/sign-in?callbackUrl=');
    });
  });

  it('calls onUnauthenticated callback when provided instead of redirecting', async () => {
    const onUnauth = vi.fn();

    const unauthContext = {
      data: null,
      status: 'unauthenticated' as const,
      update: vi.fn(async () => null),
    };

    render(
      <SessionContext.Provider value={unauthContext}>
        <SessionConsumer required={true} onUnauthenticated={onUnauth} />
      </SessionContext.Provider>,
    );

    await waitFor(() => {
      expect(onUnauth).toHaveBeenCalled();
    });
  });

  it('does nothing when required=false', async () => {
    const unauthContext = {
      data: null,
      status: 'unauthenticated' as const,
      update: vi.fn(async () => null),
    };

    const { getByTestId } = render(
      <SessionContext.Provider value={unauthContext}>
        <SessionConsumer required={false} />
      </SessionContext.Provider>,
    );

    expect(getByTestId('status').textContent).toBe('unauthenticated');
  });

  it('does nothing when status is loading', async () => {
    const loadingContext = {
      data: null,
      status: 'loading' as const,
      update: vi.fn(async () => null),
    };

    const { getByTestId } = render(
      <SessionContext.Provider value={loadingContext}>
        <SessionConsumer required={true} />
      </SessionContext.Provider>,
    );

    expect(getByTestId('status').textContent).toBe('loading');
  });
});
