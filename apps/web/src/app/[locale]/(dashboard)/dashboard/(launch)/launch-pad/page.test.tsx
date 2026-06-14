import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  completeLaunchChecklistItem: vi.fn(),
  createLaunchPlan: vi.fn(),
  getLaunchPadDashboard: vi.fn(),
  getLaunchProjectOptions: vi.fn(),
  getPlanReadiness: vi.fn(),
  normalizeLaunchStatus: vi.fn(),
  publishLaunchPlan: vi.fn(),
}));

vi.mock('@/lib/launch-pad/actions', () => ({
  completeLaunchChecklistItem: mocks.completeLaunchChecklistItem,
  createLaunchPlan: mocks.createLaunchPlan,
  publishLaunchPlan: mocks.publishLaunchPlan,
}));

vi.mock('@/lib/launch-pad', () => ({
  getLaunchPadDashboard: mocks.getLaunchPadDashboard,
  getLaunchProjectOptions: mocks.getLaunchProjectOptions,
  getPlanReadiness: mocks.getPlanReadiness,
  normalizeLaunchStatus: mocks.normalizeLaunchStatus,
}));

import LaunchPadPage from './page';

describe('launch pad dashboard page', () => {
  it('renders persisted launch plans and project-backed creation controls', async () => {
    mocks.getPlanReadiness.mockReturnValue(50);
    mocks.normalizeLaunchStatus.mockReturnValue('Preparing');
    mocks.getLaunchPadDashboard.mockResolvedValue([
      {
        id: 'launch-plan-1',
        projectId: 'project-1',
        project: { title: 'Arena Tactics' },
        name: 'Steam launch',
        positioning: 'A tactical prototype release for early strategy testers.',
        targetLaunchAt: '2026-07-01T12:00:00.000Z',
        status: 'Preparing',
        channels: ['Steam', 'Newsletter'],
        checklistItems: [
          {
            id: 'checklist-1',
            title: 'Landing page approved',
            category: 'Storefront',
            isRequired: true,
            isComplete: false,
          },
        ],
      },
    ]);
    mocks.getLaunchProjectOptions.mockResolvedValue([
      {
        id: 'project-1',
        title: 'Arena Tactics',
        slug: 'arena-tactics',
        status: 'Draft',
      },
    ]);

    render(await LaunchPadPage());

    expect(screen.getByRole('heading', { name: 'Launch Pad' })).toBeInTheDocument();
    expect(screen.getByText('Steam launch')).toBeInTheDocument();
    expect(screen.getAllByText('Arena Tactics')).toHaveLength(2);
    expect(screen.getByText('50%')).toBeInTheDocument();
    expect(screen.getByText('Steam, Newsletter')).toBeInTheDocument();
    expect(screen.getByText('Landing page approved')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Mark done' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Publish launch' })).toBeDisabled();
    expect(screen.getAllByText('Create launch plan')).toHaveLength(2);
    expect(screen.getByLabelText('Launch name')).toBeRequired();
    expect(screen.getByRole('button', { name: 'Create launch plan' })).toBeEnabled();
  });
});
