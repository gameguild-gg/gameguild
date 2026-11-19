import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getAuthMfa: vi.fn(),
  getAuthMfaMethods: vi.fn(),
  getAuthMfaBackupCodes: vi.fn(),
  regenerate: vi.fn(),
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
    },
  },
}));

const { loadMfaData, regenerateBackupCodesAction } = await import('./mfa-actions');

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
