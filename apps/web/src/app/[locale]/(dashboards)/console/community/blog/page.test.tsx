import { render, screen } from '@testing-library/react';
import type React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getBlogAdminPosts: vi.fn(),
  getBlogAdminComments: vi.fn(),
  getBlogAdminStats: vi.fn(),
  requireDashboardCapability: vi.fn(),
}));

vi.mock('@/lib/blogs/admin', () => ({
  getBlogAdminPosts: mocks.getBlogAdminPosts,
  getBlogAdminComments: mocks.getBlogAdminComments,
  getBlogAdminStats: mocks.getBlogAdminStats,
}));

vi.mock('@/lib/blogs/admin-actions', () => ({
  unpublishPostAction: vi.fn(),
  deletePostAction: vi.fn(),
  deleteCommentAction: vi.fn(),
  importGhostAction: vi.fn(),
}));

vi.mock('@/lib/require-dashboard-capability', () => ({
  requireDashboardCapability: mocks.requireDashboardCapability,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: { href: string; children: React.ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

import BlogAdminConsolePage from './page';

function post(overrides: Record<string, unknown> = {}) {
  return {
    id: 'post-1',
    primaryAuthorHandle: 'alice',
    title: 'Migrated from Ghost',
    slug: 'migrated-from-ghost',
    status: 'Published' as const,
    publishedAt: '2026-01-15T10:00:00Z',
    updatedAt: '2026-02-01T10:00:00Z',
    commentsCount: 3,
    viewsCount: 10,
    allowComments: true,
    tags: ['news'],
    ...overrides,
  };
}

function comment(overrides: Record<string, unknown> = {}) {
  return {
    id: 'comment-1',
    blogPostId: 'post-1',
    postTitle: 'Migrated from Ghost',
    postHandle: 'alice',
    postSlug: 'migrated-from-ghost',
    parentCommentId: null,
    authorHandle: 'bob',
    content: 'Needs a look',
    createdAt: '2026-02-02T10:00:00Z',
    ...overrides,
  };
}

describe('blog admin console page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.requireDashboardCapability.mockResolvedValue(undefined);
    mocks.getBlogAdminStats.mockResolvedValue({
      status: 'ok',
      data: { publishedPosts: 5, draftPosts: 2, commentsLast30Days: 7 },
    });
    mocks.getBlogAdminPosts.mockResolvedValue({ status: 'ok', data: { items: [], hasMore: false } });
    mocks.getBlogAdminComments.mockResolvedValue({ status: 'ok', data: { items: [], hasMore: false } });
  });

  it('renders stats, the Ghost import surface, and the moderation tables', async () => {
    mocks.getBlogAdminPosts.mockResolvedValue({
      status: 'ok',
      data: { items: [post()], hasMore: false },
    });

    const view = await BlogAdminConsolePage({ searchParams: Promise.resolve({}) });
    render(view);

    expect(mocks.requireDashboardCapability).toHaveBeenCalledWith('Community.ManageBlog');
    expect(screen.getByText('5')).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
    expect(screen.getByText('7')).toBeInTheDocument();
    expect(screen.getByText('Ghost content migration')).toBeInTheDocument();
    expect(screen.getByText('Migrated from Ghost')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /take down/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /delete/i })).toBeInTheDocument();
  });

  it('renders the comments moderation tab with post context', async () => {
    mocks.getBlogAdminComments.mockResolvedValue({
      status: 'ok',
      data: { items: [comment()], hasMore: false },
    });

    const view = await BlogAdminConsolePage({ searchParams: Promise.resolve({ tab: 'comments' }) });
    render(view);

    expect(screen.getByText('Needs a look')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Migrated from Ghost' })).toHaveAttribute(
      'href',
      '/blogs/alice/migrated-from-ghost',
    );
    expect(screen.getByRole('button', { name: /remove/i })).toBeInTheDocument();
  });

  it('renders a SystemAdmin-required alert when the API denies access', async () => {
    mocks.getBlogAdminStats.mockResolvedValue({ status: 'forbidden' });

    const view = await BlogAdminConsolePage({ searchParams: Promise.resolve({}) });
    render(view);

    expect(screen.getByText('SystemAdmin required')).toBeInTheDocument();
  });

  it('surfaces action status messages from the URL', async () => {
    const view = await BlogAdminConsolePage({
      searchParams: Promise.resolve({ message: 'Imported 12/12 posts' }),
    });
    render(view);

    expect(screen.getByText('Imported 12/12 posts')).toBeInTheDocument();
  });
});
