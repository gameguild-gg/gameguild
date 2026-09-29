import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ getPublishedProjectsPage: vi.fn() }));

vi.mock('@/lib/projects/public-projects', () => ({
  getPublishedProjectsPage: mocks.getPublishedProjectsPage,
  PUBLIC_PROJECT_PAGE_SIZE: 24,
}));

vi.mock('@/components/projects/social-project-gallery', () => ({
  SocialProjectGallery: (props: {
    projects: unknown[];
    searchQuery: string;
    projectType?: string;
    initialHasMore: boolean;
    initialError: string;
  }) => (
    <div
      data-testid="gallery"
      data-query={props.searchQuery}
      data-type={props.projectType ?? ''}
      data-has-more={String(props.initialHasMore)}
      data-error={props.initialError}
      data-project-count={String(props.projects.length)}
    />
  ),
}));

import Page from './page';

describe('public Projects route', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getPublishedProjectsPage.mockResolvedValue([]);
  });

  it('passes search and type filters to the public catalog query', async () => {
    mocks.getPublishedProjectsPage.mockResolvedValue(Array.from({ length: 25 }, (_, index) => ({ id: `p-${index}` })));

    render(await Page({ searchParams: Promise.resolve({ q: '  mothlight  ', type: 'Game' }) }));

    expect(mocks.getPublishedProjectsPage).toHaveBeenCalledWith(0, 25, 'mothlight', 'Game');
    expect(screen.getByTestId('gallery')).toHaveAttribute('data-query', 'mothlight');
    expect(screen.getByTestId('gallery')).toHaveAttribute('data-type', 'Game');
    expect(screen.getByTestId('gallery')).toHaveAttribute('data-project-count', '24');
    expect(screen.getByTestId('gallery')).toHaveAttribute('data-has-more', 'true');
  });

  it('ignores repeated query params and unknown project types', async () => {
    render(await Page({ searchParams: Promise.resolve({ q: ['first', 'second'], type: 'Unknown' }) }));

    expect(mocks.getPublishedProjectsPage).toHaveBeenCalledWith(0, 25, '', undefined);
    expect(screen.getByTestId('gallery')).toHaveAttribute('data-query', '');
    expect(screen.getByTestId('gallery')).toHaveAttribute('data-type', '');
  });

  it('passes an explicit error state when the catalog request fails', async () => {
    mocks.getPublishedProjectsPage.mockRejectedValueOnce(new Error('Internal API details'));

    render(await Page({ searchParams: Promise.resolve({}) }));

    expect(screen.getByTestId('gallery')).toHaveAttribute(
      'data-error',
      'We couldn’t load projects right now. Try again.',
    );
    expect(screen.getByTestId('gallery')).toHaveAttribute('data-project-count', '0');
  });
});
