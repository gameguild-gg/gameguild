import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  forbidden: vi.fn(() => {
    throw new Error('forbidden');
  }),
  getDashboardContexts: vi.fn(),
}));

vi.mock('next/navigation', () => ({ forbidden: mocks.forbidden }));
vi.mock('@/lib/dashboard-contexts', () => ({
  getDashboardContexts: mocks.getDashboardContexts,
  hasAnyDashboardCapability: (capabilities: string[], ...required: string[]) =>
    required.some((capability) => capabilities.some((value) =>
      capability.endsWith('.') ? value.startsWith(capability) : value === capability)),
}));

import TestingLabLayout from './layout';

describe('TestingLabLayout', () => {
  beforeEach(() => vi.clearAllMocks());

  it.each([
    'TestingLab.ManageEvents',
    'TestingLab.ReviewApplications',
    'TestingLab.ManageParticipants',
    'TestingLab.ManageFeedback',
    'TestingLab.ViewAnalytics',
    'TestingLab.ManageSettings',
  ])('allows the management shell for %s', async (capability) => {
    mocks.getDashboardContexts.mockResolvedValue({ capabilities: [capability] });

    const result = await TestingLabLayout({ children: 'management' as unknown as ReactNode });

    expect(result.props.children).toBe('management');
    expect(mocks.forbidden).not.toHaveBeenCalled();
  });

  it('rejects participation-only access from the management shell', async () => {
    mocks.getDashboardContexts.mockResolvedValue({ capabilities: ['TestingLab.Participate'] });

    await expect(TestingLabLayout({ children: 'management' })).rejects.toThrow('forbidden');
  });
});
