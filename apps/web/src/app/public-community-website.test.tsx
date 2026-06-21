import { render, screen, within } from '@testing-library/react';
import type { AnchorHTMLAttributes, ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: AnchorHTMLAttributes<HTMLAnchorElement> & { href: string; children: ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock('@/i18n', () => ({
  Link: ({ href, children, ...props }: AnchorHTMLAttributes<HTMLAnchorElement> & { href: string; children: ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

import { PublicWebsiteHeader } from '@/components/site/public-website-shell';
import CommunityPage from './[locale]/(community)/community/page';
import JobsPage from './[locale]/(contents)/(jobs)/jobs/page';
import ProjectsPage from './[locale]/(contents)/(projects)/projects/page';
import ProjectDetailPage from './[locale]/(contents)/(projects)/projects/[project]/page';
import TestingLabPage from './[locale]/(contents)/(testing-lab)/testing-lab/page';
import HomePage from './[locale]/(site)/page';

describe('public community website UX', () => {
  it('exposes the learning-to-community information architecture in the header', () => {
    render(<PublicWebsiteHeader />);

    const nav = screen.getByRole('navigation', { name: /main navigation/i });
    expect(within(nav).getAllByRole('link').map((link) => link.textContent)).toEqual([
      'Courses',
      'Programs',
      'Testing Lab',
      'Projects',
      'Community',
      'Jobs',
      'About',
    ]);
  });

  it('turns the home page into a community gateway', async () => {
    render(await HomePage({ params: Promise.resolve({ locale: 'en-US' }) } as PageProps<'/[locale]'>));

    expect(screen.getByRole('heading', { name: /learn, build & connect/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /featured community projects/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /active members/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /upcoming playtests/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /community activity/i })).toBeInTheDocument();
  });

  it('renders a public project showcase and detail path', async () => {
    render(await ProjectsPage());

    expect(screen.getByRole('heading', { name: /project showcase/i })).toBeInTheDocument();
    expect(screen.getAllByRole('link', { name: /view project/i }).length).toBeGreaterThan(0);
    expect(screen.getByRole('link', { name: /submit to testing lab/i })).toBeInTheDocument();
  });

  it('renders project detail with creator, media, playtest status, and community CTAs', async () => {
    render(await ProjectDetailPage({ params: Promise.resolve({ project: 'skybound-courier' }) }));

    expect(screen.getByRole('heading', { name: /skybound courier/i })).toBeInTheDocument();
    expect(screen.getAllByText(/creator/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/playtest/i).length).toBeGreaterThan(0);
    expect(screen.getByRole('link', { name: /join this playtest/i })).toBeInTheDocument();
  });

  it('renders the community hub and public testing lab entry', async () => {
    render(await CommunityPage());

    expect(screen.getByRole('heading', { name: /community hub/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /member spotlights/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /recent activity/i })).toBeInTheDocument();

    render(await TestingLabPage());
    expect(screen.getByRole('heading', { name: /testing lab/i })).toBeInTheDocument();
    expect(screen.getAllByText(/submit a build/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/define test goals/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/feedback report/i).length).toBeGreaterThan(0);
  });

  it('replaces the jobs placeholder with a community opportunities page', async () => {
    render(await JobsPage());

    expect(screen.getByRole('heading', { name: /community opportunities/i })).toBeInTheDocument();
    expect(screen.getAllByText(/mentor/i).length).toBeGreaterThan(0);
    expect(screen.queryByText(/show modal immediately/i)).not.toBeInTheDocument();
  });
});
