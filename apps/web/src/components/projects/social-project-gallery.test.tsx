import { fireEvent, render, screen } from '@testing-library/react';
import type { AnchorHTMLAttributes, ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ loadMorePublicProjects: vi.fn() }));

vi.mock('@/lib/projects/public-project-actions', () => ({
  loadMorePublicProjects: mocks.loadMorePublicProjects,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: AnchorHTMLAttributes<HTMLAnchorElement> & { href: string; children: ReactNode }) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

vi.mock('@/components/projects/project-cover-image', () => ({
  ProjectCoverImage: ({ alt }: { alt: string }) => <div role="img" aria-label={alt} />,
}));

import { SocialProjectGallery } from './social-project-gallery';
import type { PublicProject } from '@/lib/community/public-community';

const projects: PublicProject[] = [
  {
    slug: 'mothlight',
    title: 'Mothlight',
    creator: 'Nightjar Studio',
    creatorRole: 'Game creator',
    summary: 'Explore a greenhouse that changes after dark.',
    description: 'A hand-painted exploration game.',
    status: 'In development',
    tags: ['Adventure', 'Exploration'],
    coursePath: 'Independent',
    accent: 'from-sky-400/30 to-slate-950',
    previewImage: '/mothlight.jpg',
    buildType: 'Game',
    feedbackGoal: 'Test navigation.',
    metrics: [],
    media: [],
  },
  {
    slug: 'hollow-signal',
    title: 'Hollow Signal',
    creator: 'Aster Studio',
    creatorRole: 'Game creator',
    summary: 'Trace strange transmissions across an island.',
    description: 'A narrative adventure game.',
    status: 'Beta',
    tags: ['Narrative'],
    coursePath: 'Independent',
    accent: 'from-orange-400/30 to-slate-950',
    previewImage: '/hollow-signal.jpg',
    buildType: 'Game',
    feedbackGoal: 'Test the radio loop.',
    metrics: [],
    media: [],
  },
  {
    slug: 'level-editor',
    title: 'Level Editor',
    creator: 'Tools Collective',
    creatorRole: 'Tool creator',
    summary: 'A lightweight tool for building puzzle levels.',
    description: 'A level design utility.',
    status: 'Published',
    tags: ['Tools'],
    coursePath: 'Independent',
    accent: 'from-emerald-400/30 to-slate-950',
    previewImage: '/level-editor.jpg',
    buildType: 'Tool',
    feedbackGoal: 'Test the editor.',
    metrics: [],
    media: [],
  },
];

describe('social Projects gallery', () => {
  beforeEach(() => vi.clearAllMocks());

  it('shows one spotlight and the remaining projects as catalog cards', () => {
    render(<SocialProjectGallery projects={projects} />);

    expect(screen.getByRole('heading', { name: 'Projects' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Mothlight' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Project gallery' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Hollow Signal' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Level Editor' })).toBeInTheDocument();
    expect(screen.getAllByText('Mothlight')).toHaveLength(1);
    expect(screen.getByRole('link', { name: /Manage your projects/ })).toHaveAttribute('href', '/workspace/projects');
  });

  it('keeps submitted search and type filters visible in the form', () => {
    render(<SocialProjectGallery projects={[projects[2]]} searchQuery="level editor" projectType="Tool" />);

    expect(screen.getByRole('heading', { name: 'Search results' })).toBeInTheDocument();
    expect(screen.getByRole('searchbox', { name: 'Search projects' })).toHaveValue('level editor');
    expect(screen.getByRole('combobox', { name: 'Project type' })).toHaveTextContent('Tool');
    expect(screen.getByRole('link', { name: 'Clear filters' })).toHaveAttribute('href', '/projects');
  });

  it('loads the next matching page and appends it to the gallery', async () => {
    mocks.loadMorePublicProjects.mockResolvedValueOnce({
      items: [{ ...projects[0], id: 'project-4', slug: 'starling', title: 'Starling' }],
      hasMore: false,
    });
    render(<SocialProjectGallery projects={projects} searchQuery="game" projectType="Game" initialHasMore />);

    fireEvent.click(screen.getByRole('button', { name: 'Load more projects' }));
    expect(await screen.findByRole('heading', { name: 'Starling' })).toBeInTheDocument();
    expect(mocks.loadMorePublicProjects).toHaveBeenCalledWith(3, 'game', 'Game');
    expect(screen.queryByRole('button', { name: 'Load more projects' })).not.toBeInTheDocument();
  });

  it('shows a retry action when loading another page fails', async () => {
    mocks.loadMorePublicProjects.mockResolvedValueOnce({ items: [], hasMore: false, error: 'Temporary failure.' });
    render(<SocialProjectGallery projects={projects} initialHasMore />);

    fireEvent.click(screen.getByRole('button', { name: 'Load more projects' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Temporary failure.');
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument();
  });

  it('distinguishes a true empty catalog from an API error', () => {
    const { rerender } = render(<SocialProjectGallery projects={[]} />);
    expect(screen.getByRole('heading', { name: 'No public projects yet' })).toBeInTheDocument();

    rerender(<SocialProjectGallery projects={[]} initialError="The catalog is unavailable." />);
    expect(screen.getByRole('alert')).toHaveTextContent('The catalog is unavailable.');
    expect(screen.queryByRole('heading', { name: 'No public projects yet' })).not.toBeInTheDocument();
  });

  it('offers a helpful empty result state for a search with no matches', () => {
    render(<SocialProjectGallery projects={[]} searchQuery="no such project" />);

    expect(screen.getByRole('heading', { name: 'No projects match your search' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Clear filters' })).toHaveAttribute('href', '/projects');
  });
});
