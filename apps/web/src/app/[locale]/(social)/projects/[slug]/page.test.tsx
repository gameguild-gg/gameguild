import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getVisibleProject: vi.fn(),
  getPublicTestingEventsDirectory: vi.fn(),
}));

vi.mock('@/lib/projects/public-projects', () => ({ getVisibleProject: mocks.getVisibleProject }));
vi.mock('@/lib/testing-lab/events-public-queries', () => ({ getPublicTestingEventsDirectory: mocks.getPublicTestingEventsDirectory }));
vi.mock('next/navigation', () => ({ notFound: vi.fn(() => { throw new Error('not found'); }) }));
vi.mock('@/i18n/navigation', () => ({ Link: ({ href, children, ...props }: { href: string; children: ReactNode }) => <a href={href} {...props}>{children}</a> }));
vi.mock('@/components/projects/project-cover-image', () => ({
  ProjectCoverImage: ({ alt }: { alt: string }) => <div role="img" aria-label={alt} />,
}));

import Page from './page';

const project = {
  id: 'project-1',
  slug: 'mothlight',
  title: 'Mothlight',
  creator: 'Nightjar Studio',
  creatorRole: 'Game creator',
  summary: 'An exploration game.',
  description: 'Explore the greenhouse.',
  status: 'Beta',
  tags: [],
  coursePath: 'Independent',
  accent: 'from-sky-400/30 to-slate-950',
  previewImage: '/mothlight.png',
  buildType: 'Game',
  feedbackGoal: 'Test exploration.',
  metrics: [],
  media: [],
};

describe('social project detail', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getVisibleProject.mockResolvedValue(project);
    mocks.getPublicTestingEventsDirectory.mockResolvedValue({ events: [], accessIssues: [] });
  });

  it('links to a playtest that includes this published game', async () => {
    mocks.getPublicTestingEventsDirectory.mockResolvedValue({
      events: [{
        id: 'event-1',
        name: 'Mothlight feedback session',
        status: 'Scheduled',
        endsAt: '2030-10-02T21:00:00.000Z',
        startsAt: '2030-10-02T18:00:00.000Z',
        games: [{ projectId: 'project-1' }],
      }],
      accessIssues: [],
    });

    render(await Page({ params: Promise.resolve({ slug: 'mothlight' }) }));

    expect(screen.getByRole('link', { name: 'View this playtest' })).toHaveAttribute(
      'href',
      '/testing-lab/events/event-1',
    );
  });

  it('does not claim this game has a playtest when no related event is public', async () => {
    render(await Page({ params: Promise.resolve({ slug: 'mothlight' }) }));

    expect(screen.getByRole('link', { name: 'Browse playtests' })).toHaveAttribute('href', '/testing-lab');
    expect(screen.queryByRole('link', { name: 'Join this playtest' })).not.toBeInTheDocument();
  });
});
