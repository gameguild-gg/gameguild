import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import React from 'react';
import {
  SessionContext,
  useAuth,
  type SessionContextValue,
} from '@game-guild/client/react';

/* ------------------------------------------------------------------ */
/*  Mock fetch                                                         */
/* ------------------------------------------------------------------ */

const mockFetch = vi.fn();
const mockLocationHref = vi.fn();

// Mock window.location
const originalLocation = window.location;

beforeEach(() => {
  vi.stubGlobal('fetch', mockFetch);

  // Mock window.location.href setter
  Object.defineProperty(window, 'location', {
    value: {
      ...originalLocation,
      set href(url: string) {
        mockLocationHref(url);
      },
    },
    writable: true,
    configurable: true,
  });
});

afterEach(() => {
  vi.restoreAllMocks();
  window.location = originalLocation;
});

/* ------------------------------------------------------------------ */
/*  Mock SessionContext                                                 */
/* ------------------------------------------------------------------ */

const mockSessionUpdate = vi.fn().mockResolvedValue(undefined);

function createWrapper(sessionValue?: Partial<SessionContextValue>) {
  const value: SessionContextValue = {
    data: null,
    status: 'unauthenticated',
    update: mockSessionUpdate,
    ...sessionValue,
  };

  return function Wrapper({ children }: { children: React.ReactNode }) {
    return React.createElement(SessionContext.Provider, { value }, children);
  };
}

/* ------------------------------------------------------------------ */
/*  Helper: mock CSRF response                                         */
/* ------------------------------------------------------------------ */

function mockCSRFResponse() {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    json: () => Promise.resolve({ csrfToken: 'test-csrf-token' }),
  });
}

function mockSignInSuccess(data: Record<string, unknown> = {}) {
  mockFetch.mockResolvedValueOnce({
    ok: true,
    json: () => Promise.resolve(data),
  });
}

function mockSignInFailure(message: string, status = 401) {
  mockFetch.mockResolvedValueOnce({
    ok: false,
    status,
    json: () => Promise.resolve({ message }),
  });
}

/* ------------------------------------------------------------------ */
/*  Tests                                                              */
/* ------------------------------------------------------------------ */

describe('useAuth', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockSessionUpdate.mockReset().mockResolvedValue(undefined);
  });

  /* ---------- Initial state ---------- */

  it('returns initial state with isLoading false and no error', () => {
    const { result } = renderHook(() => useAuth(), {
      wrapper: createWrapper(),
    });

    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
    expect(typeof result.current.signIn).toBe('function');
    expect(typeof result.current.signUp).toBe('function');
    expect(typeof result.current.signOut).toBe('function');
    expect(typeof result.current.clearError).toBe('function');
  });

  /* ---------- CSRF token fetching ---------- */

  describe('CSRF token', () => {
    it('fetches CSRF token before signIn', async () => {
      mockCSRFResponse();
      mockSignInSuccess();

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await act(async () => {
        await result.current.signIn('credentials', {
          email: 'test@test.com',
          password: 'pass',
          redirect: false,
        });
      });

      expect(mockFetch).toHaveBeenCalledWith('/api/auth/csrf', {
        credentials: 'include',
      });
    });

    it('throws when CSRF fetch fails', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
      });

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await act(async () => {
        try {
          await result.current.signIn('credentials', {
            email: 'test@test.com',
            password: 'pass',
            redirect: false,
          });
        } catch {
          // expected
        }
      });

      expect(result.current.error).toBeTruthy();
      expect(result.current.error?.message).toBe('Failed to fetch CSRF token');
    });
  });

  /* ---------- signIn ---------- */

  describe('signIn', () => {
    it('sends credentials to the correct endpoint', async () => {
      mockCSRFResponse();
      mockSignInSuccess();

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await act(async () => {
        await result.current.signIn('credentials', {
          email: 'test@test.com',
          password: 'password123',
          redirect: false,
        });
      });

      const signInCall = mockFetch.mock.calls[1];
      expect(signInCall[0]).toBe('/api/auth/signin/credentials');
      expect(signInCall[1]).toMatchObject({
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
      });

      const body = JSON.parse(signInCall[1].body);
      expect(body.email).toBe('test@test.com');
      expect(body.password).toBe('password123');
      expect(body.csrfToken).toBe('test-csrf-token');
    });

    it('sets isLoading to true during signIn', async () => {
      let resolveSignIn: (value: unknown) => void;
      mockCSRFResponse();
      mockFetch.mockImplementationOnce(
        () =>
          new Promise((resolve) => {
            resolveSignIn = resolve;
          })
      );

      const { result } = renderHook(() => useAuth());

      // Don't await — check loading state in flight
      let promise: Promise<void>;
      act(() => {
        promise = result.current.signIn('credentials', {
          email: 'test@test.com',
          password: 'pass',
          redirect: false,
        });
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(true);
      });

      // Resolve to clean up
      await act(async () => {
        resolveSignIn?.({
          ok: true,
          json: () => Promise.resolve({}),
        });
        try {
          await promise!;
        } catch {
          // ignore
        }
      });
      expect(result.current.isLoading).toBe(false);
    });

    it('sets error on signIn failure', async () => {
      mockCSRFResponse();
      mockSignInFailure('Invalid credentials');

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await act(async () => {
        try {
          await result.current.signIn('credentials', {
            email: 'test@test.com',
            password: 'wrong',
            redirect: false,
          });
        } catch {
          // expected
        }
      });

      expect(result.current.error).toBeTruthy();
      expect(result.current.error?.message).toBe('Invalid credentials');
      expect(result.current.isLoading).toBe(false);
    });

    it('handles OAuth redirect by setting window.location.href', async () => {
      mockCSRFResponse();
      mockSignInSuccess({ url: 'https://accounts.google.com/o/oauth2/auth' });

      const { result } = renderHook(() => useAuth());

      await act(async () => {
        await result.current.signIn('google', {
          redirectTo: '/dashboard',
        });
      });

      expect(mockLocationHref).toHaveBeenCalledWith(
        'https://accounts.google.com/o/oauth2/auth'
      );
    });

    it('uses custom basePath', async () => {
      mockCSRFResponse();
      mockSignInSuccess();

      const { result } = renderHook(() =>
        useAuth({ basePath: '/custom/auth' })
        , {
          wrapper: createWrapper(),
        });

      await act(async () => {
        await result.current.signIn('credentials', {
          email: 'test@test.com',
          password: 'pass',
          redirect: false,
        });
      });

      expect(mockFetch).toHaveBeenCalledWith('/custom/auth/csrf', {
        credentials: 'include',
      });
      expect(mockFetch.mock.calls[1][0]).toBe(
        '/custom/auth/signin/credentials'
      );
    });

    it('calls sessionContext.update after successful signIn', async () => {
      mockCSRFResponse();
      mockSignInSuccess();

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await act(async () => {
        await result.current.signIn('credentials', {
          email: 'test@test.com',
          password: 'pass',
          redirect: false,
        });
      });

      expect(mockSessionUpdate).toHaveBeenCalled();
    });
  });

  /* ---------- signUp ---------- */

  describe('signUp', () => {
    it('sends signup data to the correct endpoint', async () => {
      mockCSRFResponse();
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({}),
      });

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await act(async () => {
        await result.current.signUp({
          username: 'john',
          email: 'john@example.com',
          password: 'password123',
          firstName: 'John',
          lastName: 'Doe',
          redirect: false,
        });
      });

      const signUpCall = mockFetch.mock.calls[1];
      expect(signUpCall[0]).toBe('/api/auth/signup');
      expect(signUpCall[1].method).toBe('POST');

      const body = JSON.parse(signUpCall[1].body);
      expect(body.username).toBe('john');
      expect(body.email).toBe('john@example.com');
      expect(body.password).toBe('password123');
      expect(body.firstName).toBe('John');
      expect(body.lastName).toBe('Doe');
      expect(body.csrfToken).toBe('test-csrf-token');
    });

    it('sets error on signUp failure', async () => {
      mockCSRFResponse();
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 409,
        json: () => Promise.resolve({ message: 'Email already exists' }),
      });

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await act(async () => {
        try {
          await result.current.signUp({
            username: 'john',
            email: 'john@example.com',
            password: 'password123',
            redirect: false,
          });
        } catch {
          // expected
        }
      });

      expect(result.current.error?.message).toBe('Email already exists');
    });

    it('calls sessionContext.update after successful signUp', async () => {
      mockCSRFResponse();
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({}),
      });

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await act(async () => {
        await result.current.signUp({
          username: 'john',
          email: 'john@example.com',
          password: 'password123',
          redirect: false,
        });
      });

      expect(mockSessionUpdate).toHaveBeenCalled();
    });
  });

  /* ---------- signOut ---------- */

  describe('signOut', () => {
    it('sends signout request to the correct endpoint', async () => {
      mockCSRFResponse();
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({}),
      });

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await act(async () => {
        await result.current.signOut({ redirect: false });
      });

      const signOutCall = mockFetch.mock.calls[1];
      expect(signOutCall[0]).toBe('/api/auth/signout');
      expect(signOutCall[1].method).toBe('POST');

      const body = JSON.parse(signOutCall[1].body);
      expect(body.csrfToken).toBe('test-csrf-token');
    });

    it('sets error on signOut failure', async () => {
      mockCSRFResponse();
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
      });

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await act(async () => {
        try {
          await result.current.signOut({ redirect: false });
        } catch {
          // expected
        }
      });

      expect(result.current.error?.message).toBe('Sign-out failed');
    });
  });

  /* ---------- clearError ---------- */

  describe('clearError', () => {
    it('clears the error state', async () => {
      mockCSRFResponse();
      mockSignInFailure('Some error');

      const { result } = renderHook(() => useAuth());

      await act(async () => {
        try {
          await result.current.signIn('credentials', {
            email: 'test@test.com',
            password: 'wrong',
            redirect: false,
          });
        } catch {
          // expected
        }
      });

      expect(result.current.error).toBeTruthy();

      act(() => {
        result.current.clearError();
      });

      expect(result.current.error).toBeNull();
    });
  });
});
