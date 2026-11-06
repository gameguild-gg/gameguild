import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getAuthMfa: vi.fn(),
  getAuthMfaMethods: vi.fn(),
  getAuthMfaBackupCodes: vi.fn(),
  regenerate: vi.fn(),
  smsSetup: vi.fn(),
  smsComplete: vi.fn(),
  revalidatePath: vi.fn(),
  sendSignInSms: vi.fn(),
  smsChallengeFactory: vi.fn(),
}));

vi.mock('@/auth', () => ({
  getToken: vi.fn().mockResolvedValue('token'),
  authConfig: { apiUrl: 'https://api.example.test' },
}));

vi.mock('next/cache', () => ({ revalidatePath: mocks.revalidatePath }));

vi.mock('@game-guild/client', () => ({
  createMfaEnrollmentCompleteAction: vi.fn(() => vi.fn()),
  createMfaSmsChallengeAction: mocks.smsChallengeFactory.mockImplementation(() => mocks.sendSignInSms),
  createServerClient: vi.fn(() => ({})),
  GeneratedApi: {
    AuthMultiFactorModule: class {
      getAuthMfa = mocks.getAuthMfa;
      getAuthMfaMethods = mocks.getAuthMfaMethods;
      getAuthMfaBackupCodes = mocks.getAuthMfaBackupCodes;
      postAuthMfaBackupCodesRegenerate = mocks.regenerate;
      postAuthMfaSmsSetup = mocks.smsSetup;
      postAuthMfaSmsComplete = mocks.smsComplete;
    },
  },
}));

const { loadMfaData, regenerateBackupCodesAction, startSmsEnrollmentAction, completeSmsEnrollmentAction } = await import('./mfa-actions');

const CONFIGURATION = { isEnabled: true } as never;
const METHODS = { methods: [] } as never;
const BACKUP_CODES = { hasBackupCodes: false } as never;

describe('sign-in SMS server action contract', () => {
  beforeEach(() => {
    mocks.sendSignInSms.mockReset();
    mocks.smsSetup.mockClear();
    mocks.smsComplete.mockClear();
  });

  it('exports the action used by the form and delegates its continuation to the SDK factory', async () => {
    const actions = await import('./mfa-sign-in-actions');
    const { authConfig } = await import('@/auth');
    const token = (crypto.randomUUID() + crypto.randomUUID()).replaceAll('-', '').slice(0, 43);
    const delivered = { success: true, phoneNumberMasked: '***4567', expiresInSeconds: 600 };
    mocks.sendSignInSms.mockResolvedValueOnce(delivered);

    expect(actions).toHaveProperty('sendMfaSmsChallengeAction', expect.any(Function));
    const send = Reflect.get(actions, 'sendMfaSmsChallengeAction') as (value: string) => Promise<unknown>;
    await expect(send(token)).resolves.toEqual(delivered);
    expect(mocks.smsChallengeFactory).toHaveBeenCalledWith(authConfig);
    expect(mocks.sendSignInSms).toHaveBeenCalledExactlyOnceWith(token);
    expect(mocks.smsSetup).not.toHaveBeenCalled();
    expect(mocks.smsComplete).not.toHaveBeenCalled();
  });
});

describe('backup-code password re-verification transport', () => {
  it('forwards the password and returns the newly issued codes', async () => {
    const password = crypto.randomUUID();
    mocks.regenerate.mockResolvedValueOnce({ ok: true, data: { codes: ['new-recovery-code'] } });
    const result = await regenerateBackupCodesAction(password);
    expect(mocks.regenerate).toHaveBeenLastCalledWith({ password });
    expect(result).toMatchObject({ success: true, codes: ['new-recovery-code'] });
  });

  it('returns no recovery codes after the API refuses the password', async () => {
    mocks.regenerate.mockResolvedValueOnce({ ok: false, error: { message: 'Password verification failed', status: 400 } });
    const result = await regenerateBackupCodesAction(crypto.randomUUID());
    expect(result).toMatchObject({ success: false });
    expect(result.codes).toBeUndefined();
  });
});

describe('loadMfaData (issue #864 contract-drift defense)', () => {
  beforeEach(() => {
    mocks.getAuthMfa.mockReset();
    mocks.getAuthMfaMethods.mockReset();
    mocks.getAuthMfaBackupCodes.mockReset();
  });

  it('returns data when all calls succeed', async () => {
    mocks.getAuthMfa.mockResolvedValue({ ok: true, data: CONFIGURATION });
    mocks.getAuthMfaMethods.mockResolvedValue({ ok: true, data: METHODS });
    mocks.getAuthMfaBackupCodes.mockResolvedValue({ ok: true, data: BACKUP_CODES });

    await expect(loadMfaData()).resolves.toEqual({
      configuration: CONFIGURATION,
      methods: METHODS,
      backupCodes: BACKUP_CODES,
    });
  });

  it('degrades to null when safeParse throws on contract mismatch instead of crashing the RSC render', async () => {
    const contractDrift = new Error('[GET /v1/auth/mfa] Response validation failed');
    mocks.getAuthMfa.mockRejectedValue(contractDrift);
    mocks.getAuthMfaMethods.mockRejectedValue(contractDrift);
    mocks.getAuthMfaBackupCodes.mockRejectedValue(contractDrift);

    await expect(loadMfaData()).resolves.toEqual({
      configuration: null,
      methods: null,
      backupCodes: null,
    });
  });

  it('degrades individual failures to null while keeping successful sections', async () => {
    mocks.getAuthMfa.mockRejectedValue(new Error('drift'));
    mocks.getAuthMfaMethods.mockResolvedValue({ ok: false, error: { status: 500 } });
    mocks.getAuthMfaBackupCodes.mockResolvedValue({ ok: true, data: BACKUP_CODES });

    await expect(loadMfaData()).resolves.toEqual({
      configuration: null,
      methods: null,
      backupCodes: BACKUP_CODES,
    });
  });

  it('degrades non-ok results to null', async () => {
    mocks.getAuthMfa.mockResolvedValue({ ok: false, error: { status: 401 } });
    mocks.getAuthMfaMethods.mockResolvedValue({ ok: false, error: { status: 500 } });
    mocks.getAuthMfaBackupCodes.mockResolvedValue({ ok: false, error: { status: 404 } });

    await expect(loadMfaData()).resolves.toEqual({
      configuration: null,
      methods: null,
      backupCodes: null,
    });
  });
});

describe('SMS enrollment guarded transport', () => {
  it('passes the password and phone to setup without client-controlled account selectors', async () => {
    const password = crypto.randomUUID();
    mocks.smsSetup.mockResolvedValueOnce({ ok: true, data: { phoneNumberMasked: '***4567', expiresInSeconds: 600 } });
    const result = await startSmsEnrollmentAction('+15551234567', password);
    expect(mocks.smsSetup).toHaveBeenLastCalledWith({ phoneNumber: '+15551234567', password });
    expect(result).toMatchObject({ success: true, smsSetup: { phoneNumberMasked: '***4567' } });
  });

  it('returns no pending enrollment after the API denies re-verification', async () => {
    mocks.smsSetup.mockResolvedValueOnce({ ok: false, error: { status: 400, message: 'Password verification failed' } });
    const result = await startSmsEnrollmentAction('+15551234567', crypto.randomUUID());
    expect(result.success).toBe(false);
    expect(result.smsSetup).toBeUndefined();
  });

  it('completes SMS with the delivered code and no authenticator provisioning secret', async () => {
    mocks.smsComplete.mockResolvedValueOnce({ ok: true, data: { message: 'SMS MFA setup completed' } });
    await expect(completeSmsEnrollmentAction('123456')).resolves.toMatchObject({ success: true });
    expect(mocks.smsComplete).toHaveBeenLastCalledWith({ code: '123456' });
  });
});
