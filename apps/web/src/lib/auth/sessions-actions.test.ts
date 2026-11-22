import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getAuthSessions: vi.fn(),
  getAuthSessionsAnalyzeSecurity: vi.fn(),
  getAuthTrustedDevices: vi.fn(),
  revalidatePath: vi.fn(),
}));

vi.mock('@/auth', () => ({ getToken: vi.fn().mockResolvedValue('token') }));

vi.mock('next/cache', () => ({ revalidatePath: mocks.revalidatePath }));

vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn(() => ({})),
  GeneratedApi: {
    AuthSessionsModule: class {
      getAuthSessions = mocks.getAuthSessions;
      getAuthSessionsAnalyzeSecurity = mocks.getAuthSessionsAnalyzeSecurity;
    },
    AuthTrustedDevicesModule: class {
      getAuthTrustedDevices = mocks.getAuthTrustedDevices;
    },
  },
}));

const { loadSessionsData } = await import('./sessions-actions');

const SESSIONS = [{ id: 's1', isCurrent: true }] as never;
const ANALYSIS = { activeSessionCount: 1 } as never;
const TRUSTED_DEVICES = [{ id: 'd1' }] as never;

describe('loadSessionsData (issue #1298 J4: failures surface, never silently blank)', () => {
  beforeEach(() => {
    mocks.getAuthSessions.mockReset();
    mocks.getAuthSessionsAnalyzeSecurity.mockReset();
    mocks.getAuthTrustedDevices.mockReset();
  });

  it('returns data and loadStatus ok when all calls succeed', async () => {
    mocks.getAuthSessions.mockResolvedValue({ ok: true, data: SESSIONS });
    mocks.getAuthSessionsAnalyzeSecurity.mockResolvedValue({ ok: true, data: ANALYSIS });
    mocks.getAuthTrustedDevices.mockResolvedValue({ ok: true, data: TRUSTED_DEVICES });

    await expect(loadSessionsData()).resolves.toEqual({
      sessions: SESSIONS,
      analysis: ANALYSIS,
      trustedDevices: TRUSTED_DEVICES,
      loadStatus: 'ok',
    });
  });

  it('reports contract drift instead of rendering empty lists when safeParse throws', async () => {
    const contractDrift = new Error('[GET /v1/auth/sessions] Response validation failed');
    mocks.getAuthSessions.mockRejectedValue(contractDrift);
    mocks.getAuthSessionsAnalyzeSecurity.mockRejectedValue(contractDrift);
    mocks.getAuthTrustedDevices.mockRejectedValue(contractDrift);

    await expect(loadSessionsData()).resolves.toEqual({
      sessions: [],
      analysis: null,
      trustedDevices: [],
      loadStatus: 'contractError',
    });
  });

  it('keeps successful sections and reports the most actionable aggregate status', async () => {
    mocks.getAuthSessions.mockRejectedValue(new Error('drift'));
    mocks.getAuthSessionsAnalyzeSecurity.mockResolvedValue({ ok: true, data: ANALYSIS });
    mocks.getAuthTrustedDevices.mockResolvedValue({ ok: false, error: { status: 500 } });

    await expect(loadSessionsData()).resolves.toEqual({
      sessions: [],
      analysis: ANALYSIS,
      trustedDevices: [],
      loadStatus: 'serverError',
    });
  });

  it('classifies non-ok results by their HTTP status', async () => {
    mocks.getAuthSessions.mockResolvedValue({ ok: false, error: { status: 401 } });
    mocks.getAuthSessionsAnalyzeSecurity.mockResolvedValue({ ok: false, error: { status: 500 } });
    mocks.getAuthTrustedDevices.mockResolvedValue({ ok: false, error: { status: 404 } });

    await expect(loadSessionsData()).resolves.toEqual({
      sessions: [],
      analysis: null,
      trustedDevices: [],
      loadStatus: 'unauthorized',
    });
  });
});
