import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { getMfaMethods, MfaVerificationError, setupTotpMfa } from '../../src/runtime/auth/extended-operations.js';
import { AuthMultiFactorModule } from '../../src/generated/modules/auth-multi-factor.gen.js';

const API_URL = 'https://api.gameguild.gg';
const ACCESS_TOKEN = 'settings-access-token';
const SETUP = {
  // InitiateTotpSetup sets the provisioning fields, but leaves this DTO flag at
  // its default false. HTTP status and complete provisioning data are authoritative.
  isSuccess: false,
  errorMessage: null,
  secretKey: 'SYNTHETIC-TOTP-SECRET',
  qrCodeData: null,
  qrCodeUri: 'otpauth://totp/Test:user?secret=SYNTHETIC-TOTP-SECRET',
  expiresAt: '2026-11-04T06:30:00+00:00',
  backupCodes: [],
};
const METHODS = {
  defaultMethod: 'Totp',
  methods: [
    { method: 'Totp', name: 'Authenticator', description: null, isAvailable: true, isEnabled: true, priority: 1 },
    { method: 'Sms', name: 'SMS', description: null, isAvailable: false, isEnabled: true, priority: 2 },
    { method: 'Email', name: 'Email', description: null, isAvailable: true, isEnabled: false, priority: 3 },
    { method: 'Push', name: 'Push', description: null, isAvailable: false, isEnabled: false, priority: 4 },
    { method: 'BackupCode', name: 'Backup codes', description: null, isAvailable: true, isEnabled: false, priority: 5 },
  ],
};

const fetchMock = vi.fn<typeof fetch>();

beforeEach(() => {
  fetchMock.mockReset();
  vi.stubGlobal('fetch', fetchMock);
});

afterEach(() => {
  vi.useRealTimers();
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe('authenticated MFA settings contracts', () => {
  it('uses the served TOTP action route, authenticated generated method and public result shape', async () => {
    const generated = vi.spyOn(AuthMultiFactorModule.prototype, 'postAuthMfaTotpSetup');
    fetchMock.mockImplementation((url, options) => {
      const requestUrl = url instanceof Request ? url.url : url.toString();
      if (requestUrl !== `${API_URL}/v1/auth/mfa/totp:setup`) return Promise.resolve(Response.json({}, { status: 404 }));
      if (new Headers(options?.headers).get('Authorization') !== `Bearer ${ACCESS_TOKEN}`) return Promise.resolve(Response.json({}, { status: 401 }));
      return Promise.resolve(Response.json(SETUP));
    });

    await expect(setupTotpMfa(API_URL, ACCESS_TOKEN)).resolves.toEqual({
      secret: SETUP.secretKey,
      qrCodeUri: SETUP.qrCodeUri,
      backupCodes: [],
      requiresVerification: true,
    });
    expect(generated).toHaveBeenCalledOnce();
    expect(fetchMock).toHaveBeenCalledOnce();
    const [, options] = fetchMock.mock.calls[0];
    expect(options?.method).toBe('POST');
    expect(options?.body).toBeUndefined();
    expect(options?.redirect).toBe('error');
  });

  it('maps backup codes and omits nullable optional fields', async () => {
    fetchMock.mockResolvedValueOnce(Response.json({ ...SETUP, isSuccess: true, backupCodes: ['one', 'two'] }));
    expect((await setupTotpMfa(API_URL, ACCESS_TOKEN)).backupCodes).toEqual(['one', 'two']);
    fetchMock.mockResolvedValueOnce(Response.json({ ...SETUP, backupCodes: null }));
    expect((await setupTotpMfa(API_URL, ACCESS_TOKEN)).backupCodes).toBeUndefined();
  });

  it.each([400, 401, 403, 404, 429, 500])('rejects HTTP %i with the public setup error and never refreshes credentials', async (status) => {
    fetchMock.mockResolvedValueOnce(Response.json({ title: 'Setup denied', detail: 'Cannot enroll this actor' }, { status }));
    await expect(setupTotpMfa(API_URL, ACCESS_TOKEN)).rejects.toThrow(MfaVerificationError);
    expect(fetchMock).toHaveBeenCalledOnce();
  });

  it.each(
    [
      {},
      null,
      [],
      { secret: SETUP.secretKey, qrCodeUri: SETUP.qrCodeUri, requiresVerification: true },
      { ...SETUP, secretKey: null },
      { ...SETUP, secretKey: ' ' },
      { ...SETUP, secretKey: 42 },
      { ...SETUP, qrCodeUri: null },
      { ...SETUP, qrCodeUri: 'javascript:alert(1)' },
      { ...SETUP, backupCodes: [42] },
      { ...SETUP, expiresAt: 'yesterday' },
      { ...SETUP, errorMessage: 'Enrollment denied' },
    ].map((body) => ({ body })),
  )('rejects incomplete, malformed or denied provisioning instead of presenting setup as ready: %j', async ({ body }) => {
    fetchMock.mockResolvedValueOnce(Response.json(body));
    await expect(setupTotpMfa(API_URL, ACCESS_TOKEN)).rejects.toThrow(MfaVerificationError);
    expect(fetchMock).toHaveBeenCalledOnce();
  });

  it('maps available method objects to the existing string API without requiring enrollment', async () => {
    const generated = vi.spyOn(AuthMultiFactorModule.prototype, 'getAuthMfaMethods');
    fetchMock.mockResolvedValueOnce(Response.json(METHODS));
    await expect(getMfaMethods(API_URL, ACCESS_TOKEN)).resolves.toEqual(['totp', 'email', 'backup_code']);
    expect(generated).toHaveBeenCalledOnce();
    expect(fetchMock).toHaveBeenCalledOnce();
    const [url, options] = fetchMock.mock.calls[0];
    expect(url).toBe(`${API_URL}/v1/auth/mfa/methods`);
    expect(options?.method).toBe('GET');
    expect(new Headers(options?.headers).get('Authorization')).toBe(`Bearer ${ACCESS_TOKEN}`);
    expect(options?.body).toBeUndefined();
    expect(options?.redirect).toBe('error');
  });

  it('maps all generated enum values to stable public names', async () => {
    fetchMock.mockResolvedValueOnce(
      Response.json({
        methods: ['Totp', 'BackupCode', 'Sms', 'Email', 'WebAuthn', 'Push'].map((method) => ({
          method,
          name: null,
          description: null,
          isAvailable: true,
          isEnabled: false,
          priority: 1,
        })),
      }),
    );
    await expect(getMfaMethods(API_URL, ACCESS_TOKEN)).resolves.toEqual(['totp', 'backup_code', 'sms', 'email', 'webauthn', 'push']);
  });

  it.each([{ methods: null }, { methods: [] }])('accepts a valid empty method list: %j', async ({ methods }) => {
    fetchMock.mockResolvedValueOnce(Response.json({ methods }));
    await expect(getMfaMethods(API_URL, ACCESS_TOKEN)).resolves.toEqual([]);
  });

  it.each([401, 403, 429, 500])('preserves the method discovery HTTP %i fallback without retrying or refreshing', async (status) => {
    fetchMock.mockResolvedValueOnce(Response.json({ detail: 'Unavailable' }, { status }));
    await expect(getMfaMethods(API_URL, ACCESS_TOKEN)).resolves.toEqual([]);
    expect(fetchMock).toHaveBeenCalledOnce();
  });

  it.each([
    {},
    { methods: ['totp', 'sms'] },
    { methods: [{ ...METHODS.methods[0], method: 'Unknown' }] },
    { methods: [{ ...METHODS.methods[0], isAvailable: 'true' }] },
    { methods: [{ method: 'Totp' }] },
  ])('rejects invalid discovery data rather than returning strings or objects from an unchecked cast: %j', async (body) => {
    fetchMock.mockResolvedValueOnce(Response.json(body));
    await expect(getMfaMethods(API_URL, ACCESS_TOKEN)).rejects.toThrow(MfaVerificationError);
  });

  it.each(['', '   '])('makes no authenticated request with an empty bearer: %j', async (token) => {
    await expect(setupTotpMfa(API_URL, token)).rejects.toThrow(MfaVerificationError);
    await expect(getMfaMethods(API_URL, token)).resolves.toEqual([]);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  for (const operation of [setupTotpMfa, getMfaMethods]) {
    it(`${operation.name} wraps network failure in the public error without exposing secrets`, async () => {
      const log = vi.spyOn(console, 'log').mockImplementation(() => undefined);
      const group = vi.spyOn(console, 'groupCollapsed').mockImplementation(() => undefined);
      fetchMock.mockRejectedValueOnce(new Error(`Network refused ${ACCESS_TOKEN}`));
      const error: unknown = await operation(API_URL, ACCESS_TOKEN).catch((error: unknown) => error);
      expect(error).toBeInstanceOf(MfaVerificationError);
      if (!(error instanceof Error)) throw new Error('Expected an authentication error');
      expect(error.message).not.toContain(ACCESS_TOKEN);
      expect(log).not.toHaveBeenCalled();
      expect(group).not.toHaveBeenCalled();
      expect(fetchMock).toHaveBeenCalledOnce();
    });

    it(`${operation.name} rejects invalid JSON with a typed error`, async () => {
      fetchMock.mockResolvedValueOnce(new Response('{', { headers: { 'Content-Type': 'application/json' } }));
      await expect(operation(API_URL, ACCESS_TOKEN)).rejects.toThrow(MfaVerificationError);
    });

    it(`${operation.name} has a five-second request deadline even if the network ignores abort`, async () => {
      vi.useFakeTimers();
      fetchMock.mockImplementation(() => new Promise<Response>(() => undefined));
      const settled = expect(operation(API_URL, ACCESS_TOKEN)).rejects.toThrow(MfaVerificationError);
      await vi.advanceTimersByTimeAsync(5_000);
      await settled;
      expect(fetchMock).toHaveBeenCalledOnce();
      expect(fetchMock.mock.calls[0][1]?.signal?.aborted).toBe(true);
    });
  }
});
