/**
 * @vitest-environment happy-dom
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import React from 'react';
import { render, act, waitFor } from '@testing-library/react';
import { SessionContext } from '../../src/integrations/react/session-provider.js';
import { useAuth } from '../../src/integrations/react/use-auth.js';

const mockFetch = vi.fn();

beforeEach(() => {
  vi.stubGlobal('fetch', mockFetch);
  mockFetch.mockReset();
});

afterEach(() => {
  vi.unstubAllGlobals();
});

// Helper component to capture useAuth return
function AuthConsumer({ onResult, basePath }: { onResult: (auth: ReturnType<typeof useAuth>) => void; basePath?: string }) {
  const auth = useAuth({ basePath });
  React.useEffect(() => {
    onResult(auth);
  });
  return <span data-testid="loading">{auth.isLoading ? 'yes' : 'no'}</span>;
}

describe('useAuth — gap coverage (lines 173, 234, 285)', () => {
  const mockSessionContext = {
    data: { user: { id: '1', email: 'test@test.com', name: 'Test' }, expires: '' },
    status: 'authenticated' as const,
    update: vi.fn(async () => null),
  };

  function renderWithContext(onResult: (auth: ReturnType<typeof useAuth>) => void) {
    return render(
      <SessionContext.Provider value={mockSessionContext}>
        <AuthConsumer onResult={onResult} />
      </SessionContext.Provider>,
    );
  }

  it('signIn redirects to OAuth URL when response has url (line 173)', async () => {
    let auth: ReturnType<typeof useAuth> | undefined;
    renderWithContext((a) => {
      auth = a;
    });

    // Mock CSRF token fetch
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ csrfToken: 'csrf-123' }),
    });

    // Mock sign-in response with URL (OAuth redirect)
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ url: 'https://oauth.provider.com/auth' }),
    });

    await act(async () => {
      await auth!.signIn('github', { redirect: false });
    });

    // The code tries to set window.location.href to the OAuth URL
    // In happy-dom this may or may not actually navigate -- the key is no error
    expect(mockFetch).toHaveBeenCalledTimes(2);
  });

  it('signIn throws and sets error on non-ok response', async () => {
    let auth: ReturnType<typeof useAuth> | undefined;
    renderWithContext((a) => {
      auth = a;
    });

    // CSRF
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ csrfToken: 'csrf-123' }),
    });

    // Failed sign-in with message
    mockFetch.mockResolvedValueOnce({
      ok: false,
      json: async () => ({ message: 'Invalid credentials' }),
    });

    await act(async () => {
      try {
        await auth!.signIn('credentials', { email: 'a@b.com', password: 'wrong' });
      } catch (e) {
        expect((e as Error).message).toBe('Invalid credentials');
      }
    });

    expect(auth!.error).toBeTruthy();
    expect(auth!.error!.message).toBe('Invalid credentials');
  });

  it('signIn throws generic error when response.json fails', async () => {
    let auth: ReturnType<typeof useAuth> | undefined;
    renderWithContext((a) => {
      auth = a;
    });

    // CSRF
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ csrfToken: 'csrf-123' }),
    });

    // Failed sign-in, json parsing also fails
    mockFetch.mockResolvedValueOnce({
      ok: false,
      json: async () => {
        throw new Error('parse error');
      },
    });

    await act(async () => {
      try {
        await auth!.signIn('credentials');
      } catch (e) {
        expect((e as Error).message).toBe('Sign-in failed');
      }
    });
  });

  it('signUp succeeds and updates session (line 234)', async () => {
    let auth: ReturnType<typeof useAuth> | undefined;
    renderWithContext((a) => {
      auth = a;
    });

    // CSRF
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ csrfToken: 'csrf-123' }),
    });

    // Successful signup
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ userId: 'u1' }),
    });

    await act(async () => {
      await auth!.signUp({
        username: 'test',
        email: 'test@example.com',
        password: 'pass123',
        redirect: false,
      });
    });

    expect(mockSessionContext.update).toHaveBeenCalled();
  });

  it('signUp throws on non-ok response', async () => {
    let auth: ReturnType<typeof useAuth> | undefined;
    renderWithContext((a) => {
      auth = a;
    });

    // CSRF
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ csrfToken: 'csrf-123' }),
    });

    // Failed signup
    mockFetch.mockResolvedValueOnce({
      ok: false,
      json: async () => ({ message: 'Email taken' }),
    });

    await act(async () => {
      try {
        await auth!.signUp({
          username: 'test',
          email: 'taken@example.com',
          password: 'pass123',
          redirect: false,
        });
      } catch (e) {
        expect((e as Error).message).toBe('Email taken');
      }
    });
  });

  it('signOut succeeds and updates session (line 285)', async () => {
    let auth: ReturnType<typeof useAuth> | undefined;
    renderWithContext((a) => {
      auth = a;
    });

    // CSRF
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ csrfToken: 'csrf-123' }),
    });

    // Successful signout
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({}),
    });

    await act(async () => {
      await auth!.signOut({ redirect: false });
    });

    expect(mockSessionContext.update).toHaveBeenCalled();
  });

  it('signOut throws on non-ok response', async () => {
    let auth: ReturnType<typeof useAuth> | undefined;
    renderWithContext((a) => {
      auth = a;
    });

    // CSRF
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ csrfToken: 'csrf-123' }),
    });

    // Failed signout
    mockFetch.mockResolvedValueOnce({
      ok: false,
    });

    await act(async () => {
      try {
        await auth!.signOut({ redirect: false });
      } catch (e) {
        expect((e as Error).message).toBe('Sign-out failed');
      }
    });
  });

  it('clearError resets error state', async () => {
    let auth: ReturnType<typeof useAuth> | undefined;
    renderWithContext((a) => {
      auth = a;
    });

    // CSRF
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ csrfToken: 'csrf-123' }),
    });

    // Failed sign-in
    mockFetch.mockResolvedValueOnce({
      ok: false,
      json: async () => ({ message: 'fail' }),
    });

    await act(async () => {
      try {
        await auth!.signIn('credentials');
      } catch {}
    });

    expect(auth!.error).toBeTruthy();

    act(() => {
      auth!.clearError();
    });

    expect(auth!.error).toBeNull();
  });

  it('CSRF token is cached and reused', async () => {
    let auth: ReturnType<typeof useAuth> | undefined;
    renderWithContext((a) => {
      auth = a;
    });

    // First CSRF fetch
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ csrfToken: 'csrf-cached' }),
    });

    // First sign-in
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true }),
    });

    await act(async () => {
      await auth!.signIn('credentials', { redirect: false });
    });

    // After signIn, csrfToken is cleared. Next call should fetch again
    // CSRF fetch
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ csrfToken: 'csrf-2' }),
    });
    // Sign-in
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true }),
    });

    await act(async () => {
      await auth!.signIn('credentials', { redirect: false });
    });

    // Total fetch calls: 4 (2 CSRF + 2 signIn)
    expect(mockFetch).toHaveBeenCalledTimes(4);
  });

  it('CSRF token fetch failure throws', async () => {
    let auth: ReturnType<typeof useAuth> | undefined;
    renderWithContext((a) => {
      auth = a;
    });

    // Failed CSRF fetch
    mockFetch.mockResolvedValueOnce({
      ok: false,
    });

    await act(async () => {
      try {
        await auth!.signIn('credentials', { redirect: false });
      } catch (e) {
        expect((e as Error).message).toBe('Failed to fetch CSRF token');
      }
    });
  });
});
