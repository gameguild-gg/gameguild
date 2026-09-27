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

  it('shows a single art-led spotlight and the remaining projects as catalog cards', () => {
    render(<SocialProjectGallery projects={projects} />);

    expect(screen.getByRole('heading', { name: 'Projects' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Mothlight' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Project gallery' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Hollow Signal' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Level Editor' })).toBeInTheDocument();
    expect(screen.getAllByText('Mothlight')).toHaveLength(1);
  });

  it('filters by project text and includes the spotlight only once in search results', () => {
    render(<SocialProjectGallery projects={projects} />);
    fireEvent.change(screen.getByLabelText('Search projects'), { target: { value: 'mothlight' } });

    expect(screen.getByRole('heading', { name: 'Search results' })).toBeInTheDocument();
    expect(screen.getAllByText('Mothlight')).toHaveLength(1);
    expect(screen.queryByText('Hollow Signal')).not.toBeInTheDocument();
  });

  it('filters project cards by the type returned by the API', () => {
    render(<SocialProjectGallery projects={projects} />);
    fireEvent.change(screen.getByLabelText('Filter by project type'), { target: { value: 'Tool' } });

    expect(screen.getByText('Level Editor')).toBeInTheDocument();
    expect(screen.queryByText('Hollow Signal')).not.toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Mothlight' })).not.toBeInTheDocument();
  });

  it('loads the next page and extends the local search catalog', async () => {
    mocks.loadMorePublicProjects.mockResolvedValueOnce({
      items: [{ ...projects[0], id: 'project-4', slug: 'starling', title: 'Starling' }],
      hasMore: false,
    });
    render(<SocialProjectGallery projects={projects} initialHasMore />);

    fireEvent.click(screen.getByRole('button', { name: 'Load more projects' }));
    expect(await screen.findByRole('heading', { name: 'Starling' })).toBeInTheDocument();
    expect(mocks.loadMorePublicProjects).toHaveBeenCalledWith(3);
    expect(screen.queryByRole('button', { name: 'Load more projects' })).not.toBeInTheDocument();
  });
});
