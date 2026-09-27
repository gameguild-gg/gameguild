import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, ...props }: React.ComponentProps<'a'>) => <a {...props}>{children}</a>,
}));
vi.mock('@/components/projects/project-cover-image', () => ({
  ProjectCoverImage: ({ alt }: { alt: string }) => <div role="img" aria-label={alt} />,
}));

import { SocialExploreDiscovery, type SocialExploreDiscoveryProps } from './social-explore-discovery';

const discovery: SocialExploreDiscoveryProps = {
  conversations: [{
    id: 'post-1',
    authorName: 'Mira Chen',
    handle: 'mira',
    content: 'How do you playtest a narrative prototype?',
    publishedAt: 'Sep 24, 2026 UTC',
    repliesCount: 12,
    reactionsCount: 8,
    tags: ['playtest', 'narrative'],
  }],
  people: [{
    userId: 'user-1',
    handle: 'mira',
    displayName: 'Mira Chen',
    headline: 'Narrative designer',
    projectCount: 2,
    followerCount: 8,
  }],
  communities: [{
    id: 'group-1',
    name: 'Narrative Design Circle',
    description: 'Share writing and story systems.',
    memberCount: 24,
    type: 'InterestCommunity',
  }],
  projects: [{
    slug: 'echoes-of-dusk',
    title: 'Echoes of Dusk',
    summary: 'A story-driven exploration game.',
    creator: 'Mira Chen',
    buildType: 'Game',
    previewImage: '/echoes.png',
  }],
  events: [{
    id: 'event-1',
    title: 'Narrative prototype playtest',
    description: 'Help shape the opening chapter.',
    startsAt: 'Oct 2, 2026, 6:00 PM UTC',
    mode: 'Online',
    status: 'Applications open',
    availableTesterCount: 4,
  }],
};

describe('SocialExploreDiscovery', () => {
  it('presents discovery across conversations, people, communities, projects, and events', () => {
    render(<SocialExploreDiscovery {...discovery} />);

    expect(screen.getByRole('heading', { name: 'Explore' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Conversations gaining traction' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'People to meet' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Public communities' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Projects from the community' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Playtests and events' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Community hub/ })).toHaveAttribute('href', '/community');
  });

  it('filters to one discovery type without turning the page into a timeline', () => {
    render(<SocialExploreDiscovery {...discovery} />);

    fireEvent.click(screen.getByRole('button', { name: 'Projects' }));

    expect(screen.getByRole('heading', { name: 'Projects from the community' })).toBeInTheDocument();
    expect(screen.getByText('Echoes of Dusk')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Conversations gaining traction' })).not.toBeInTheDocument();
    expect(screen.getByText('1 discovery')).toBeInTheDocument();
  });

  it('searches across discovery categories and offers a clear recovery state', () => {
    render(<SocialExploreDiscovery {...discovery} />);

    fireEvent.change(screen.getByRole('searchbox', { name: 'Filter featured discoveries' }), {
      target: { value: 'Echoes' },
    });
    expect(screen.getByText('Echoes of Dusk')).toBeInTheDocument();
    expect(screen.queryByText('Narrative Design Circle')).not.toBeInTheDocument();

    fireEvent.change(screen.getByRole('searchbox', { name: 'Filter featured discoveries' }), {
      target: { value: 'no match' },
    });
    expect(screen.getByRole('heading', { name: 'No featured results match' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Clear search and filters' }));
    expect(screen.getByText('Echoes of Dusk')).toBeInTheDocument();
  });

  it('explains that people suggestions are personalized and requires sign-in', () => {
    render(<SocialExploreDiscovery {...discovery} people={[]} isAuthenticated={false} />);
    fireEvent.click(screen.getByRole('button', { name: 'People' }));

    expect(screen.getByRole('heading', { name: 'Sign in to meet people' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Sign in to discover people' })).toHaveAttribute(
      'href',
      '/sign-in?redirectTo=%2Fexplore',
    );
  });
});
