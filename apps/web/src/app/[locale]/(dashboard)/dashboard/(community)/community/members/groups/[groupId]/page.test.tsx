import { render, screen, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getGroup: vi.fn(),
  getGroupMembers: vi.fn(),
  getMemberAccessDirectory: vi.fn(),
  updateCommunityGroup: vi.fn(),
  addCommunityGroupMember: vi.fn(),
  approveCommunityGroupMember: vi.fn(),
  rejectCommunityGroupMember: vi.fn(),
  changeCommunityGroupMemberRole: vi.fn(),
  removeCommunityGroupMember: vi.fn(),
  archiveCommunityGroup: vi.fn(),
}));

vi.mock('@/lib/community', () => ({
  getGroup: mocks.getGroup,
  getGroupMembers: mocks.getGroupMembers,
  getMemberAccessDirectory: mocks.getMemberAccessDirectory,
}));

vi.mock('@/lib/community/actions/groups', () => ({
  updateCommunityGroup: mocks.updateCommunityGroup,
  addCommunityGroupMember: mocks.addCommunityGroupMember,
  approveCommunityGroupMember: mocks.approveCommunityGroupMember,
  rejectCommunityGroupMember: mocks.rejectCommunityGroupMember,
  changeCommunityGroupMemberRole: mocks.changeCommunityGroupMemberRole,
  removeCommunityGroupMember: mocks.removeCommunityGroupMember,
  archiveCommunityGroup: mocks.archiveCommunityGroup,
}));

import GroupDetailPage from './page';

describe('community group detail page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getGroup.mockResolvedValue({
      group: {
        id: 'group-1',
        name: 'Pixel Art Mentors',
        description: 'Mentor-led critiques',
        memberCount: 2,
        pendingMemberCount: 1,
        createdAt: '2026-01-01T00:00:00.000Z',
        isPublic: false,
        visibility: 'InviteOnly',
        status: 'Active',
        type: 'StudyGroup',
      },
      error: null,
    });
    mocks.getGroupMembers.mockResolvedValue({
      members: [
        {
          id: 'member-1',
          userId: 'user-owner',
          displayName: 'Owner User',
          email: 'owner@game-guild.com',
          role: 'Owner',
          status: 'Active',
          requestedAt: '2026-01-01T00:00:00.000Z',
          joinedAt: '2026-01-01T00:00:00.000Z',
        },
        {
          id: 'member-2',
          userId: 'user-pending',
          displayName: 'Pending User',
          email: 'pending@game-guild.com',
          role: 'Member',
          status: 'Pending',
          requestedAt: '2026-01-02T00:00:00.000Z',
          joinedAt: null,
        },
      ],
      error: null,
    });
    mocks.getMemberAccessDirectory.mockResolvedValue({
      total: 3,
      currentUserId: 'user-owner',
      error: null,
      members: [
        {
          member: {
            id: 'user-owner',
            username: 'owner',
            displayName: 'Owner User',
            email: 'owner@game-guild.com',
            status: 'active',
            joinedAt: '2026-01-01T00:00:00.000Z',
            lastActiveAt: '2026-01-01T00:00:00.000Z',
          },
          memberships: [],
          primaryMembership: null,
          role: 'SystemAdmin',
          isSuperAdmin: true,
          isCurrentUser: true,
        },
        {
          member: {
            id: 'user-new',
            username: 'new',
            displayName: 'New Member',
            email: 'new@game-guild.com',
            status: 'active',
            joinedAt: '2026-01-01T00:00:00.000Z',
            lastActiveAt: '2026-01-01T00:00:00.000Z',
          },
          memberships: [],
          primaryMembership: null,
          role: 'Member',
          isSuperAdmin: false,
          isCurrentUser: false,
        },
      ],
    });
  });

  it('renders settings, add-member controls, and active/pending member operations', async () => {
    render(
      await GroupDetailPage({
        params: Promise.resolve({ groupId: 'group-1' }),
        searchParams: Promise.resolve({}),
      }),
    );

    expect(screen.getByRole('heading', { name: 'Pixel Art Mentors' })).toBeInTheDocument();
    expect(screen.getByText('Group settings')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save group' })).toBeInTheDocument();
    expect(screen.getByText('Add member')).toBeInTheDocument();
    expect(screen.getByText('Archive group')).toBeInTheDocument();

    const ownerRow = screen.getByText('Owner User').closest('tr');
    expect(ownerRow).not.toBeNull();
    expect(within(ownerRow!).getAllByText('Owner').length).toBeGreaterThan(0);
    expect(within(ownerRow!).getByRole('button', { name: 'Update role' })).toBeInTheDocument();
    expect(within(ownerRow!).getByRole('button', { name: 'Remove' })).toBeInTheDocument();

    const pendingRow = screen.getByText('Pending User').closest('tr');
    expect(pendingRow).not.toBeNull();
    expect(within(pendingRow!).getByText('Pending')).toBeInTheDocument();
    expect(within(pendingRow!).getByRole('button', { name: 'Approve' })).toBeInTheDocument();
    expect(within(pendingRow!).getByRole('button', { name: 'Reject' })).toBeInTheDocument();
  });
});
