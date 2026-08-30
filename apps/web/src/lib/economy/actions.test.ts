import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(),
  createServerClient: vi.fn(),
  postEconomyConversionsHardToSoft: vi.fn(),
  revalidatePath: vi.fn(),
}));

vi.mock('@/auth', () => ({
  auth: mocks.auth,
  getToken: mocks.getToken,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    EconomyModule: class {
      postEconomyConversionsHardToSoft = mocks.postEconomyConversionsHardToSoft;
    },
  },
}));

vi.mock('next/cache', () => ({
  revalidatePath: mocks.revalidatePath,
}));

import { convertHardToSoftAction } from './actions';

describe('economy actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'user-1' }, tenantId: 'tenant-1' });
    mocks.getToken.mockResolvedValue('access-token');
    mocks.createServerClient.mockReturnValue({});
    mocks.postEconomyConversionsHardToSoft.mockResolvedValue({ ok: true, data: {} });
  });

  it('lets the server policy calculate the hard-to-soft conversion fee', async () => {
    await expect(convertHardToSoftAction(25, ' conversion-1 ')).resolves.toEqual({
      success: true,
      message: 'Conversion recorded in the Economy journal.',
    });

    expect(mocks.postEconomyConversionsHardToSoft).toHaveBeenCalledWith({
      principalHardCoinUnits: 25,
      idempotencyKey: 'conversion-1',
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/', 'layout');
  });
});
