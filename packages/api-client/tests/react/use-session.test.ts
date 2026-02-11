/**
 * Tests for useSession Hook
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';

// Control what useContext returns per-test
let contextReturnValue: any;

const defaultContextValue = {
  data: null as any,
  status: 'loading' as string,
  update: vi.fn(),
};

vi.mock('react', () => ({
  useContext: vi.fn(() => contextReturnValue),
  useEffect: vi.fn((fn: () => any) => fn()),
}));

vi.mock('../../src/integrations/react/session-provider.js', () => ({
  SessionContext: {},
}));

import { useSession } from '../../src/integrations/react/use-session.js';

describe('useSession', () => {
  beforeEach(() => {
    // Reset to default context
    contextReturnValue = { ...defaultContextValue };
    contextReturnValue.update = vi.fn();
    vi.clearAllMocks();
  });

  it('should return session data and status', () => {
    contextReturnValue = {
      data: {
        user: { id: '1', email: 'test@example.com', name: 'Test', image: null },
        expires: '2025-01-01T00:00:00Z',
      },
      status: 'authenticated',
      update: vi.fn(),
    };

    const result = useSession();

    expect(result.data).toBeDefined();
    expect(result.status).toBe('authenticated');
  });

  it('should return loading status initially', () => {
    contextReturnValue = { ...defaultContextValue, status: 'loading', update: vi.fn() };

    const result = useSession();

    expect(result.status).toBe('loading');
  });

  it('should throw when used outside SessionProvider', () => {
    contextReturnValue = undefined;

    expect(() => useSession()).toThrow('useSession must be used within a <SessionProvider>');
  });

  it('should handle required option with unauthenticated status', () => {
    contextReturnValue = { ...defaultContextValue, status: 'unauthenticated', update: vi.fn() };

    // This should trigger the useEffect which checks required
    const result = useSession({ required: true });

    expect(result).toBeDefined();
  });

  it('should call custom onUnauthenticated callback', () => {
    contextReturnValue = { ...defaultContextValue, status: 'unauthenticated', update: vi.fn() };

    const onUnauthenticated = vi.fn();
    useSession({ required: true, onUnauthenticated });

    expect(onUnauthenticated).toHaveBeenCalled();
  });

  it('should not call onUnauthenticated when status is loading', () => {
    contextReturnValue = { ...defaultContextValue, status: 'loading', update: vi.fn() };

    const onUnauthenticated = vi.fn();
    useSession({ required: true, onUnauthenticated });

    expect(onUnauthenticated).not.toHaveBeenCalled();
  });

  it('should not call onUnauthenticated when not required', () => {
    contextReturnValue = { ...defaultContextValue, status: 'unauthenticated', update: vi.fn() };

    const onUnauthenticated = vi.fn();
    useSession({ required: false, onUnauthenticated });

    expect(onUnauthenticated).not.toHaveBeenCalled();
  });

  it('should return update function', () => {
    contextReturnValue = { ...defaultContextValue, update: vi.fn() };

    const result = useSession();
    expect(typeof result.update).toBe('function');
  });
});
