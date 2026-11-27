import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getAuthMfa: vi.fn(),
  getAuthMfaMethods: vi.fn(),
  getAuthMfaBackupCodes: vi.fn(),
  regenerate: vi.fn(),
  emailSetup: vi.fn(),
  emailComplete: vi.fn(),
  revalidatePath: vi.fn(),
}));

vi.mock('@/auth', () => ({ getToken: vi.fn().mockResolvedValue('token') }));

vi.mock('next/cache', () => ({ revalidatePath: mocks.revalidatePath }));

vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn(() => ({})),
  GeneratedApi: {
    AuthMultiFactorModule: class {
      getAuthMfa = mocks.getAuthMfa;
      getAuthMfaMethods = mocks.getAuthMfaMethods;
      getAuthMfaBackupCodes = mocks.getAuthMfaBackupCodes;
      postAuthMfaBackupCodesRegenerate = mocks.regenerate;
      postAuthMfaEmailSetup = mocks.emailSetup;
      postAuthMfaEmailComplete = mocks.emailComplete;
    },
  },
}));

const { completeEmailEnrollmentAction, loadMfaData, regenerateBackupCodesAction, startEmailEnrollmentAction } =
  await import('./mfa-actions');

const CONFIGURATION = { isEnabled: true } as never;
const METHODS = { methods: [] } as never;
const BACKUP_CODES = { hasBackupCodes: false } as never;

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

  it('reports a set with no codes as a failure instead of an empty success (issue #1298 J2)', async () => {
    mocks.regenerate.mockResolvedValueOnce({ ok: true, data: { codes: [] } });
    const result = await regenerateBackupCodesAction(crypto.randomUUID());
    expect(result).toMatchObject({ success: false, status: 'error', emptySet: true });
    expect(result.codes).toBeUndefined();
  });
});

describe('email-codes enrollment transport (issue #1298 J2)', () => {
  it('starts enrollment with no request body: the server resolves the address', async () => {
    mocks.emailSetup.mockResolvedValueOnce({ ok: true, data: { emailMasked: 'j***@example.com', expiresInSeconds: 600 } });
    const result = await startEmailEnrollmentAction();
    expect(mocks.emailSetup).toHaveBeenCalledWith();
    expect(result).toMatchObject({
      success: true,
      status: 'success',
      emailSetup: { emailMasked: 'j***@example.com', expiresInSeconds: 600 },
    });
  });

  it('carries the server reason when the code is throttled', async () => {
    mocks.emailSetup.mockResolvedValueOnce({
      ok: false,
      error: { status: 400, message: 'A verification code was sent recently. Wait before requesting another.' },
    });
    const result = await startEmailEnrollmentAction();
    expect(result).toMatchObject({ success: false, status: 'error', emailSetup: undefined });
    expect(result.message).toBe('A verification code was sent recently. Wait before requesting another.');
  });

  it('completes enrollment with the mailed code alone, then refreshes the security page', async () => {
    mocks.revalidatePath.mockClear();
    mocks.emailComplete.mockResolvedValueOnce({ ok: true, data: { message: 'Email MFA setup completed successfully' } });
    const result = await completeEmailEnrollmentAction('123456');
    expect(mocks.emailComplete).toHaveBeenCalledWith({ code: '123456' });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/workspace/settings/security');
    expect(result).toEqual({ success: true, status: 'success', message: undefined });
  });
});

describe('loadMfaData (issue #1298 J4: failures surface, never silently blank)', () => {
  beforeEach(() => {
    mocks.getAuthMfa.mockReset();
    mocks.getAuthMfaMethods.mockReset();
    mocks.getAuthMfaBackupCodes.mockReset();
  });

  it('returns data and loadStatus ok when all calls succeed', async () => {
    mocks.getAuthMfa.mockResolvedValue({ ok: true, data: CONFIGURATION });
    mocks.getAuthMfaMethods.mockResolvedValue({ ok: true, data: METHODS });
    mocks.getAuthMfaBackupCodes.mockResolvedValue({ ok: true, data: BACKUP_CODES });

    await expect(loadMfaData()).resolves.toEqual({
      configuration: CONFIGURATION,
      methods: METHODS,
      backupCodes: BACKUP_CODES,
      loadStatus: 'ok',
    });
  });

  it('reports contract drift instead of rendering empty MFA panels when safeParse throws', async () => {
    const contractDrift = new Error('[GET /v1/auth/mfa] Response validation failed');
    mocks.getAuthMfa.mockRejectedValue(contractDrift);
    mocks.getAuthMfaMethods.mockRejectedValue(contractDrift);
    mocks.getAuthMfaBackupCodes.mockRejectedValue(contractDrift);

    await expect(loadMfaData()).resolves.toEqual({
      configuration: null,
      methods: null,
      backupCodes: null,
      loadStatus: 'contractError',
    });
  });

  it('keeps successful sections and reports the most actionable aggregate status', async () => {
    mocks.getAuthMfa.mockRejectedValue(new Error('drift'));
    mocks.getAuthMfaMethods.mockResolvedValue({ ok: false, error: { status: 500 } });
    mocks.getAuthMfaBackupCodes.mockResolvedValue({ ok: true, data: BACKUP_CODES });

    await expect(loadMfaData()).resolves.toEqual({
      configuration: null,
      methods: null,
      backupCodes: BACKUP_CODES,
      loadStatus: 'serverError',
    });
  });

  it('classifies non-ok results by their HTTP status', async () => {
    mocks.getAuthMfa.mockResolvedValue({ ok: false, error: { status: 401 } });
    mocks.getAuthMfaMethods.mockResolvedValue({ ok: false, error: { status: 500 } });
    mocks.getAuthMfaBackupCodes.mockResolvedValue({ ok: false, error: { status: 404 } });

    await expect(loadMfaData()).resolves.toEqual({
      configuration: null,
      methods: null,
      backupCodes: null,
      loadStatus: 'unauthorized',
    });
  });
});
