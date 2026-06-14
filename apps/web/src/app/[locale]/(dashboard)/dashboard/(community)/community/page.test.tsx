import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getCommunityStats: vi.fn(),
}));

vi.mock('@/lib/community', () => ({
  getCommunityStats: mocks.getCommunityStats,
}));

import CommunityOverviewPage from './page';

describe('community overview page', () => {
  it('renders live summary panels instead of generic empty placeholders', async () => {
    mocks.getCommunityStats.mockResolvedValue({
      totalMembers: 20,
      activeMembers: 5,
      newMembersThisMonth: 3,
      totalPosts: 7,
      totalGroups: 2,
      openTickets: 1,
    });

    render(await CommunityOverviewPage());

    expect(screen.getByText('Community Overview')).toBeInTheDocument();
    expect(screen.getByText('Open support requests')).toBeInTheDocument();
    expect(screen.getByText('Published posts and discussions')).toBeInTheDocument();
    expect(screen.getByText('Active member rate')).toBeInTheDocument();
    expect(screen.getByText('25%')).toBeInTheDocument();
    expect(screen.queryByText('No data available yet.')).not.toBeInTheDocument();
    expect(screen.queryByText('No recent activity to display.')).not.toBeInTheDocument();
  });
});
