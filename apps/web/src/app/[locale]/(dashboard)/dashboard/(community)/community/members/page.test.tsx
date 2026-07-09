import { render, screen, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getMembers: vi.fn(),
  getCommunityStats: vi.fn(),
  getGroups: vi.fn(),
  getSupportTickets: vi.fn(),
}));

vi.mock('@/lib/community', () => ({
  getMembers: mocks.getMembers,
  getCommunityStats: mocks.getCommunityStats,
  getGroups: mocks.getGroups,
  getSupportTickets: mocks.getSupportTickets,
}));

import MembersOverviewPage from './page';
import GroupsPage from './groups/page';
import MemberSupportPage from './support/page';

describe('community member management pages', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getCommunityStats.mockResolvedValue({
      totalMembers: 0,
      activeMembers: 0,
      newMembersThisMonth: 0,
      openTickets: 0,
    });
    mocks.getMembers.mockResolvedValue({ members: [], total: 0 });
    mocks.getGroups.mockResolvedValue({ groups: [], total: 0 });
    mocks.getSupportTickets.mockResolvedValue({ tickets: [], total: 0 });
  });

  it('renders member overview KPIs and recent members from the dashboard API', async () => {
    mocks.getCommunityStats.mockResolvedValue({
      totalMembers: 42,
      activeMembers: 31,
      newMembersThisMonth: 8,
      openTickets: 3,
    });
    mocks.getMembers.mockResolvedValue({
      total: 3,
      members: [
        {
          id: 'member-admin',
          displayName: 'Admin Member',
          username: 'admin',
          role: 'admin',
          status: 'active',
          joinedAt: '2026-01-01T00:00:00.000Z',
          lastActiveAt: '2026-06-01T00:00:00.000Z',
        },
        {
          id: 'member-banned',
          displayName: 'Banned Member',
          username: 'banned',
          role: 'moderator',
          status: 'banned',
          joinedAt: '2026-02-01T00:00:00.000Z',
          lastActiveAt: '2026-05-15T00:00:00.000Z',
        },
        {
          id: 'member-pending',
          displayName: 'Pending Member',
          username: 'pending',
          role: 'member',
          status: 'pending',
          joinedAt: '2026-03-01T00:00:00.000Z',
          lastActiveAt: '2026-05-20T00:00:00.000Z',
        },
      ],
    });

    render(await MembersOverviewPage());

    expect(screen.getByRole('heading', { name: 'Members Overview' })).toBeInTheDocument();
    expect(screen.getByText('Total Members')).toBeInTheDocument();
    expect(screen.getByText('42')).toBeInTheDocument();
    expect(screen.getByText('Showing 3 of 3 members')).toBeInTheDocument();

    const adminRow = screen.getByText('Admin Member').closest('tr');
    expect(adminRow).not.toBeNull();
    expect(within(adminRow!).getByText('@admin')).toBeInTheDocument();
    expect(within(adminRow!).getByText('admin')).toBeInTheDocument();
    expect(within(adminRow!).getByText('active')).toBeInTheDocument();

    const bannedRow = screen.getByText('Banned Member').closest('tr');
    expect(bannedRow).not.toBeNull();
    expect(within(bannedRow!).getByText('moderator')).toBeInTheDocument();
    expect(within(bannedRow!).getByText('banned')).toBeInTheDocument();

    const pendingRow = screen.getByText('Pending Member').closest('tr');
    expect(pendingRow).not.toBeNull();
    expect(within(pendingRow!).getByText('member')).toBeInTheDocument();
    expect(within(pendingRow!).getByText('pending')).toBeInTheDocument();
  });

  it('renders empty member, group, and support states when there is no operational data', async () => {
    render(await MembersOverviewPage());
    expect(screen.getByText('No members registered yet')).toBeInTheDocument();
    expect(screen.getByText('No members to display. Members will appear here once users register.')).toBeInTheDocument();

    render(await GroupsPage());
    expect(screen.getByRole('heading', { name: 'Groups' })).toBeInTheDocument();
    expect(screen.getByText('No groups created yet')).toBeInTheDocument();
    expect(screen.getByText('No groups yet')).toBeInTheDocument();

    render(await MemberSupportPage());
    expect(screen.getByRole('heading', { name: 'Member Support' })).toBeInTheDocument();
    expect(screen.getByText('No tickets submitted yet')).toBeInTheDocument();
    expect(screen.getByText('No support tickets')).toBeInTheDocument();
  });

  it('renders group visibility and ticket assignment status for user-management follow-up', async () => {
    mocks.getGroups.mockResolvedValue({
      total: 2,
      groups: [
        {
          id: 'public-group',
          name: 'Mentors',
          description: 'Mentor-led critique group',
          memberCount: 12,
          isPublic: true,
          createdAt: '2026-04-01T00:00:00.000Z',
        },
        {
          id: 'private-group',
          name: 'Super Admin Review',
          description: 'Private operator group',
          memberCount: 2,
          isPublic: false,
          createdAt: '2026-05-01T00:00:00.000Z',
        },
      ],
    });
    mocks.getSupportTickets.mockResolvedValue({
      total: 3,
      tickets: [
        {
          id: 'ticket-open',
          subject: 'Cannot access course',
          status: 'open',
          priority: 'critical',
          createdBy: { username: 'learner' },
          assignedTo: null,
          createdAt: '2026-06-01T00:00:00.000Z',
          updatedAt: '2026-06-02T00:00:00.000Z',
        },
        {
          id: 'ticket-resolved',
          subject: 'Billing question',
          status: 'resolved',
          priority: 'low',
          createdBy: { username: 'studio' },
          assignedTo: { username: 'operator' },
          createdAt: '2026-06-03T00:00:00.000Z',
          updatedAt: '2026-06-04T00:00:00.000Z',
        },
        {
          id: 'ticket-custom',
          subject: 'Unknown queue state',
          status: 'triage',
          priority: 'unknown',
          createdBy: { username: 'guest' },
          assignedTo: null,
          createdAt: '2026-06-05T00:00:00.000Z',
          updatedAt: '2026-06-06T00:00:00.000Z',
        },
      ],
    });

    render(await GroupsPage());
    expect(screen.getByText('2 groups created')).toBeInTheDocument();
    expect(screen.getByText('Mentors')).toBeInTheDocument();
    expect(screen.getByText('Public')).toBeInTheDocument();
    expect(screen.getByText('Super Admin Review')).toBeInTheDocument();
    expect(screen.getByText('Private')).toBeInTheDocument();

    render(await MemberSupportPage());
    expect(screen.getByText('3 tickets')).toBeInTheDocument();

    const openTicket = screen.getByText('Cannot access course').closest('tr');
    expect(openTicket).not.toBeNull();
    expect(within(openTicket!).getByText('open')).toBeInTheDocument();
    expect(within(openTicket!).getByText('critical')).toBeInTheDocument();
    expect(within(openTicket!).getByText('@learner')).toBeInTheDocument();
    expect(within(openTicket!).getByText('—')).toBeInTheDocument();

    const resolvedTicket = screen.getByText('Billing question').closest('tr');
    expect(resolvedTicket).not.toBeNull();
    expect(within(resolvedTicket!).getByText('resolved')).toBeInTheDocument();
    expect(within(resolvedTicket!).getByText('low')).toBeInTheDocument();
    expect(within(resolvedTicket!).getByText('@operator')).toBeInTheDocument();

    const customTicket = screen.getByText('Unknown queue state').closest('tr');
    expect(customTicket).not.toBeNull();
    expect(within(customTicket!).getByText('triage')).toBeInTheDocument();
    expect(within(customTicket!).getByText('unknown')).toBeInTheDocument();
  });
});
