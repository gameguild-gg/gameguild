import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';

vi.mock('next/link', () => ({
  default: ({
    children,
    href,
    ...rest
  }: {
    children: React.ReactNode;
    href: string;
  }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

const { BlogPostView } = await import('@/components/blogs/blog-post-view');
const { BlogPostCard } = await import('@/components/blogs/blog-post-card');
type BlogPostDetail = import('@/lib/blogs/types').BlogPostDetail;
type BlogPostSummary = import('@/lib/blogs/types').BlogPostSummary;

const detail: BlogPostDetail = {
  id: 'post-1',
  title: 'Author links post',
  slug: 'author-links-post',
  primaryAuthorHandle: 'ada',
  primaryAuthorDisplayName: 'Ada Lovelace',
  coAuthorHandles: ['grace', 'alan'],
  publishedAt: '2025-01-02T00:00:00.000Z',
  readTimeMinutes: 5,
  tags: ['testing'],
  format: 'Markdown',
};

const summary: BlogPostSummary = {
  id: 'post-1',
  title: 'Author links post',
  slug: 'author-links-post',
  primaryAuthorHandle: 'ada',
  primaryAuthorDisplayName: 'Ada Lovelace',
  coAuthorHandles: ['grace', 'alan'],
  publishedAt: '2025-01-02T00:00:00.000Z',
  readTimeMinutes: 5,
  tags: ['testing'],
  excerpt: 'A short excerpt.',
};

describe('BlogPostView author profile links', () => {
  it('links the primary author by display name and each co-author by handle', () => {
    render(<BlogPostView post={detail} />);

    expect(screen.getByRole('link', { name: 'Ada Lovelace' })).toHaveAttribute('href', '/social/profiles/ada');
    expect(screen.getByRole('link', { name: 'grace' })).toHaveAttribute('href', '/social/profiles/grace');
    expect(screen.getByRole('link', { name: 'alan' })).toHaveAttribute('href', '/social/profiles/alan');
  });
});

describe('BlogPostCard author profile links', () => {
  it('links the primary author by display name and each co-author by handle', () => {
    render(<BlogPostCard post={summary} />);

    expect(screen.getByRole('link', { name: 'Ada Lovelace' })).toHaveAttribute('href', '/social/profiles/ada');
    expect(screen.getByRole('link', { name: 'grace' })).toHaveAttribute('href', '/social/profiles/grace');
    expect(screen.getByRole('link', { name: 'alan' })).toHaveAttribute('href', '/social/profiles/alan');
  });
});
