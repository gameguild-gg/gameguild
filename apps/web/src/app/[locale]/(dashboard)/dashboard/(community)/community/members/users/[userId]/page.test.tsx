import { render, screen } from '@testing-library/react';
import type React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getMember: vi.fn(),
  notFound: vi.fn(),
}));

vi.mock('@/lib/community', () => ({
  getMember: mocks.getMember,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: { href: string; children: React.ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock('next/navigation', () => ({
  notFound: mocks.notFound,
}));

import UserDetailPage from './page';

function buildMember(overrides: Record<string, unknown> = {}) {
  return {
    id: 'user-1',
    username: 'ada',
    handle: 'ada-dev',
    displayName: 'Ada Developer',
    email: 'ada@game-guild.com',
    role: 'admin',
    status: 'active',
    headline: 'Gameplay engineer',
    bio: 'Builds AI-heavy gameplay systems.',
    location: 'Sao Paulo',
    website: 'https://ada.example',
    timezone: 'America/Sao_Paulo',
    phoneNumber: '+55 11 99999-1111',
    joinedAt: '2026-01-10T00:00:00.000Z',
    lastActiveAt: '2026-06-01T00:00:00.000Z',
    updatedAt: '2026-06-05T00:00:00.000Z',
    availabilityStatus: 'Available',
    followerCount: 12,
    followingCount: 8,
    postCount: 5,
    projectCount: 3,
    skills: [
      { id: 'skill-1', name: 'Unreal Engine', proficiency: 'Advanced' },
      { id: 'skill-2', name: 'Technical Art', proficiency: null },
    ],
    portfolioItems: [
      {
        id: 'portfolio-1',
        title: 'Boss AI prototype',
        description: 'Combat behavior tree showcase.',
        url: 'https://portfolio.example/boss-ai',
        isPinned: true,
      },
    ],
    ...overrides,
  };
}

describe('community user detail page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.notFound.mockImplementation(() => {
      throw new Error('not-found');
    });
  });

  it('renders the dashboard member profile, stats, skills, and portfolio', async () => {
    mocks.getMember.mockResolvedValue(buildMember());

    render(await UserDetailPage({ params: Promise.resolve({ userId: 'user-1' }) }));

    expect(mocks.getMember).toHaveBeenCalledWith('user-1');
    expect(screen.getByRole('heading', { name: 'Ada Developer' })).toBeInTheDocument();
    expect(screen.getByText('@ada-dev')).toBeInTheDocument();
    expect(screen.getByText('Gameplay engineer')).toBeInTheDocument();
    expect(screen.getByText('Available')).toBeInTheDocument();
    expect(screen.getByText('active')).toBeInTheDocument();
    expect(screen.getByText('admin')).toBeInTheDocument();
    expect(screen.getByText('ada@game-guild.com')).toBeInTheDocument();
    expect(screen.getByText('+55 11 99999-1111')).toBeInTheDocument();
    expect(screen.getByText('Builds AI-heavy gameplay systems.')).toBeInTheDocument();
    expect(screen.getByText('Sao Paulo')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'https://ada.example' })).toHaveAttribute('href', 'https://ada.example');
    expect(screen.getByText('12')).toBeInTheDocument();
    expect(screen.getByText(/Unreal Engine/)).toBeInTheDocument();
    expect(screen.getByText(/Advanced/)).toBeInTheDocument();
    expect(screen.getByText('Technical Art')).toBeInTheDocument();
    expect(screen.getByText('Boss AI prototype')).toBeInTheDocument();
    expect(screen.getByText('Pinned')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /view project/i })).toHaveAttribute('href', 'https://portfolio.example/boss-ai');
  });

  it('falls back from handle to username and hides optional profile sections', async () => {
    mocks.getMember.mockResolvedValue(buildMember({
      handle: undefined,
      phoneNumber: undefined,
      bio: undefined,
      location: undefined,
      website: undefined,
      availabilityStatus: 'NotSet',
      skills: [],
      portfolioItems: [],
    }));

    render(await UserDetailPage({ params: Promise.resolve({ userId: 'user-1' }) }));

    expect(screen.getByText('@ada')).toBeInTheDocument();
    expect(screen.queryByText('Available')).not.toBeInTheDocument();
    expect(screen.queryByText('Skills')).not.toBeInTheDocument();
    expect(screen.queryByText('Portfolio')).not.toBeInTheDocument();
  });

  it('renders non-admin status variants and optional portfolio fallbacks', async () => {
    mocks.getMember.mockResolvedValue(buildMember({
      role: 'moderator',
      status: 'banned',
      skills: [
        { id: null, name: 'Mentoring', proficiency: '' },
      ],
      portfolioItems: [
        {
          id: null,
          title: 'Private critique notes',
          description: null,
          url: '',
          isPinned: false,
        },
      ],
    }));

    render(await UserDetailPage({ params: Promise.resolve({ userId: 'user-1' }) }));

    expect(screen.getByText('banned')).toBeInTheDocument();
    expect(screen.getByText('moderator')).toBeInTheDocument();
    expect(screen.getByText('Mentoring')).toBeInTheDocument();
    expect(screen.queryByText(/Mentoring ·/)).not.toBeInTheDocument();
    expect(screen.getByText('Private critique notes')).toBeInTheDocument();
    expect(screen.queryByText('Pinned')).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /view project/i })).not.toBeInTheDocument();
  });

  it('renders ordinary member role and secondary account status variants', async () => {
    mocks.getMember.mockResolvedValue(buildMember({
      role: 'member',
      status: 'pending',
      followerCount: null,
      followingCount: null,
      postCount: null,
      projectCount: null,
    }));

    render(await UserDetailPage({ params: Promise.resolve({ userId: 'user-1' }) }));

    expect(screen.getByText('member')).toBeInTheDocument();
    expect(screen.getByText('pending')).toBeInTheDocument();
    expect(screen.queryByText('Followers')).not.toBeInTheDocument();
  });

  it('uses the Next not-found boundary when the user does not exist', async () => {
    mocks.getMember.mockResolvedValue(null);

    await expect(UserDetailPage({ params: Promise.resolve({ userId: 'missing' }) })).rejects.toThrow('not-found');
    expect(mocks.notFound).toHaveBeenCalled();
  });
});
