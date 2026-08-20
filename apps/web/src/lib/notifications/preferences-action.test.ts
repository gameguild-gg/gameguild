import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(),
  revalidatePath: vi.fn(),
  putPreferences: vi.fn(),
  putMutedTypes: vi.fn(),
  putDigestFrequency: vi.fn(),
  putQuietHours: vi.fn(),
}));

vi.mock('@/auth', () => ({
  auth: mocks.auth,
  getToken: mocks.getToken,
}));

vi.mock('next/cache', () => ({ revalidatePath: mocks.revalidatePath }));

vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn(() => ({})),
  GeneratedApi: {
    NotificationsModule: class {
      putApiNotificationsPreferences = mocks.putPreferences;
      putApiNotificationsPreferencesMutedTypes = mocks.putMutedTypes;
      putApiNotificationsPreferencesDigestFrequency = mocks.putDigestFrequency;
      putApiNotificationsPreferencesQuietHours = mocks.putQuietHours;
    },
  },
}));

const {
  updatePreferenceFlagsAction,
  updateMutedTypesAction,
  updateDigestFrequencyAction,
  updateQuietHoursAction,
} = await import('./preferences-action');

function apiError(status: number) {
  return { ok: false as const, error: { name: 'ApiError' as const, status, code: 'ERROR' as const, message: 'boom' } };
}

describe('notification preferences actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });
    mocks.getToken.mockResolvedValue('access-token');
    mocks.putPreferences.mockResolvedValue({ ok: true, data: {} });
    mocks.putMutedTypes.mockResolvedValue({ ok: true, data: { mutedTypes: [] } });
    mocks.putDigestFrequency.mockResolvedValue({ ok: true, data: { emailDigestFrequency: 'Daily' } });
    mocks.putQuietHours.mockResolvedValue({ ok: true, data: undefined });
  });

  it('sends only the toggled channel flag to the preferences endpoint', async () => {
    const result = await updatePreferenceFlagsAction({ emailEnabled: false });

    expect(result).toEqual({ success: true, status: 'success' });
    expect(mocks.putPreferences).toHaveBeenCalledTimes(1);
    expect(mocks.putPreferences).toHaveBeenCalledWith({ emailEnabled: false });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/', 'layout');
  });

  it('sends several flags in one call', async () => {
    await updatePreferenceFlagsAction({ marketingEnabled: false, smsEnabled: true });

    expect(mocks.putPreferences).toHaveBeenCalledWith({
      marketingEnabled: false,
      smsEnabled: true,
    });
  });

  it('replaces the full muted types list', async () => {
    const result = await updateMutedTypesAction(['MonthlyStatement', 'FeatureAnnouncement']);

    expect(result).toEqual({ success: true, status: 'success' });
    expect(mocks.putMutedTypes).toHaveBeenCalledWith({
      types: ['MonthlyStatement', 'FeatureAnnouncement'],
    });
  });

  it('sends an empty list to clear all mutes', async () => {
    await updateMutedTypesAction([]);

    expect(mocks.putMutedTypes).toHaveBeenCalledWith({ types: [] });
  });

  it('sends the digest frequency and null to disable it', async () => {
    await updateDigestFrequencyAction('Weekly');
    expect(mocks.putDigestFrequency).toHaveBeenCalledWith({ frequency: 'Weekly' });

    await updateDigestFrequencyAction(null);
    expect(mocks.putDigestFrequency).toHaveBeenCalledWith({ frequency: null });
  });

  it('sends quiet hours with timezone and nulls to clear the window', async () => {
    await updateQuietHoursAction('22:00:00', '07:00:00', 'America/Sao_Paulo');
    expect(mocks.putQuietHours).toHaveBeenCalledWith({
      start: '22:00:00',
      end: '07:00:00',
      timezone: 'America/Sao_Paulo',
    });

    await updateQuietHoursAction(null, null, null);
    expect(mocks.putQuietHours).toHaveBeenCalledWith({
      start: null,
      end: null,
      timezone: null,
    });
  });

  it('returns unauthorized without calling the API when the session is missing', async () => {
    mocks.auth.mockResolvedValue(null);

    const result = await updatePreferenceFlagsAction({ emailEnabled: true });

    expect(result).toEqual({ success: false, status: 'unauthorized' });
    expect(mocks.putPreferences).not.toHaveBeenCalled();
    expect(mocks.revalidatePath).not.toHaveBeenCalled();
  });

  it.each([
    ['flags', () => updatePreferenceFlagsAction({ emailEnabled: true }), 'putPreferences'],
    ['muted types', () => updateMutedTypesAction(['Billing']), 'putMutedTypes'],
    ['digest', () => updateDigestFrequencyAction('Daily'), 'putDigestFrequency'],
    ['quiet hours', () => updateQuietHoursAction('22:00:00', '07:00:00', 'UTC'), 'putQuietHours'],
  ])('maps %s API failures to error and skips revalidation', async (_name, run, mockKey) => {
    mocks[mockKey].mockResolvedValue(apiError(500));

    const result = await run();

    expect(result).toEqual({ success: false, status: 'error' });
    expect(mocks.revalidatePath).not.toHaveBeenCalled();
  });
});
