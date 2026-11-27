/** @vitest-environment happy-dom */
import { afterEach, describe, expect, it, vi } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import { useAuth } from '../../src/integrations/react/use-auth.js';
import { EmailVerificationRequiredError } from '../../src/runtime/auth/errors.js';

afterEach(() => vi.unstubAllGlobals());
const json = (data: unknown, status = 200) => new Response(JSON.stringify(data), { status });

describe('browser email-verification interrupt (#1298 J1)', () => {
  it('keeps the interrupt typed so the form can route to verification, and stores no MFA challenge', async () => {
    const backend = vi
      .fn()
      .mockResolvedValueOnce(json({ csrfToken: 'first-csrf' }))
      .mockResolvedValueOnce(
        json({ error: 'EmailVerificationRequired', message: 'Email verification required', status: 403, userId: 'u1', email: 'a@example.com' }, 403),
      );
    vi.stubGlobal('fetch', backend);
    const { result, unmount } = renderHook(() => useAuth());
    let thrown: unknown;
    await act(async () => {
      try {
        await result.current.signIn('credentials', { email: 'a@example.com', password: 'pw', redirect: false });
      } catch (error) {
        thrown = error;
      }
    });
    expect(thrown).toBeInstanceOf(EmailVerificationRequiredError);
    expect((thrown as EmailVerificationRequiredError).email).toBe('a@example.com');
    expect(result.current.mfaChallenge).toBeNull();
    expect(sessionStorage.getItem('gg.auth.mfaChallenge')).toBeNull();
    // The hook's error state keeps the typed error; the sign-in form decides what to show.
    expect(result.current.error).toBe(thrown);
    unmount();
  });

  it('falls back to the typed address when the interrupt carries none', async () => {
    const backend = vi
      .fn()
      .mockResolvedValueOnce(json({ csrfToken: 'first-csrf' }))
      .mockResolvedValueOnce(json({ error: 'EmailVerificationRequired', status: 403 }, 403));
    vi.stubGlobal('fetch', backend);
    const { result, unmount } = renderHook(() => useAuth());
    let thrown: unknown;
    await act(async () => {
      try {
        await result.current.signIn('credentials', { email: 'typed@example.com', password: 'pw', redirect: false });
      } catch (error) {
        thrown = error;
      }
    });
    expect(thrown).toBeInstanceOf(EmailVerificationRequiredError);
    expect((thrown as EmailVerificationRequiredError).email).toBeUndefined();
    unmount();
  });
});
