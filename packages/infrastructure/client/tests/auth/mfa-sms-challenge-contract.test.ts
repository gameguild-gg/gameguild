import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { requestSmsMfaChallenge, MfaVerificationError } from '../../src/runtime/auth/extended-operations.js';
import { createMfaSmsChallengeAction } from '../../src/integrations/next/actions.js';
import { AuthMultiFactorModule } from '../../src/generated/modules/auth-multi-factor.gen.js';
import type { ResolvedAuthConfig } from '../../src/runtime/auth/types.js';

const API_URL = 'https://api.example.test';
const MFA_TOKEN = 'x'.repeat(43);
const fetchMock = vi.fn<typeof fetch>();
beforeEach(() => { fetchMock.mockReset(); vi.stubGlobal('fetch', fetchMock); });
afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks(); });

describe('first-factor-bound SMS delivery transport', () => {
  it('uses the generated operation with only the continuation and no ordinary bearer', async () => {
    const generated = vi.spyOn(AuthMultiFactorModule.prototype, 'postAuthMfaSmsChallenge');
    fetchMock.mockResolvedValueOnce(Response.json({ message: 'Verification code sent', phoneNumberMasked: '***0123', expiresInSeconds: 600 }));
    const result = await requestSmsMfaChallenge(API_URL, MFA_TOKEN);
    expect(generated).toHaveBeenCalledExactlyOnceWith({ mfaToken: MFA_TOKEN });
    expect(fetchMock).toHaveBeenCalledOnce();
    const [url, options] = fetchMock.mock.calls[0];
    expect(url).toBe(API_URL + '/v1/auth/mfa/sms:challenge');
    expect(options?.method).toBe('POST');
    expect(JSON.parse(options?.body as string)).toEqual({ mfaToken: MFA_TOKEN });
    expect(new Headers(options?.headers).has('Authorization')).toBe(false);
    expect(result).toEqual({ success: true, phoneNumberMasked: '***0123', expiresInSeconds: 600 });
    expect(result).not.toHaveProperty('tokens');
  });
  it.each(['', 'x'.repeat(42), '/'.repeat(43), '11111111-1111-4111-8111-111111111111'])('rejects malformed continuations before delivery: %s', async (value) => {
    await expect(requestSmsMfaChallenge(API_URL, value)).rejects.toThrow(MfaVerificationError);
    expect(fetchMock).not.toHaveBeenCalled();
  });
  it.each([400, 429, 500])('returns a safe action failure for backend status%s without creating a session', async (status) => {
    fetchMock.mockResolvedValueOnce(Response.json({ error: 'private backend detail' }, { status }));
    const action = createMfaSmsChallengeAction({ apiUrl: API_URL } as ResolvedAuthConfig);
    const result = await action(MFA_TOKEN);
    expect(result.success).toBe(false);
    expect(result.message).not.toContain('private backend detail');
    expect(result).not.toHaveProperty('tokens');
    expect(fetchMock).toHaveBeenCalledOnce();
  });
});
